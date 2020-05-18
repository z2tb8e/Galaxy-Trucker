using GalaxyTrucker.Model;
using GalaxyTrucker.Network;
using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GalaxyTrucker.ViewModels
{
    public class LobbyViewModel : NotifyBase
    {
        private const int DefaultPort = 11000;
        private const string DefaultIp = "192.168.1.110";
        private const string DefaultName = "Teszt";

        private readonly PlayerListViewModel _playerList;
        private ObservableCollection<ShipLayout> _layoutOptions;
        private ShipLayout _selectedLayout;
        private GameStage _selectedGameStage;
        private string _remoteIp;
        private int _remotePort;
        private string _playerName;
        private string _error;
        private bool _connectInProgress;
        private string _connectionStatus;

        private readonly GTTcpClient _client;

        #region properties

        #region shared

        public PlayerListViewModel PlayerList { get { return _playerList; } }

        public ObservableCollection<ShipLayout> LayoutOptions
        {
            get
            {
                return _layoutOptions;
            }
            set
            {
                if (_layoutOptions != value)
                {
                    _layoutOptions = value;
                    OnPropertyChanged();
                }
            }
        }

        public ShipLayout SelectedLayout
        {
            get
            {
                return _selectedLayout;
            }
            set
            {
                _selectedLayout = value;
                OnPropertyChanged();
            }
        }

        public string ConnectionStatus
        {
            get
            {
                return _connectionStatus;
            }
            set
            {
                _connectionStatus = value;
                OnPropertyChanged();
            }
        }

        public string RemoteIp
        {
            get
            {
                return _remoteIp;
            }
            set
            {
                if (_remoteIp != value)
                {
                    _remoteIp = value;
                    OnPropertyChanged();
                }
            }
        }

        public int RemotePort
        {
            get
            {
                return _remotePort;
            }
            set
            {
                if (_remotePort != value)
                {
                    _remotePort = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PlayerName
        {
            get
            {
                return _playerName;
            }
            set
            {
                if (_playerName != value)
                {

                    _playerName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Error
        {
            get
            {
                return _error;
            }
            set
            {
                _error = value;
                OnPropertyChanged();
            }
        }

        public bool ConnectInProgress
        {
            get
            {
                return _connectInProgress;
            }
            set
            {
                _connectInProgress = value;
                OnPropertyChanged();
            }
        }

        public bool IsConnected => _client.IsConnected;

        public DelegateCommand ConnectCommand { get; set; }

        public DelegateCommand ReadyCommand { get; set; }

        public DelegateCommand BackToMenuCommand { get; set; }

        #endregion

        #region onlyhost

        public GameStage SelectedGameStage
        {
            get
            {
                return _selectedGameStage;
            }
            set
            {
                _selectedGameStage = value;
                OnPropertyChanged();
            }
        }

        public GTTcpListener Server { get; private set; }

        public string HostIp { get; set; }

        public DelegateCommand HostCommand { get; set; }

        public DelegateCommand LaunchBuildingCommand { get; set; }

        #endregion

        #endregion

        public event EventHandler<bool> BackToMenu;

        public event EventHandler<bool> BuildingStarted;

        public LobbyViewModel(GTTcpClient client, PlayerListViewModel playerList)
        {
            SelectedGameStage = GameStage.First;
            _playerList = playerList;
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            HostIp = ipHostInfo.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).First().ToString();
            ConnectInProgress = false;
            RemotePort = DefaultPort;
            RemoteIp = DefaultIp;
            PlayerName = DefaultName;
            _client = client;
            _client.BuildingBegun += Client_BuildingBegun;
            _playerList.LostConnection += PlayerList_LostConnection;

            HostCommand = new DelegateCommand(param => Server == null, param =>
            {
                Server = new GTTcpListener(RemotePort, SelectedGameStage);
                Task.Factory.StartNew(() => Server.Start(), TaskCreationOptions.RunContinuationsAsynchronously | TaskCreationOptions.LongRunning);
                RemoteIp = "127.0.0.1";
                ConnectCommand.Execute(null);
            });

            ConnectCommand = new DelegateCommand(param => !ConnectInProgress && !IsConnected, param => Connect());

            ReadyCommand = new DelegateCommand(param =>
            {
                try
                {
                    _client.ToggleReady(ServerStage.Lobby);
                }
                catch (Exception e)
                {
                    Error = $"Hiba a szerverrel való kommunikáció közben:\n{e.Message}";
                }
            });

            BackToMenuCommand = new DelegateCommand(param =>
            {
                UnsubscribeFromEvents();
                BackToMenu?.Invoke(this, Server != null);
            });

            LaunchBuildingCommand = new DelegateCommand(param => Server != null && !Server.NotReadyPlayers.Any() && _client.PlayerInfos.Count > 1,
                param =>
            {
                Task.Factory.StartNew(() => Server.StartBuildStage(), TaskCreationOptions.LongRunning);
            });
        }

        private async void Connect()
        {
            try
            {
                Error = "";
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(RemoteIp), RemotePort);
                ConnectInProgress = true;
                Error = "Csatlakozás folyamatban...";
                await _client.Connect(endpoint, PlayerName);
                ConnectionStatus = $"Csatlakozva, kapott szín: {EnumHelpers.GetDescription(_client.Player)}\nJáték fázis: {EnumHelpers.GetDescription(_client.GameStage)}";
                Error = "";
                _playerList.SynchronizeListWithClient();
                SelectedGameStage = _client.GameStage;
                if(SelectedGameStage == GameStage.First)
                {
                    SelectedLayout = ShipLayout.Small;
                }
                else if(SelectedGameStage == GameStage.Second)
                {
                    SelectedLayout = ShipLayout.Medium;
                }
                else
                {
                    LayoutOptions = new ObservableCollection<ShipLayout>()
                    {
                        ShipLayout.BigLong,
                        ShipLayout.BigWide
                    };
                    SelectedLayout = ShipLayout.BigWide;
                }
                OnPropertyChanged(nameof(IsConnected));
            }
            catch (ConnectionRefusedException)
            {
                Error = "A megadott játékhoz már nem lehet csatlakozni.";
            }
            catch (TimeoutException)
            {
                Error = "Nem jött létre a kapcsolat az időlimiten belül.";
            }
            catch (Exception e)
            {
                Error = $"Hiba a csatlakozás közben:\n{e.Message}";
            }
            finally
            {
                ConnectInProgress = false;
            }
        }

        private void PlayerList_LostConnection(object sender, EventArgs e)
        {
            UnsubscribeFromEvents();
        }

        private void Client_BuildingBegun(object sender, EventArgs e)
        {
            UnsubscribeFromEvents();
            BuildingStarted?.Invoke(this, Server != null);
        }

        private void UnsubscribeFromEvents()
        {
            _client.BuildingBegun -= Client_BuildingBegun;
            _playerList.LostConnection -= PlayerList_LostConnection;
        }
    }
}
