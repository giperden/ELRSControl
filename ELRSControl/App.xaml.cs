using System.Configuration;
using System.Data;
using System.Windows;
using ELRSControl.ViewModels;

namespace ELRSControl
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static MainViewModel SharedViewModel { get; } = new MainViewModel();
    }

}