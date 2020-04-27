using GalaxyTrucker.Model;
using GalaxyTrucker.Network;
using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Net;
using System.Windows.Data;
using System.Windows;

namespace GalaxyTrucker.ViewModels
{
    public class ConnectViewModel : NotifyBase
    {
        private const int DefaultPort = 11000;

        private readonly object _lock;
        private ObservableCollection<PlayerInfo> _connectedPlayers;
        private string _remoteIp;
        private int _remotePort;
        private string _playerName;

        private readonly GTTcpClient _client;
        public string ConnectionStatus { get; set; }

        public ObservableCollection<PlayerInfo> ConnectedPlayers
        {
            get { return _connectedPlayers; }
            set
            {
                _connectedPlayers = value;
                BindingOperations.EnableCollectionSynchronization(_connectedPlayers, _lock);
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

        public bool IsConnected => _client.IsConnected;

        public DelegateCommand Connect { get; set; }

        public DelegateCommand Ready { get; set; }

        public ConnectViewModel()
        {
            _lock = new object();
            RemotePort = DefaultPort;
            _client = new GTTcpClient();
            _client.PlayerConnected += Client_PlayerConnected;
            _client.PlayerReadied += Client_PlayerReadied;
            Connect = new DelegateCommand(param =>
            {
                try
                {
                    Error = "";
                    IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(RemoteIp), RemotePort);
                    _client.Connect(endpoint, PlayerName);
                    ConnectionStatus = $"Csatlakozva, kapott szín: {_client.Player.ToUserString()}";
                    ConnectedPlayers ??= new ObservableCollection<PlayerInfo>();
                    foreach(PlayerInfo info in _client.PlayerInfos.Values)
                    {
                        ConnectedPlayers.Add(info);
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
                catch(TimeoutException)
                {
                    Error = "Nem jött létre a kapcsolat az időlimiten belül.";
                    OnPropertyChanged("Error");
                }
                catch (Exception e)
                {
                    Error = $"Hiba a csatlakozás közben: {e.Message}";
                    OnPropertyChanged("Error");
                }
            });
        }

        private void Client_PlayerReadied(object sender, PlayerReadiedEventArgs e)
        {
            PlayerInfo info = _connectedPlayers.Where(info => info.Color == e.Player).First();
            info.IsReady = !info.IsReady;
            OnPropertyChanged("ConnectedPlayers");
        }

        private void Client_PlayerConnected(object sender, PlayerConnectedEventArgs e)
        {
            ConnectedPlayers.Add(new PlayerInfo(e.Color, e.PlayerName, false));
            OnPropertyChanged("ConnectedPlayers");
        }
    }
}
