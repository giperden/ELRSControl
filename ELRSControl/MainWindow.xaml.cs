using ELRSControl.ViewModels;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ELRSControl
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _maximizeIcon = "\uE922";

        public MainViewModel ViewModel => App.SharedViewModel;

        public string MaximizeIcon
        {
            get => _maximizeIcon;
            set { _maximizeIcon = value; OnPropertyChanged(); }
        }

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = ViewModel;

            this.StateChanged += (s, e) => MaximizeIcon = this.WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
            this.Loaded += MainWindow_Loaded;
            LeftJoystick.SetBinding(Models.JoystickControl.XValueProperty, new System.Windows.Data.Binding("Roll") { Mode = System.Windows.Data.BindingMode.TwoWay });
            LeftJoystick.SetBinding(Models.JoystickControl.YValueProperty, new System.Windows.Data.Binding("Pitch") { Mode = System.Windows.Data.BindingMode.TwoWay });
            RightJoystick.SetBinding(Models.JoystickControl.XValueProperty, new System.Windows.Data.Binding("Yaw") { Mode = System.Windows.Data.BindingMode.TwoWay });
            RightJoystick.SetBinding(Models.JoystickControl.YValueProperty, new System.Windows.Data.Binding("Throttle") { Mode = System.Windows.Data.BindingMode.TwoWay });
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            HwndSource src = HwndSource.FromHwnd(windowHandle);
            src?.AddHook(new HwndSourceHook(WndProc));
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0219) // WM_DEVICECHANGE
            {
                ViewModel.HandleDeviceChange(wParam.ToInt32());
            }
            return IntPtr.Zero;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(this);
            if (this.ActualWidth > 0 && this.ActualHeight > 0)
            {
                FollowGradient.Center = new Point(mousePos.X / this.ActualWidth, mousePos.Y / this.ActualHeight);
                FollowGradient.GradientOrigin = FollowGradient.Center;
            }
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        private void MaximizeBtn_Click(object sender, RoutedEventArgs e) => this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StopTransmissionOnClose();
            this.Close();
        }
        private void SliderWindow_Click(object sender, RoutedEventArgs e)
        {
            App.ShowSliderWindow();
        }

        private void Menu_Drop_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (RightMenu.Visibility == Visibility.Visible)
            {
                RightMenu.Visibility = Visibility.Collapsed;
                RightMenuContent.Visibility = Visibility.Collapsed;
                TopMenu.CornerRadius = new CornerRadius(10);
                System.Windows.Controls.Grid.SetColumnSpan(Joystick, 2);
                button.Content = "\uE70D";
            }
            else
            {
                RightMenu.Visibility = Visibility.Visible;
                RightMenuContent.Visibility = Visibility.Visible;
                TopMenu.CornerRadius = new CornerRadius(10, 10, 0, 10);
                System.Windows.Controls.Grid.SetColumnSpan(Joystick, 1);
                button.Content = "\uE70E";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}