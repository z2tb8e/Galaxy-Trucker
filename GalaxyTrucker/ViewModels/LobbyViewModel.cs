﻿using GalaxyTrucker.Model;
using GalaxyTrucker.Network;
using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Net;
using System.Windows.Data;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GalaxyTrucker.ViewModels
{
    public class LobbyViewModel : NotifyBase
    {
        private const int DefaultPort = 11000;
        private const string DefaultIp = "192.168.1.110";
        private const string DefaultName = "Teszt";

        private readonly object _lock;
        private ObservableCollection<PlayerInfoViewModel> _connectedPlayers;
        private string _remoteIp;
        private int _remotePort;
        private string _playerName;

        private readonly GTTcpClient _client;

        #region properties

        #region shared

        public string ConnectionStatus { get; set; }

        public ObservableCollection<PlayerInfoViewModel> ConnectedPlayers
        {
            get { return _connectedPlayers; }
            set
            {
                _connectedPlayers = value;
                BindingOperations.EnableCollectionSynchronization(_connectedPlayers, _lock);
                OnPropertyChanged("ConnectedPlayers");
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
                    OnPropertyChanged("CanConnect");
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
                    OnPropertyChanged("CanConnect");
                }
            }
        }

        public string Error { get; set; }

        public bool IsReady => _client.IsReady;

        public bool ConnectInProgress { get; set; }

        public bool IsConnected => _client.IsConnected;

        public DelegateCommand ConnectCommand { get; set; }

        public DelegateCommand ReadyCommand { get; set; }

        public DelegateCommand BackToMenuCommand { get; set; }

        #endregion

        #region onlyhost

        public GTTcpListener Server { get; private set; }

        public string HostIp { get; set; }

        public bool CanStart
        {
            get
            {
                if (Server != null && !Server.NotReadyPlayers.Any())
                {
                    return true;
                }
                return false;
            }
        }

        public DelegateCommand HostCommand { get; set; }

        public DelegateCommand LaunchBuilding { get; set; }

        #endregion

        #endregion

        public event EventHandler<bool> BackToMenu;

        public event EventHandler<bool> BuildingStarted;

        public LobbyViewModel(GTTcpClient client)
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            HostIp = ipHostInfo.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).First().ToString();
            //OnPropertyChanged("HostIp");
            ConnectInProgress = false;
            _lock = new object();
            RemotePort = DefaultPort;
            RemoteIp = DefaultIp;
            PlayerName = DefaultName;
            ConnectedPlayers = new ObservableCollection<PlayerInfoViewModel>();
            _client = client;
            _client.PlayerConnected += Client_PlayerConnected;
            _client.PlayerReadied += Client_PlayerReadied;
            _client.BuildingBegun += Client_BuildingBegun;
            _client.PlayerDisconnected += Client_PlayerDisconnected;
            _client.ThisPlayerDisconnected += Client_ThisPlayerDisconnected;

            HostCommand = new DelegateCommand(param =>
            {
                Server = new GTTcpListener(RemotePort);
                Task.Factory.StartNew(() => Server.Start(), TaskCreationOptions.LongRunning);

                ConnectCommand.Execute(param);
            });

            ConnectCommand = new DelegateCommand(async param =>
            {
                try
                {
                    Error = "";
                    OnPropertyChanged("Error");
                    IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(RemoteIp), RemotePort);
                    ConnectInProgress = true;
                    OnPropertyChanged("ConnectInProgress");
                    await _client.Connect(endpoint, PlayerName);
                    ConnectionStatus = $"Csatlakozva, kapott szín: {_client.Player.ToUserString()}";
                    foreach (PlayerInfo info in _client.PlayerInfos.Values)
                    {
                        ConnectedPlayers.Add(new PlayerInfoViewModel(info));
                    }
                    OnPropertyChanged("ConnectionStatus");
                    OnPropertyChanged("Error");
                    OnPropertyChanged("IsConnected");
                    OnPropertyChanged("ConnectedPlayers");
                }
                catch (ConnectionRefusedException)
                {
                    Error = "A megadott játékhoz már nem lehet csatlakozni.";
                    OnPropertyChanged("Error");
                }
                catch (TimeoutException)
                {
                    Error = "Nem jött létre a kapcsolat az időlimiten belül.";
                    OnPropertyChanged("Error");
                }
                catch (Exception e)
                {
                    Error = $"Hiba a csatlakozás közben:\n{e.Message}";
                    OnPropertyChanged("Error");
                }
                finally
                {
                    ConnectInProgress = false;
                    OnPropertyChanged("ConnectInProgress");
                }
            });

            ReadyCommand = new DelegateCommand(param =>
            {
                try
                {
                    _client.ToggleReady(ServerStage.Lobby);
                    ConnectedPlayers.Where(info => info.Name == PlayerName).First().IsReady = _client.IsReady;
                    OnPropertyChanged("ConnectedPlayers");
                    OnPropertyChanged("IsReady");
                    OnPropertyChanged("CanStart");
                }
                catch (Exception e)
                {
                    Error = $"Hiba a szerverrel való kommunikáció közben:\n{e.Message}";
                    OnPropertyChanged("Error");
                }
            });

            BackToMenuCommand = new DelegateCommand(param =>
            {
                BackToMenu?.Invoke(this, Server != null);
            });
        }

        private void Client_ThisPlayerDisconnected(object sender, EventArgs e)
        {
            Error = "A szerverrel való kapcsolat megszakadt.";
            OnPropertyChanged("Error");
            OnPropertyChanged("IsConnected");
        }

        private void Client_PlayerDisconnected(object sender, PlayerDisconnectedEventArgs e)
        {
            PlayerInfoViewModel playerToRemove = ConnectedPlayers.Where(item => item.Color == e.Color).First();
            ConnectedPlayers.Remove(playerToRemove);
            OnPropertyChanged("ConnectedPlayers");
        }

        private void Client_BuildingBegun(object sender, BuildingBegunEventArgs e)
        {
            BuildingStarted?.Invoke(this, Server != null);
        }

        private void Client_PlayerReadied(object sender, PlayerReadiedEventArgs e)
        {
            ConnectedPlayers.Where(info => info.Color == e.Player).First().IsReady = _client.PlayerInfos[e.Player].IsReady;
            OnPropertyChanged("ConnectedPlayers");
            OnPropertyChanged("CanStart");
        }

        private void Client_PlayerConnected(object sender, PlayerConnectedEventArgs e)
        {
            ConnectedPlayers.Add(new PlayerInfoViewModel(new PlayerInfo(e.Color, e.PlayerName, false)));
        }
    }
}