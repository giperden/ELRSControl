using ELRSControl.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ELRSControl
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class SliderWindow : Window, INotifyPropertyChanged
    {
        private string _maximizeIcon = "\uE922";
        private DispatcherTimer _transmitTimer;
        private SerialPortManager _portManager = new();
        private ConfigManager _configManager = new();
        private ObservableCollection<SerialPortInfo> _availablePorts = new();
        private ObservableCollection<string> _addresses = new();
        public string MaximizeIcon
        {
            get => _maximizeIcon;
            set { _maximizeIcon = value; OnPropertyChanged(); }
        }

        public SliderWindow()
        {
            InitializeComponent();

            this.DataContext = this;
            this.StateChanged += SliderWindow_StateChanged;
            _transmitTimer = new DispatcherTimer();
            _transmitTimer.Interval = TimeSpan.FromMilliseconds(20);
            _transmitTimer.Tick += (s, e) => TransmitTimer_Tick();
            this.Loaded += SliderWindow_Loaded;

            InitializeUI();
        }
        private void SliderWindow_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            HwndSource src = HwndSource.FromHwnd(windowHandle);
            src?.AddHook(new HwndSourceHook(WndProc));
        }
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == GlobalStates.WM_DEVICECHANGE)
            {
                int action = wParam.ToInt32();
                if (action == GlobalStates.DBT_DEVICEARRIVAL || action == GlobalStates.DBT_DEVICEREMOVECOMPLETE)
                {
                    if (GlobalStates._isTransmitting)
                    {
                        StartStopSending();
                        GlobalStates._endtransmissingstatus = true;
                        ConnectBtn.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(130, 154, 165, 54));
                        GlobalStates._lastdPort = GlobalStates._selectedPort;
                    }
                    if (GlobalStates._selectedPort != "пусто" && !GlobalStates._endtransmissingstatus) GlobalStates._lastdPort = GlobalStates._selectedPort;
                    RefreshPorts();
                    foreach (var port in _availablePorts)
                    {
                        if (port.PortName == GlobalStates._lastdPort)
                        {
                            GlobalStates._selectedPort = GlobalStates._lastdPort;
                            PortMenuButton.Header = GlobalStates._lastdPort;
                            if (GlobalStates._endtransmissingstatus)
                            {
                                StartStopSending();
                                GlobalStates._endtransmissingstatus = false;
                                ConnectBtn.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(33, 255, 255, 255));
                            }
                        }
                    }
                }
            }
            return IntPtr.Zero;
        }
        private void RefreshPorts()
        {
            _availablePorts = SerialPortManager.GetAvailablePorts();
            UpdatePortMenu();
        }
        private void UpdatePortMenu()
        {
            PortMenuButton.Items.Clear();
            if (_availablePorts.Count == 0)
            {
                PortMenuButton.Header = "пусто";
                var item = new MenuItem { Header = "пусто", IsEnabled = false };
                PortMenuButton.Items.Add(item);
            }
            else
            {
                foreach (var port in _availablePorts)
                {
                    var item = new MenuItem
                    {
                        Header = port.PortName,
                        Tag = port.PortName,
                        Style = (Style)Resources["SelectableMenuItemStyle"]
                    };
                    item.Click += PortMenuItem_Click;
                    PortMenuButton.Items.Add(item);
                }
                if (_availablePorts.Count > 0)
                {
                    GlobalStates._selectedPort = _availablePorts[0].PortName;
                    PortMenuButton.Header = GlobalStates._selectedPort;
                }
            }
        }
        private void PortMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string port)
            {
                GlobalStates._selectedPort = port;
                PortMenuButton.Header = port;
                if (GlobalStates._isTransmitting)
                {
                    StartStopSending();
                    StartStopSending();
                }
            }
        }
        private void InitializeUI()
        {
            RefreshPorts();

            _addresses.Add("FF");
            _addresses.Add("C8");
            var customAddresses = _configManager.LoadCustomAddresses();
            foreach (var addr in customAddresses)
            {
                if (addr != "C8" && addr != "FF")
                    _addresses.Add(addr);
            }
            UpdateAddressMenu();
        }
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(this);

            if (this.ActualWidth > 0 && this.ActualHeight > 0)
            {
                double relativeX = mousePos.X / this.ActualWidth;
                double relativeY = mousePos.Y / this.ActualHeight;
                FollowGradient.Center = new Point(relativeX, relativeY);
                FollowGradient.GradientOrigin = new Point(relativeX, relativeY);
            }
        }

        private void SliderWindow_StateChanged(object sender, EventArgs e)
        {
            MaximizeIcon = this.WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        private void MaximizeBtn_Click(object sender, RoutedEventArgs e) =>
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => this.Close();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        private void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalStates._isTransmitting) GlobalStates._endtransmissingstatus = false;
            ConnectBtn.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(33, 255, 255, 255));
            StartStopSending();
        }
        private void StartStopSending()
        {
            if (!GlobalStates._isTransmitting)
            {
                if (string.IsNullOrWhiteSpace(GlobalStates._selectedPort) || GlobalStates._selectedPort == "пусто")
                {
                    MessageBox.Show("Пожалуйста, выберите серийный порт", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    if (!int.TryParse(GlobalStates._selectedBaudRate, out int baudRate))
                    {
                        baudRate = 115200;
                    }
                    if (_portManager.OpenPort(GlobalStates._selectedPort, baudRate))
                    {
                        GlobalStates._isTransmitting = true;
                        _transmitTimer.Start();
                        ConnectBtn.Content = "Стоп";
                        System.Diagnostics.Debug.WriteLine($"Передача начата на {GlobalStates._selectedPort} ({baudRate} baud)");
                    }
                    else
                    {
                        MessageBox.Show($"Не удалось открыть порт {GlobalStates._selectedPort}", "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии порта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                _transmitTimer.Stop();
                GlobalStates._isTransmitting = false;
                _portManager.ClosePort();
                ConnectBtn.Content = "Старт";
                System.Diagnostics.Debug.WriteLine("Передача остановлена");
            }
        }
        private void TransmitTimer_Tick()
        {
            if (!GlobalStates._isTransmitting)
                return;

            try
            {
                ushort roll = (ushort)1500;
                ushort pitch = (ushort)1500;
                ushort yaw = (ushort)1500;
                ushort throttle = (ushort)1500;
                ushort[] ch = { (ushort)1500 }; //{ (ushort)Ch4Slider.Value, (ushort)Ch5Slider.Value, (ushort)Ch6Slider.Value, (ushort)Ch7Slider.Value, (ushort)Ch8Slider.Value, (ushort)Ch9Slider.Value, (ushort)Ch10Slider.Value, (ushort)Ch11Slider.Value, (ushort)Ch12Slider.Value, (ushort)Ch13Slider.Value, (ushort)Ch14Slider.Value, (ushort)Ch15Slider.Value };
                byte address = byte.Parse(GlobalStates._selectedAddress, System.Globalization.NumberStyles.HexNumber);
                _portManager.SendCRSFPacket(address, roll, pitch, yaw, throttle, ch);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при отправке: {ex.Message}");
            }
        }
        private void UpdateAddressMenu()
        {
            AddressMenuButton.Items.Clear();

            for (int i = 0; i < _addresses.Count; i++)
            {
                var address = _addresses[i];
                bool isCustom = i >= 2;

                if (isCustom)
                {
                    var dockPanel = new DockPanel();
                    var deleteBtn = new Button
                    {
                        Content = "✕",
                        Width = 24,
                        Height = 24,
                        Padding = new Thickness(0),
                        Margin = new Thickness(8, 0, 0, 0),
                        Style = (Style)this.FindResource("DeleteButtonStyle"),
                        Cursor = System.Windows.Input.Cursors.Hand
                    };
                    deleteBtn.Click += (s, e) => DeleteAddress_Click(address);
                    DockPanel.SetDock(deleteBtn, Dock.Right);
                    dockPanel.Children.Add(deleteBtn);
                    var textBlock = new TextBlock
                    {
                        Text = address,
                        Foreground = System.Windows.Media.Brushes.White,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        Margin = new Thickness(10, 0, 0, 0)
                    };
                    DockPanel.SetDock(textBlock, Dock.Left);
                    dockPanel.Children.Add(textBlock);

                    var item = new MenuItem
                    {
                        Header = dockPanel,
                        Tag = address,
                        Style = (Style)Resources["SelectableMenuItemStyle"]
                    };
                    item.Click += AddressMenuItem_Click;
                    AddressMenuButton.Items.Add(item);
                }
                else
                {
                    var item = new MenuItem
                    {
                        Header = address,
                        Tag = address,
                        Style = (Style)Resources["SelectableMenuItemStyle"]
                    };
                    item.Click += AddressMenuItem_Click;
                    AddressMenuButton.Items.Add(item);
                }
            }
            AddressMenuButton.Items.Add(new Separator());
            MenuItem addingitem = null;
            var AddDockPanel = new DockPanel();
            var addressBlock = new TextBox
            {
                Text = "AA",
                Foreground = System.Windows.Media.Brushes.White,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };
            DockPanel.SetDock(addressBlock, Dock.Left);
            AddDockPanel.Children.Add(addressBlock);
            var addBtn = new Button
            {
                Content = "+",
                Width = 24,
                Height = 24,
                Padding = new Thickness(0),
                Margin = new Thickness(8, 0, 0, 0),
                Style = (Style)this.FindResource("AddButtonStyle"),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            DockPanel.SetDock(addBtn, Dock.Right);
            AddDockPanel.Children.Add(addBtn);
            addingitem = new MenuItem
            {
                Header = AddDockPanel,
                Style = (Style)Resources["SelectableMenuItemStyle"],
                Visibility = Visibility.Collapsed
            };
            addingitem.Click += AddressMenuItem_Click;
            AddressMenuButton.Items.Add(addingitem);
            var addItem = new MenuItem
            {
                Header = "Добавить",
                Style = (Style)Resources["SelectableMenuItemStyle"]
            };
            addItem.Click += (s, ea) =>
            {
                addItem.Visibility = Visibility.Collapsed;
                addingitem.Visibility = Visibility.Visible;
                AddressMenuButton.IsSubmenuOpen = true;
            };
            AddressMenuButton.Items.Add(addItem);
            addBtn.Click += (s, ea) =>
            {
                var address = addressBlock.Text.ToUpper();
                if (!string.IsNullOrWhiteSpace(address))
                {
                    if (byte.TryParse(address, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte value))
                    {
                        _configManager.AddAddress(_addresses, address);
                        UpdateAddressMenu();
                        GlobalStates._selectedAddress = address;
                        AddressMenuButton.Header = address;
                        addItem.Visibility = Visibility.Visible;
                        addingitem.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        addressBlock.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 232, 17, 35));
                    }
                }
                AddressMenuButton.IsSubmenuOpen = true;
            };

        }
        private void DeleteAddress_Click(string address)
        {
            if (address != "FF" && address != "C8")
            {
                _configManager.RemoveAddress(_addresses, address);
                UpdateAddressMenu();
            }
        }
        private void AddressMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string address)
            {
                GlobalStates._selectedAddress = address;
                AddressMenuButton.Header = address;
            }
        }

        private void BaudMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string baud)
            {
                GlobalStates._selectedBaudRate = baud;
                BaudMenuButton.Header = baud;
                if (GlobalStates._isTransmitting)
                {
                    StartStopSending();
                    StartStopSending();
                }
            }
        }
        private void BaudBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            GlobalStates._selectedBaudRate = BaudBox.Text;
            BaudMenuButton.Header = BaudBox.Text;
            if (GlobalStates._isTransmitting)
            {
                StartStopSending();
            }
        }





    }
}