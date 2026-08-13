using ELRSControl.Services;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ELRSControl.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly SerialPortManager _portManager = new();
        private readonly ConfigManager _configManager = new();
        private readonly DispatcherTimer _transmitTimer;

        private bool _isTransmitting = false;
        private bool _endTransmissingStatus = false;
        private string _selectedPort = "COM9";
        private string _lastPort = "COM9";
        private string _selectedBaudRate = "115200";
        private string _selectedAddress = "FF";
        private string _connectBtnContent = "Старт";
        private Brush _connectBtnBackground = new SolidColorBrush(Color.FromArgb(33, 255, 255, 255));

        private readonly ushort[] _channels = new ushort[16] { 1500, 1500, 1500, 1500, 1000, 1500, 1500, 1500, 1500, 1500, 1500, 1500, 1500, 1500, 1500, 1500 };

        public ObservableCollection<SerialPortInfo> AvailablePorts { get; set; } = new();
        public ObservableCollection<string> Addresses { get; set; } = new();
        private string _customBaudRateText = "115200";
        private string _newAddressText = "AA";
        private bool _isAddAddressVisible = false;

        public ObservableCollection<string> BaudRates { get; set; } = new()
        {
            "9600", "19200", "38400", "57600", "115200"
        };

        #region Properties for Menu Bindings

        public string CustomBaudRateText
        {
            get => _customBaudRateText;
            set
            {
                if (SetProperty(ref _customBaudRateText, value) && !string.IsNullOrWhiteSpace(value))
                {
                    SelectedBaudRate = value;
                }
            }
        }

        public string NewAddressText
        {
            get => _newAddressText;
            set => SetProperty(ref _newAddressText, value);
        }

        public bool IsAddAddressVisible
        {
            get => _isAddAddressVisible;
            set => SetProperty(ref _isAddAddressVisible, value);
        }

        #endregion

        #region Commands for Menu

        public ICommand SelectPortCommand { get; }
        public ICommand SelectBaudRateCommand { get; }
        public ICommand SelectAddressCommand { get; }
        public ICommand ShowAddAddressCommand { get; }
        public ICommand ConfirmAddAddressCommand { get; }

        #endregion

        public MainViewModel()
        {
            SelectPortCommand = new RelayCommand(param => { if (param is string p) SelectedPort = p; });
            SelectBaudRateCommand = new RelayCommand(param => { if (param is string b) SelectedBaudRate = b; });
            SelectAddressCommand = new RelayCommand(param => { if (param is string a) SelectedAddress = a; });

            ShowAddAddressCommand = new RelayCommand(_ => IsAddAddressVisible = true);
            ConfirmAddAddressCommand = new RelayCommand(_ => AddNewAddress());

            ToggleConnectionCommand = new RelayCommand(_ => ToggleSending());
            DeleteAddressCommand = new RelayCommand(param => DeleteAddress(param?.ToString()));
            _transmitTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(20) // 50 Hz
            };
            _transmitTimer.Tick += (s, e) => TransmitTimer_Tick();

            ToggleConnectionCommand = new RelayCommand(_ => ToggleSending());
            AddAddressCommand = new RelayCommand(param => AddAddress(param?.ToString()));
            DeleteAddressCommand = new RelayCommand(param => DeleteAddress(param?.ToString()));

            InitializeData();
        }

        #region Properties for Channels (Ch0..Ch15)
        public ushort Ch0 { get => _channels[0]; set { if (SetChannel(0, value)) OnPropertyChanged(nameof(Throttle)); } }
        public ushort Ch1 { get => _channels[1]; set { if (SetChannel(1, value)) OnPropertyChanged(nameof(Yaw)); } }
        public ushort Ch2 { get => _channels[2]; set { if (SetChannel(2, value)) OnPropertyChanged(nameof(Roll)); } }
        public ushort Ch3 { get => _channels[3]; set { if (SetChannel(3, value)) OnPropertyChanged(nameof(Pitch)); } }
        public ushort Ch4 { get => _channels[4]; set => SetChannel(4, value); }
        public ushort Ch5 { get => _channels[5]; set => SetChannel(5, value); }
        public ushort Ch6 { get => _channels[6]; set => SetChannel(6, value); }
        public ushort Ch7 { get => _channels[7]; set => SetChannel(7, value); }
        public ushort Ch8 { get => _channels[8]; set => SetChannel(8, value); }
        public ushort Ch9 { get => _channels[9]; set => SetChannel(9, value); }
        public ushort Ch10 { get => _channels[10]; set => SetChannel(10, value); }
        public ushort Ch11 { get => _channels[11]; set => SetChannel(11, value); }
        public ushort Ch12 { get => _channels[12]; set => SetChannel(12, value); }
        public ushort Ch13 { get => _channels[13]; set => SetChannel(13, value); }
        public ushort Ch14 { get => _channels[14]; set => SetChannel(14, value); }
        public ushort Ch15 { get => _channels[15]; set => SetChannel(15, value); }
        public ushort Throttle { get => Ch0; set => Ch0 = value; }
        public ushort Yaw { get => Ch1; set => Ch1 = value; }
        public ushort Roll { get => Ch2; set => Ch2 = value; }
        public ushort Pitch { get => Ch3; set => Ch3 = value; }

        private bool SetChannel(int index, ushort value)
        {
            if (_channels[index] == value) return false;
            _channels[index] = value;
            OnPropertyChanged($"Ch{index}");
            return true;
        }
        #endregion

        #region State Properties
        public string SelectedPort
        {
            get => _selectedPort;
            set
            {
                if (SetProperty(ref _selectedPort, value) && _isTransmitting)
                {
                    RestartSending();
                }
            }
        }

        public string SelectedBaudRate
        {
            get => _selectedBaudRate;
            set
            {
                if (SetProperty(ref _selectedBaudRate, value))
                {
                    CustomBaudRateText = value;
                    _configManager.SaveBaudRate(value); // Сохраняем при изменении

                    if (_isTransmitting)
                    {
                        RestartSending();
                    }
                }
            }
        }
        public string SelectedAddress
        {
            get => _selectedAddress;
            set
            {
                if (SetProperty(ref _selectedAddress, value))
                {
                    _configManager.SaveAddress(value); // Сохраняем при изменении
                }
            }
        }

        public string ConnectBtnContent
        {
            get => _connectBtnContent;
            set => SetProperty(ref _connectBtnContent, value);
        }

        public Brush ConnectBtnBackground
        {
            get => _connectBtnBackground;
            set => SetProperty(ref _connectBtnBackground, value);
        }

        public bool IsTransmitting => _isTransmitting;
        #endregion

        #region Commands
        public ICommand ToggleConnectionCommand { get; }
        public ICommand AddAddressCommand { get; }
        public ICommand DeleteAddressCommand { get; }
        #endregion

        #region Core Logic
        private void InitializeData()
        {
            RefreshPorts();

            var config = _configManager.LoadConfig();
            Addresses.Add("FF");
            Addresses.Add("C8");
            if (config.CustomAddresses != null)
            {
                foreach (var addr in config.CustomAddresses)
                {
                    if (addr != "C8" && addr != "FF" && !Addresses.Contains(addr))
                        Addresses.Add(addr);
                }
            }
            if (!string.IsNullOrWhiteSpace(config.LastBaudRate))
            {
                _selectedBaudRate = config.LastBaudRate;
                _customBaudRateText = config.LastBaudRate;
            }
            if (!string.IsNullOrWhiteSpace(config.LastAddress))
            {
                _selectedAddress = config.LastAddress;
            }
        }

        public void RefreshPorts()
        {
            AvailablePorts.Clear();
            var ports = SerialPortManager.GetAvailablePorts();
            foreach (var p in ports) AvailablePorts.Add(p);

            if (AvailablePorts.Count > 0 )
            {
                bool portExists = AvailablePorts.Any(p => p.PortName == SelectedPort);

                if (!portExists || SelectedPort == "пусто" || string.IsNullOrWhiteSpace(SelectedPort))
                {
                    SelectedPort = AvailablePorts[0].PortName;
                }
            }
            else if (AvailablePorts.Count == 0)
            {
                SelectedPort = "пусто";
            }
        }

        public void HandleDeviceChange(int action)
        {
            const int DBT_DEVICEARRIVAL = 0x8000;
            const int DBT_DEVICEREMOVECOMPLETE = 0x8004;

            if (action == DBT_DEVICEARRIVAL || action == DBT_DEVICEREMOVECOMPLETE)
            {
                if (_isTransmitting)
                {
                    ToggleSending();
                    _endTransmissingStatus = true;
                    ConnectBtnBackground = new SolidColorBrush(Color.FromArgb(130, 154, 165, 54));
                    _lastPort = SelectedPort;
                }

                if (SelectedPort != "пусто" && !_endTransmissingStatus) _lastPort = SelectedPort;
                RefreshPorts();

                foreach (var port in AvailablePorts)
                {
                    if (port.PortName == _lastPort)
                    {
                        SelectedPort = _lastPort;
                        if (_endTransmissingStatus)
                        {
                            ToggleSending();
                            _endTransmissingStatus = false;
                            ConnectBtnBackground = new SolidColorBrush(Color.FromArgb(33, 255, 255, 255));
                        }
                    }
                }
            }
        }
        private void AddNewAddress()
        {
            var address = NewAddressText?.Trim().ToUpper();
            if (!string.IsNullOrWhiteSpace(address) && byte.TryParse(address, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
            {
                _configManager.AddAddress(Addresses, address);
                SelectedAddress = address;
                IsAddAddressVisible = false;
                NewAddressText = "AA";
            }
            else
            {
                MessageBox.Show("Введите корректный HEX-адрес (например, 1F, А0, 8C)", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        public void ToggleSending()
        {
            if (_isTransmitting) _endTransmissingStatus = false;
            ConnectBtnBackground = new SolidColorBrush(Color.FromArgb(33, 255, 255, 255));

            if (!_isTransmitting)
            {
                if (string.IsNullOrWhiteSpace(SelectedPort) || SelectedPort == "пусто")
                {
                    MessageBox.Show("Пожалуйста, выберите серийный порт", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(SelectedBaudRate, out int baudRate)) baudRate = 115200;

                if (_portManager.OpenPort(SelectedPort, baudRate))
                {
                    _isTransmitting = true;
                    _transmitTimer.Start();
                    ConnectBtnContent = "Стоп";
                }
                else
                {
                    MessageBox.Show($"Не удалось открыть порт {SelectedPort}", "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                _transmitTimer.Stop();
                _isTransmitting = false;
                _portManager.ClosePort();
                ConnectBtnContent = "Старт";
            }
        }

        private void RestartSending()
        {
            ToggleSending();
            ToggleSending();
        }

        private void TransmitTimer_Tick()
        {
            if (!_isTransmitting) return;

            try
            {
                byte address = byte.Parse(SelectedAddress, NumberStyles.HexNumber);
                ushort[] auxChannels = new ushort[12];
                Array.Copy(_channels, 4, auxChannels, 0, 12);

                _portManager.SendCRSFPacket(address, Ch2, Ch3, Ch1, Ch0, auxChannels);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при отправке: {ex.Message}");
            }
        }

        public void StopTransmissionOnClose()
        {
            if (_isTransmitting) ToggleSending();
        }

        private void AddAddress(string address)
        {
            if (!string.IsNullOrWhiteSpace(address) && byte.TryParse(address, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
            {
                _configManager.AddAddress(Addresses, address.ToUpper());
                SelectedAddress = address.ToUpper();
            }
        }

        private void DeleteAddress(string address)
        {
            if (address != "FF" && address != "C8")
            {
                _configManager.RemoveAddress(Addresses, address);
            }
        }
        #endregion
    }
}