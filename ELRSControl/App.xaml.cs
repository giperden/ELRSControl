using System.Linq;
using System.Windows;
using ELRSControl.ViewModels;

namespace ELRSControl
{
    public partial class App : Application
    {
        public static MainViewModel SharedViewModel { get; } = new MainViewModel();

        /// <summary>
        /// Находит существующее MainWindow или создает новое
        /// </summary>
        public static void ShowMainWindow()
        {
            var window = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();

            if (window == null)
            {
                window = new MainWindow();
                window.Show();
            }
            else
            {
                if (window.WindowState == WindowState.Minimized)
                    window.WindowState = WindowState.Normal;

                window.Activate();
            }
        }

        /// <summary>
        /// Находит существующее SliderWindow или создает новое
        /// </summary>
        public static void ShowSliderWindow()
        {
            var window = Application.Current.Windows.OfType<SliderWindow>().FirstOrDefault();

            if (window == null)
            {
                window = new SliderWindow();
                window.Show();
            }
            else
            {
                if (window.WindowState == WindowState.Minimized)
                    window.WindowState = WindowState.Normal;

                window.Activate();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SharedViewModel.StopTransmissionOnClose();
            base.OnExit(e);
        }
    }
}