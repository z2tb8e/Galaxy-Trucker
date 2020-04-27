using GalaxyTrucker.Model;
using GalaxyTrucker.Network;
using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Reflection;
using System.Windows;

namespace GalaxyTrucker.ViewModels
{
    public class ConnectViewModel : NotifyBase
    {
        private const int DefaultPort = 7880;

        private string _remoteIp;
        private int _remotePort;
        private string _playerName;

        private readonly GTTcpClient _client;
        public string ConnectionStatus { get; set; }

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

        public ObservableCollection<string> Errors { get; set; }

        public bool IsConnected => _client.IsConnected;

        public bool CanConnect
        {
            get
            {
                return _remoteIp != null && _playerName != null;
            }
        }

        public DelegateCommand Connect { get; set; }

        public DelegateCommand Ready { get; set; }

        public ConnectViewModel()
        {
            Errors = new ObservableCollection<string>();
            RemotePort = DefaultPort;
            _client = new GTTcpClient();
            Connect = new DelegateCommand(param =>
            {
                Errors.Clear();
                try
                {
                    IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(RemoteIp), RemotePort);
                    _client.Connect(endpoint, PlayerName);
                }
                catch (ConnectionRefusedException)
                {
                    Errors.Add("A megadott játékhoz már nem lehet csatlakozni.");
                    OnPropertyChanged("Errors");
                }
                catch(TimeoutException)
                {
                    Errors.Add("Nem jött létre a kapcsolat az időlimiten belül.");
                    OnPropertyChanged("Errors");
                }
                catch (Exception e)
                {
                    Errors.Add($"Hiba a csatlakozás közben: {e.Message}");
                    OnPropertyChanged("Errors");
                }
                ConnectionStatus = $"Csatlakozva, kapott szín: {_client.Player.ToUserString()}";
                OnPropertyChanged("ConnectionStatus");
            });
        }
    }
}
