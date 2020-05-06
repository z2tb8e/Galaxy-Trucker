using System;
using System.Threading;
using System.Windows;
using GalaxyTrucker.Model;
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
        const int BaseHeight = 450;
        const int BaseWidth = 800;

        private MainWindow _mainWindow;
        private GTTcpClient _client;
        private GTTcpListener _listener;
        private LobbyViewModel _connectViewModel;
        private BuildViewModel _buildViewModel;

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
            _client = new GTTcpClient();
            _connectViewModel = new LobbyViewModel(_client);
            _connectViewModel.BackToMenu += ConnectViewModel_BackToMenu;
            _connectViewModel.BuildingStarted += ConnectViewModel_BuildingStarted;
            ConnectControl connectControl = new ConnectControl
            {
                DataContext = _connectViewModel
            };
            _mainWindow.Content = connectControl;
        }

        private void ConnectViewModel_BackToMenu(object sender, bool isHost)
        {
            _client.Close();
            if (isHost)
            {
                _connectViewModel.Server.Close();
            }
            Menu(null, null);
        }

        private void Menu_HostGame(object sender, EventArgs e)
        {
            _client = new GTTcpClient();
            _connectViewModel = new LobbyViewModel(_client);
            _connectViewModel.BackToMenu += ConnectViewModel_BackToMenu;
            _connectViewModel.BuildingStarted += ConnectViewModel_BuildingStarted;
            HostControl hostControl = new HostControl
            {
                DataContext = _connectViewModel
            };
            _mainWindow.Content = hostControl;
        }

        private void ConnectViewModel_BuildingStarted(object sender, bool isHost)
        {
            if (isHost)
            {
                _listener = _connectViewModel.Server;
            }
            Dispatcher.Invoke(() =>
            {
                _buildViewModel = new BuildViewModel(_client, _connectViewModel.ConnectedPlayers, ShipLayout.Small);
                _buildViewModel.FatalErrorOccured += BuildViewModel_FatalErrorOccured;
                BuildControl buildControl = new BuildControl
                {
                    DataContext = _buildViewModel
                };
                _mainWindow.Content = buildControl;
                Current.MainWindow.WindowState = WindowState.Maximized;
                Current.MainWindow.WindowStyle = WindowStyle.None;
            });
        }

        private void BuildViewModel_FatalErrorOccured(object sender, EventArgs e)
        {
            _client.Close();
            if(_listener != null)
            {
                _listener.Close();
            }
            Menu(null, null);
        }

        private void Menu_Rules(object sender, EventArgs e)
        {
            RulesControl rulesControl = new RulesControl();
            rulesControl.BackToMenu += Menu;
            _mainWindow.Content = rulesControl;
            
        }
    }
}
