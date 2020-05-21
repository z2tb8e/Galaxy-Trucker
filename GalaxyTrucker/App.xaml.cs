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
        #region fields

        private MainWindow _mainWindow;
        private GTTcpClient _client;
        private GTTcpListener _listener;
        private PlayerListViewModel _playerListViewModel;
        private LobbyViewModel _lobbyViewModel;
        private BuildViewModel _buildViewModel;
        private FlightViewModel _flightViewModel;

        #endregion

        public App()
        {
            Startup += App_Startup;
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            _mainWindow = new MainWindow();
            Menu();
        }

        private void Menu()
        {
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.WindowStyle = WindowStyle.SingleBorderWindow;
            MenuControl menuControl = new MenuControl();
            menuControl.ConnectClick += Menu_JoinGame;
            menuControl.HostClick += Menu_HostGame;
            _mainWindow.Content = menuControl;
            _mainWindow.Show();
        }

        #region menu event handlers

        private void Menu_HostGame(object sender, EventArgs e)
        {
            _client = new GTTcpClient();
            _playerListViewModel = new PlayerListViewModel(_client);
            _playerListViewModel.LostConnection += PlayerListViewModel_LostConnection;

            _lobbyViewModel = new LobbyViewModel(_client, _playerListViewModel);
            _lobbyViewModel.BackToMenu += LobbyViewModel_BackToMenu;
            _lobbyViewModel.BuildingStarted += LobbyViewModel_BuildingStarted;
            HostControl hostControl = new HostControl
            {
                DataContext = _lobbyViewModel
            };
            _mainWindow.Content = hostControl;
        }

        private void Menu_JoinGame(object sender, EventArgs e)
        {
            _client = new GTTcpClient();
            _playerListViewModel = new PlayerListViewModel(_client);
            _playerListViewModel.LostConnection += PlayerListViewModel_LostConnection;
            _lobbyViewModel = new LobbyViewModel(_client, _playerListViewModel);
            _lobbyViewModel.BackToMenu += LobbyViewModel_BackToMenu;
            _lobbyViewModel.BuildingStarted += LobbyViewModel_BuildingStarted;
            ConnectControl connectControl = new ConnectControl
            {
                DataContext = _lobbyViewModel
            };
            _mainWindow.Content = connectControl;
        }

        #endregion

        #region lobby event handlers

        private void LobbyViewModel_BackToMenu(object sender, bool isHost)
        {
            _playerListViewModel.UnsubscribeFromEvents();
            _client.Close();
            if (isHost)
            {
                _lobbyViewModel.Server.Close();
            }
            Menu();
        }

        private void LobbyViewModel_BuildingStarted(object sender, bool isHost)
        {
            if (isHost)
            {
                _listener = _lobbyViewModel.Server;
            }
            Dispatcher.Invoke(() =>
            {
                _buildViewModel = new BuildViewModel(_client, _playerListViewModel, _lobbyViewModel.SelectedLayout);
                _buildViewModel.FlightBegun += BuildViewModel_FlightBegun;
                BuildControl buildControl = new BuildControl
                {
                    DataContext = _buildViewModel
                };
                _mainWindow.Content = buildControl;
                _mainWindow.WindowState = WindowState.Maximized;
                _mainWindow.WindowStyle = WindowStyle.None;
            });
        }

        #endregion

        #region misc event handlers

        private void PlayerListViewModel_LostConnection(object sender, EventArgs e)
        {
            _client.Close();
            if (_listener != null)
            {
                _listener.Close();
            }
            MessageBox.Show("A szerverrel való kapcsolat megszakadt!\n");
            Dispatcher.Invoke(Menu);
        }

        private void BuildViewModel_FlightBegun(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _flightViewModel = new FlightViewModel(_client, _playerListViewModel, _buildViewModel.Ship);
                _flightViewModel.GameEnded += FlightViewModel_GameEnded;
                FlightControl flightControl = new FlightControl
                {
                    DataContext = _flightViewModel
                };
                _mainWindow.Content = flightControl;
            });
        }

        private void FlightViewModel_GameEnded(object sender, EventArgs e)
        {
            Dispatcher.Invoke(Menu);
        }

        #endregion
    }
}
