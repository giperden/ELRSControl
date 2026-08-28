using System.Windows;
using System.Windows.Input;

namespace ELRSControl
{
    public partial class Warning_window : Window
    {
        public Warning_window()
        {
            InitializeComponent();
            this.MouseMove += Window_MouseMove;
        }

        /// <summary>
        /// Конструктор с заданием сообщения ошибки
        /// </summary>
        public Warning_window(string errorMessage, string errorHeader) : this()
        {
            TextError.Text = errorMessage;
            ErrorHeader.Text = errorHeader;
        }

        /// <summary>
        /// Статический метод для удобного вызова диалога ошибок из любой части приложения
        /// </summary>
        public static void ShowError(string errorMessage, string errorHeader = "Ошибка", Window owner = null)
        {
            var warningWin = new Warning_window(errorMessage, errorHeader);

            if (owner != null)
            {
                warningWin.Owner = owner;
                warningWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                warningWin.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            warningWin.ShowDialog();
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => this.Close();

        private void OKBtn_Click(object sender, RoutedEventArgs e) => this.Close();

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(this);
            if (this.ActualWidth > 0 && this.ActualHeight > 0)
            {
                FollowGradient.Center = new Point(mousePos.X / this.ActualWidth, mousePos.Y / this.ActualHeight);
                FollowGradient.GradientOrigin = FollowGradient.Center;
            }
        }
    }
}