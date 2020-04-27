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
        private const int DefaultPort = 11000;

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

        public string Error { get; set; }

        public bool IsConnected => _client.IsConnected;

        public DelegateCommand Connect { get; set; }

        public DelegateCommand Ready { get; set; }

        public ConnectViewModel()
        {
            RemotePort = DefaultPort;
            _client = new GTTcpClient();
            Connect = new DelegateCommand(param =>
            {
                try
                {
                    Error = "";
                    IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(RemoteIp), RemotePort);
                    _client.Connect(endpoint, PlayerName);
                    ConnectionStatus = $"Csatlakozva, kapott szín: {_client.Player.ToUserString()}";
                    OnPropertyChanged("ConnectionStatus");
                    OnPropertyChanged("Error");
                    OnPropertyChanged("IsConnected");
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
    }
}
