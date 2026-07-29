using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ELRSControl.Services;
using ELRSControl.Models;

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

            InitializeUI();
        }

        private void InitializeUI()
        {
            RefreshPorts();
            var baudRates = new[] { "9600", "19200", "38400", "57600", "115200" };
            BaudComboBox.ItemsSource = baudRates;
            BaudComboBox.SelectedIndex = 4;
            _addresses.Add("FF");
            _addresses.Add("C8");
            var customAddresses = _configManager.LoadCustomAddresses();
            foreach (var addr in customAddresses)
            {
                _addresses.Add(addr);
            }

            AddressComboBox.ItemsSource = _addresses;
            AddressComboBox.SelectedIndex = 0;
            LeftJoystick.PropertyChanged += LeftJoystick_PropertyChanged;
            RightJoystick.PropertyChanged += RightJoystick_PropertyChanged;
        }

        private void RefreshPorts()
        {
            _availablePorts = SerialPortManager.GetAvailablePorts();
            PortComboBox.ItemsSource = _availablePorts;

            if (_availablePorts.Count > 0)
                PortComboBox.SelectedIndex = 0;
        }

        private void PortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void BaudComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BaudComboBox.SelectedItem is string baud)
            {
            }
        }

        private void AddressComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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
                    AddressComboBox.SelectedItem = address;
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
