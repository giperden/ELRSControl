using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using ELRSControl.Services;
using ELRSControl.Models;
using System.Linq;
using System.Windows.Threading;

namespace ELRSControl
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _maximizeIcon = "\uE922";
        private SerialPortManager _portManager = new();
        private ConfigManager _configManager = new();
        private ObservableCollection<SerialPortInfo> _availablePorts = new();
        private ObservableCollection<string> _addresses = new();
        private string _selectedPort = "COM9";
        private string _selectedBaudRate = "115200";
        private string _selectedAddress = "FF";

        private bool _isTransmitting = false;
        private DispatcherTimer _transmitTimer;
        private Button _startStopButton;

        public string MaximizeIcon
        {
            get => _maximizeIcon;
            set { _maximizeIcon = value; OnPropertyChanged(); }
        }

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            this.StateChanged += MainWindow_StateChanged;

            _transmitTimer = new DispatcherTimer();
            _transmitTimer.Interval = TimeSpan.FromMilliseconds(20);
            _transmitTimer.Tick += (s, e) => TransmitTimer_Tick();

            InitializeUI();
        }

        private void InitializeUI()
        {
            RefreshPorts();

            _addresses.Add("FF");
            _addresses.Add("C8");
            var customAddresses = _configManager.LoadCustomAddresses();
            foreach (var addr in customAddresses)
            {
                _addresses.Add(addr);
            }

            UpdateAddressMenu();

            LeftJoystick.PropertyChanged += LeftJoystick_PropertyChanged;
            RightJoystick.PropertyChanged += RightJoystick_PropertyChanged;
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
                PortMenuButton.Header = "COM9";
                var item = new MenuItem { Header = "No ports", IsEnabled = false };
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
                    _selectedPort = _availablePorts[0].PortName;
                    PortMenuButton.Header = _selectedPort;
                }
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
                        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(32, 255, 255, 255)),
                        Foreground = System.Windows.Media.Brushes.White,
                        FontSize = 14,
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
            var addItem = new MenuItem
            {
                Header = "Добавить",
                Style = (Style)Resources["SelectableMenuItemStyle"]
            };
            addItem.Click += AddAddressBtn_Click;
            AddressMenuButton.Items.Add(addItem);
        }

        private void DeleteAddress_Click(string address)
        {
            if (address != "FF" && address != "C8")
            {
                _configManager.RemoveAddress(_addresses, address);
                UpdateAddressMenu();
            }
        }

        private void PortMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string port)
            {
                _selectedPort = port;
                PortMenuButton.Header = port;
            }
        }

        private void BaudMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string baud)
            {
                _selectedBaudRate = baud;
                BaudMenuButton.Header = baud;
            }
        }

        private void AddressMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string address)
            {
                _selectedAddress = address;
                AddressMenuButton.Header = address;
            }
        }

        private void AddAddressBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "Добавить адрес CRSF",
                Width = 300,
                Height = 150,
                WindowStyle = WindowStyle.SingleBorderWindow,
                Background = System.Windows.Media.Brushes.LightGray
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };
            var textBlock = new TextBlock { Text = "Введите адрес (HEX):", Margin = new Thickness(0, 0, 0, 10) };
            var textBox = new TextBox { Height = 30, Margin = new Thickness(0, 0, 0, 10) };

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okBtn = new Button { Content = "OK", Width = 80, Margin = new Thickness(5) };
            var cancelBtn = new Button { Content = "Отмена", Width = 80, Margin = new Thickness(5) };

            okBtn.Click += (s, ea) =>
            {
                var address = textBox.Text.ToUpper();
                if (!string.IsNullOrWhiteSpace(address))
                {
                    _configManager.AddAddress(_addresses, address);
                    UpdateAddressMenu();
                    _selectedAddress = address;
                    AddressMenuButton.Header = address;
                }
                dialog.Close();
            };

            cancelBtn.Click += (s, ea) => dialog.Close();

            buttonPanel.Children.Add(okBtn);
            buttonPanel.Children.Add(cancelBtn);

            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(buttonPanel);

            dialog.Content = stackPanel;
            dialog.ShowDialog();
        }

        private void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!_isTransmitting)
            {
                if (string.IsNullOrWhiteSpace(_selectedPort) || _selectedPort == "No ports")
                {
                    MessageBox.Show("Пожалуйста, выберите серийный порт", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    if (!int.TryParse(_selectedBaudRate, out int baudRate))
                    {
                        baudRate = 115200;
                    }

                    if (_portManager.OpenPort(_selectedPort, baudRate))
                    {
                        _isTransmitting = true;
                        _transmitTimer.Start();
                        ConnectBtn.Content = "Стоп";
                        System.Diagnostics.Debug.WriteLine($"Передача начата на {_selectedPort} ({baudRate} baud)");
                    }
                    else
                    {
                        MessageBox.Show($"Не удалось открыть порт {_selectedPort}", "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Error);
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
                _isTransmitting = false;
                _portManager.ClosePort();
                ConnectBtn.Content = "Старт";
                System.Diagnostics.Debug.WriteLine("Передача остановлена");
            }
        }

        private void LeftJoystick_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "XValue")
                RollTextBox.Text = LeftJoystick.XValue.ToString();
            else if (e.PropertyName == "YValue")
                PitchTextBox.Text = LeftJoystick.YValue.ToString();
        }

        private void RightJoystick_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "XValue")
                YawTextBox.Text = RightJoystick.XValue.ToString();
            else if (e.PropertyName == "YValue")
                ThrottleTextBox.Text = RightJoystick.YValue.ToString();
        }

        private void RollTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (LeftJoystick != null && int.TryParse(RollTextBox.Text, out int value))
                LeftJoystick.XValue = value;
        }

        private void PitchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (LeftJoystick != null && int.TryParse(PitchTextBox.Text, out int value))
                LeftJoystick.YValue = value;
        }

        private void YawTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (RightJoystick != null && int.TryParse(YawTextBox.Text, out int value))
                RightJoystick.XValue = value;
        }

        private void ThrottleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (RightJoystick != null && int.TryParse(ThrottleTextBox.Text, out int value))
                RightJoystick.YValue = value;
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

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            MaximizeIcon = this.WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        private void MaximizeBtn_Click(object sender, RoutedEventArgs e) =>
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => this.Close();

        private void TransmitTimer_Tick()
        {
            if (!_isTransmitting)
                return;

            try
            {
                ushort roll = (ushort)LeftJoystick.XValue;
                ushort pitch = (ushort)LeftJoystick.YValue;
                ushort yaw = (ushort)RightJoystick.XValue;
                ushort throttle = (ushort)RightJoystick.YValue;

                byte address = byte.Parse(_selectedAddress, System.Globalization.NumberStyles.HexNumber);

                _portManager.SendCRSFPacket(address, roll, pitch, yaw, throttle);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при отправке: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
