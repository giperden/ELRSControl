using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace ELRSControl
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class Window1 : Window, INotifyPropertyChanged
    {
        private string _maximizeIcon = "\uE922";
        public string MaximizeIcon
        {
            get => _maximizeIcon;
            set { _maximizeIcon = value; OnPropertyChanged(); }
        }

        public Window1()
        {
            InitializeComponent();

            this.DataContext = this;
            this.StateChanged += MainWindow_StateChanged;
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
