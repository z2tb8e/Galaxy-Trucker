using System;
using System.Windows;
using GalaxyTrucker.Network;
using GalaxyTrucker.ViewModels;
using GalaxyTrucker.Views;

namespace GalaxyTrucker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private MainWindow _mainWindow;

        public App()
        {
            Startup += App_Startup;
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            _mainWindow = new MainWindow();
            Menu(null, null);
        }

        private void Menu(object sender, EventArgs e)
        {

            MenuControl menuControl = new MenuControl();
            menuControl.ConnectClick += Menu_JoinGame;
            menuControl.HostClick += Menu_HostGame;
            menuControl.RulesClick += Menu_Rules;
            _mainWindow.Content = menuControl;
            _mainWindow.Show();
        }

        private void Menu_JoinGame(object sender, EventArgs e)
        {
            GTTcpClient client = new GTTcpClient();
            ConnectViewModel connectViewModel = new ConnectViewModel(client);
            connectViewModel.BackToMenu += Menu;
            ConnectControl connectControl = new ConnectControl
            {
                DataContext = connectViewModel
            };
            _mainWindow.Content = connectControl;
        }

        private void Menu_HostGame(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Menu_Rules(object sender, EventArgs e)
        {
            RulesControl rulesControl = new RulesControl();
            rulesControl.BackToMenu += Menu;
            _mainWindow.Content = rulesControl;
        }
    }
}
