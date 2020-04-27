using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GalaxyTrucker.Model;
using GalaxyTrucker.Model.PartTypes;
using GalaxyTrucker.ViewModels;
using System.IO;
using GalaxyTrucker.Views;
using GalaxyTrucker.Network;

namespace GalaxyTrucker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Startup += App_Startup;
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            ConnectViewModel connectViewModel = new ConnectViewModel();
            ConnectWindow connectWindow = new ConnectWindow
            {
                DataContext = connectViewModel
            };
            connectWindow.Show();
        }
    }
}
