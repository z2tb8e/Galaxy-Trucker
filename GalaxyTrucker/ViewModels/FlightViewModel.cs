using GalaxyTrucker.Model;
using GalaxyTrucker.Model.PartTypes;
using GalaxyTrucker.Network;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace GalaxyTrucker.ViewModels
{
    public class FlightViewModel : NotifyBase
    {
        private readonly object _lock;
        private readonly GTTcpClient _client;
        private readonly PlayerListViewModel _playerList;
        private readonly Ship _ship;
        private ObservableCollection<FlightPartViewModel> _shipParts;
        private ObservableCollection<PlayerAttributes> _playerAttributes;

        public PlayerListViewModel PlayerList
        {
            get
            {
                return _playerList;
            }
        }

        public ObservableCollection<FlightPartViewModel> ShipParts
        {
            get
            {
                return _shipParts;
            }
            private set
            {
                _shipParts = value;
                BindingOperations.EnableCollectionSynchronization(_shipParts, _lock);
                OnPropertyChanged();
            }
        }

        public ObservableCollection<PlayerAttributes> PlayerAttributes
        {
            get
            {
                return _playerAttributes;
            }
            private set
            {
                _playerAttributes = value;
                OnPropertyChanged();
            }
        }

        public FlightViewModel(GTTcpClient client, PlayerListViewModel playerList, Ship ship)
        {
            _lock = new object();
            _client = client;
            _playerList = playerList;
            _ship = ship;

            ShipParts = new ObservableCollection<FlightPartViewModel>();
            foreach(Part p in _ship.Parts)
            {
                ShipParts.Add(new FlightPartViewModel(p));
            }

            _playerList.LostConnection += PlayerList_LostConnection;
        }

        private void PlayerList_LostConnection(object sender, EventArgs e)
        {
            UnsubscribeFromEvents();
        }

        private void UnsubscribeFromEvents()
        {
            _playerList.LostConnection -= PlayerList_LostConnection;
        }
    }
}
