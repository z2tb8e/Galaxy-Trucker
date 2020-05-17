using GalaxyTrucker.Model;
using GalaxyTrucker.Model.PartTypes;
using GalaxyTrucker.Network;
using GalaxyTrucker.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace GalaxyTrucker.ViewModels
{
    public class FlightViewModel : NotifyBase
    {
        private readonly object _shipPartsLock;
        private readonly object _playerOrderLock;
        private readonly GTTcpClient _client;
        private readonly PlayerListViewModel _playerList;
        private readonly Ship _ship;
        private ObservableCollection<FlightPartViewModel> _shipParts;
        private ObservableCollection<OrderFieldViewModel> _playerOrderFields;

        public PlayerListViewModel PlayerList
        {
            get
            {
                return _playerList;
            }
        }

        public ObservableCollection<OrderFieldViewModel> PlayerOrderFields
        {
            get
            {
                return _playerOrderFields;
            }
            private set
            {
                _playerOrderFields = value;
                BindingOperations.EnableCollectionSynchronization(_playerOrderFields, _playerOrderLock);
                OnPropertyChanged();
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
                BindingOperations.EnableCollectionSynchronization(_shipParts, _shipPartsLock);
                OnPropertyChanged();
            }
        }

        public ObservableCollection<PlayerAttributes> PlayerAttributes
        {
            get
            {
                return new ObservableCollection<PlayerAttributes>(
                    _client.PlayerInfos.Values.Where(info => info.Color != _client.Player).Select(info => info.Attributes)
                    );
            }
        }

        public FlightViewModel(GTTcpClient client, PlayerListViewModel playerList, Ship ship)
        {
            _shipPartsLock = new object();
            _playerOrderLock = new object();
            _client = client;
            _playerList = playerList;
            _ship = ship;

            ShipParts = new ObservableCollection<FlightPartViewModel>();
            foreach(Part p in _ship.Parts)
            {
                ShipParts.Add(new FlightPartViewModel(p));
            }

            _playerList.LostConnection += PlayerList_LostConnection;

            _client.PlacesChanged += new EventHandler((sender, e) => RefreshTokens());

            InitializeOrderFields();
        }

        private void PlayerList_LostConnection(object sender, EventArgs e)
        {
            UnsubscribeFromEvents();
        }

        private void UnsubscribeFromEvents()
        {
            _playerList.LostConnection -= PlayerList_LostConnection;
        }

        /// <summary>
        /// Method to setup the basic 7*15 hollow square of fields, 
        /// with the first element being the one in the middle on the top, going clockwise
        /// </summary>
        private void InitializeOrderFields()
        {
            //Fill up the collection in that order, to make later use easier
            PlayerOrderFields = new ObservableCollection<OrderFieldViewModel>();
            for(int i = 7; i <= 14; ++i)
            {
                PlayerOrderFields.Add(new OrderFieldViewModel(0, i));
            }
            for(int j = 1; j <= 6; ++j)
            {
                PlayerOrderFields.Add(new OrderFieldViewModel(j, 14));
            }
            for(int i = 13; i >= 0; --i)
            {
                PlayerOrderFields.Add(new OrderFieldViewModel(6, i));
            }
            for(int j = 5; j >= 0; --j)
            {
                PlayerOrderFields.Add(new OrderFieldViewModel(j, 0));
            }
            for(int i = 0; i < 7; ++i)
            {
                PlayerOrderFields.Add(new OrderFieldViewModel(0, i));
            }

            //Put on the tokens
            RefreshTokens();
        }

        private void RefreshTokens()
        {
            foreach(OrderFieldViewModel field in PlayerOrderFields)
            {
                field.Token = null;
            }
            foreach(var pair in _client.PlaceProperties)
            {
                Image token = pair.Key switch
                {
                    PlayerColor.Blue => Resources.token_blue,
                    PlayerColor.Green => Resources.token_green,
                    PlayerColor.Red => Resources.token_red,
                    _ => Resources.token_yellow
                };
                PlayerOrderFields[pair.Value.PlaceValue].Token = token;
            }
        }
    }
}
