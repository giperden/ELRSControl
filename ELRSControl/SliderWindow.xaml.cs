using ELRSControl.ViewModels;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ELRSControl
{
    public partial class SliderWindow : Window, INotifyPropertyChanged
    {
        private string _maximizeIcon = "\uE922";

        public MainViewModel ViewModel => App.SharedViewModel;

        public string MaximizeIcon
        {
            get => _maximizeIcon;
            set { _maximizeIcon = value; OnPropertyChanged(); }
        }

        public SliderWindow()
        {
            InitializeComponent();
            this.DataContext = ViewModel;

            this.StateChanged += (s, e) => MaximizeIcon = this.WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
            this.Loaded += SliderWindow_Loaded;
            this.MouseMove += Window_MouseMove;
        }

        private void SliderWindow_Loaded(object sender, RoutedEventArgs e)
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
        private void CloseBtn_Click(object sender, RoutedEventArgs e) => this.Close();
        private void MainWindow_Click(object sender, RoutedEventArgs e)
        {
            App.ShowMainWindow();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}