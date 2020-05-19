﻿using GalaxyTrucker.Network;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;

namespace GalaxyTrucker.ViewModels
{
    public class PlayerListViewModel : NotifyBase
    {
        protected readonly GTTcpClient _client;
        private readonly object _lock;
        private ObservableCollection<PlayerInfoViewModel> _connectedPlayers;

        public ObservableCollection<PlayerInfoViewModel> ConnectedPlayers
        {
            get { return _connectedPlayers; }
            private set
            {
                _connectedPlayers = value;
                BindingOperations.EnableCollectionSynchronization(_connectedPlayers, _lock);
                OnPropertyChanged();
            }
        }

        public event EventHandler LostConnection;

        public PlayerListViewModel(GTTcpClient client)
        {
            _lock = new object();
            ConnectedPlayers = new ObservableCollection<PlayerInfoViewModel>();
            _client = client;
            _client.PlayerConnected += Client_PlayerConnected;
            _client.PlayerReadied += Client_PlayerReadied;
            _client.ThisPlayerReadied += Client_ThisPlayerReadied;
            _client.PlayerDisconnected += Client_PlayerDisconnected;
            _client.ThisPlayerDisconnected += Client_ThisPlayerDisconnected;
        }

        public void SynchronizeListWithClient()
        {
            ConnectedPlayers.Clear();
            foreach(PlayerInfo info in _client.PlayerInfos.Values)
            {
                ConnectedPlayers.Add(new PlayerInfoViewModel(info));
            }
        }

        public void UnsubscribeFromEvents()
        {
            _client.PlayerConnected -= Client_PlayerConnected;
            _client.PlayerReadied -= Client_PlayerReadied;
            _client.PlayerDisconnected -= Client_PlayerDisconnected;
            _client.ThisPlayerDisconnected -= Client_ThisPlayerDisconnected;
        }

        private void Client_ThisPlayerReadied(object sender, EventArgs e)
        {
            ConnectedPlayers.First(info => info.Color == _client.Player).IsReady = _client.IsReady;
        }

        private void Client_ThisPlayerDisconnected(object sender, EventArgs e)
        {
            UnsubscribeFromEvents();
            LostConnection?.Invoke(this, EventArgs.Empty);
        }

        private void Client_PlayerDisconnected(object sender, PlayerEventArgs e)
        {
            PlayerInfoViewModel playerToRemove = ConnectedPlayers.First(item => item.Color == e.Player);
            ConnectedPlayers.Remove(playerToRemove);
        }

        private void Client_PlayerReadied(object sender, PlayerEventArgs e)
        {
            ConnectedPlayers.First(info => info.Color == e.Player).IsReady = _client.PlayerInfos[e.Player].IsReady;
        }

        private void Client_PlayerConnected(object sender, PlayerConnectedEventArgs e)
        {
            ConnectedPlayers.Add(new PlayerInfoViewModel(new PlayerInfo(e.Color, e.PlayerName, false)));
        }
    }
}
