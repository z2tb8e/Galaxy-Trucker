﻿using GalaxyTrucker.Model;
using GalaxyTrucker.Network;
using GalaxyTrucker.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace GalaxyTrucker.ViewModels
{
    public class FlightViewModel : NotifyBase
    {
        #region fields

        private readonly object _shipPartsLock;
        private readonly object _playerOrderLock;
        private readonly object _optionsLock;
        private readonly object _popupLock;
        private readonly GTTcpClient _client;
        private readonly PlayerListViewModel _playerList;
        private readonly Ship _ship;
        private ObservableCollection<FlightPartViewModel> _shipParts;
        private ObservableCollection<OrderFieldViewModel> _playerOrderFields;
        private string _currentCardDescription;
        private string _currentCardToolTip;
        private ObservableCollection<OptionOrSubEventViewModel> _optionsOrSubEvents;
        private bool _requiresAttributes;
        private bool _isPlayersTurn;
        private volatile bool _roundResolved;
        private string _statusMessage;
        private volatile bool _isWaiting;
        private bool _gameOver;
        private ObservableCollection<string> _popupMessages;

        #endregion

        #region properties

        public ObservableCollection<string> PopupMessages
        {
            get
            {
                return _popupMessages;
            }
            set
            {
                _popupMessages = value;
                BindingOperations.EnableCollectionSynchronization(_popupMessages, _popupLock);
                OnPropertyChanged();
            }
        }

        public int ShipCash { get; set; }

        public PlayerAttributes CurrentAttributes { get; set; }

        public string StatusMessage
        {
            get
            {
                return _statusMessage;
            }
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public string CurrentCardToolTip
        {
            get
            {
                return _currentCardToolTip;
            }
            set
            {
                _currentCardToolTip = value;
                OnPropertyChanged();
            }
        }

        public bool RequiresAttributes
        {
            get
            {
                return _requiresAttributes;
            }
            set
            {
                _requiresAttributes = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<OptionOrSubEventViewModel> OptionsOrSubEvents
        {
            get
            {
                return _optionsOrSubEvents;
            }
            set
            {
                _optionsOrSubEvents = value;
                BindingOperations.EnableCollectionSynchronization(_optionsOrSubEvents, _optionsLock);
                OnPropertyChanged();
            }
        }

        public string CurrentCardDescription
        {
            get
            {
                return _currentCardDescription;
            }
            set
            {
                _currentCardDescription = value;
                OnPropertyChanged();
            }
        }

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

        public DelegateCommand SendAttributesCommand { get; set; }

        public DelegateCommand ActivatePartCommand { get; set; }

        public DelegateCommand CrashCommand { get; set; }

        public DelegateCommand ReadyCommand { get; set; }

        public DelegateCommand ContinueCommand { get; set; }

        #endregion

        #region events

        public event EventHandler GameEnded;

        #endregion

        #region ctor

        public FlightViewModel(GTTcpClient client, PlayerListViewModel playerList, Ship ship)
        {
            ShipCash = 0;
            _shipPartsLock = new object();
            _playerOrderLock = new object();
            _optionsLock = new object();
            _popupLock = new object();
            _client = client;
            _playerList = playerList;
            _ship = ship;
            _isWaiting = false;
            _gameOver = false;
            CurrentAttributes = new PlayerAttributes();
            OptionsOrSubEvents = new ObservableCollection<OptionOrSubEventViewModel>();
            ShipParts = new ObservableCollection<FlightPartViewModel>();
            PopupMessages = new ObservableCollection<string>();

            foreach (Part p in _ship.Parts)
            {
                ShipParts.Add(new FlightPartViewModel(p));
            }
            foreach (FlightPartViewModel part in ShipParts)
            {
                part.PartClickCommand = new DelegateCommand(param =>
                {
                    if (!_client.Crashed && part.Highlighted && !_gameOver)
                    {
                        _ship.ActivatePartAt(part.Row, part.Column);
                        foreach(FlightPartViewModel item in ShipParts)
                        {
                            if (item.Highlighted)
                            {
                                item.Part.Highlight();
                            }
                        }
                    }
                });
            }

            _client.CardPicked += Client_CardPicked;
            _client.PlayerCrashed += Client_PlayerCrashed;
            _client.PlayerTargeted += Client_PlayerTargeted;
            _client.OtherTargeted += Client_OtherTargeted;
            _client.CardOver += Client_CardOver;
            _client.OptionRemoved += Client_OptionRemoved;
            _client.OptionPicked += Client_OptionPicked;
            _client.FlightEnded += Client_FlightEnded;
            _client.GameEnded += Client_GameEnded;
            _playerList.LostConnection += PlayerList_LostConnection;
            _ship.PartRemoved += Ship_PartRemoved;
            _ship.Wrecked += Ship_Wrecked;
            _ship.FlightAttributesChanged += Ship_FlightAttributesChanged;
            _ship.CashChanged += Ship_CashChanged;
            _client.PlacesChanged += (sender, e) => RefreshTokens();

            SendAttributesCommand = new DelegateCommand(param => !_client.Crashed && RequiresAttributes && !_gameOver, param =>
            {
                RequiresAttributes = false;
                _client.UpdateAttributes(_ship.Firepower, _ship.Enginepower, _ship.CrewCount, _ship.StorageSize, _ship.Batteries);
            });

            CrashCommand = new DelegateCommand(param => !_client.Crashed && !_gameOver, param =>
            {
                MessageBoxResult result = MessageBox.Show("Biztos ki akarsz szállni?",
                                          "Megerősítés",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _client.CrashPlayer();
                }
            });

            ActivatePartCommand = new DelegateCommand(param => !_client.Crashed && _ship.Batteries > 0 && !_gameOver, param =>
            {
                bool any = _ship.HighlightActivatables();
                if (!any)
                {
                    AddPopUpMessage("Nincs aktiválható elem!");
                }
            });

            ReadyCommand = new DelegateCommand(param => !_client.Crashed && _roundResolved && _client.Card.IsResolved() && !_gameOver && !_isWaiting, param =>
            {
                _client.ToggleReady(ServerStage.Flight);
                _roundResolved = false;
                _ship.ResetActivatables();
            });

            ContinueCommand = new DelegateCommand(param => !_client.Crashed && _isWaiting && !_gameOver, param =>
            {
                _isWaiting = false;
                StatusMessage = "";
                _client.Card.ProceedCurrent();
            });

            Ship_FlightAttributesChanged(null, null);
            InitializeOrderFields();
        }

        #endregion

        #region ship event handlers

        private void Ship_CashChanged(object sender, EventArgs e)
        {
            ShipCash = _ship.Cash;
            OnPropertyChanged(nameof(ShipCash));
        }

        private void Ship_FlightAttributesChanged(object sender, EventArgs e)
        {
            CurrentAttributes.Firepower = _ship.Firepower;
            CurrentAttributes.Enginepower = _ship.Enginepower;
            CurrentAttributes.CrewCount = _ship.CrewCount;
            CurrentAttributes.StorageSize = _ship.StorageSize;
            CurrentAttributes.Batteries = _ship.Batteries;
            OnPropertyChanged(nameof(CurrentAttributes));
        }

        private void Ship_Wrecked(object sender, WreckedSource e)
        {
            _client.CrashPlayer();
            AddPopUpMessage($"A hajó nem tud továbbmenni: {e.GetDescription()}");
        }

        private void Ship_PartRemoved(object sender, PartRemovedEventArgs e)
        {
            FlightPartViewModel removedPart = ShipParts.First(p => p.Row == e.Row && p.Column == e.Column);
            removedPart.Remove();
        }

        #endregion

        #region client event handlers

        private void Client_GameEnded(object sender, EndResultEventArgs e)
        {
            StringBuilder resultMessage = new StringBuilder();
            for (int i = 0; i < e.Results.Count; ++i)
            {
                resultMessage.AppendLine($"{i + 1}. helyezés: {e.Results[i].Item1.GetDescription()}, {e.Results[i].Item2} pénzzel.");
            }

            StatusMessage = $"{resultMessage}\n{StatusMessage}";

            OptionsOrSubEvents.Clear();
            CurrentCardDescription = "Vissza a menübe...";
            CurrentCardToolTip = null;

            SendAttributesCommand = new DelegateCommand(param =>
            {
                _playerList.UnsubscribeFromEvents();
                UnsubscribeFromEvents();
                GameEnded?.Invoke(this, EventArgs.Empty);
            });
            OnPropertyChanged(nameof(SendAttributesCommand));
        }

        private void Client_FlightEnded(object sender, EventArgs e)
        {
            _gameOver = true;
            //from cards such as abandoned ships or encounters
            int fromCards = _ship.Cash;
            //from the stored wares, if player crashed, this value gets cut in half
            int fromWares = _client.Crashed ? _ship.GetWaresValue() / 2 : _ship.GetWaresValue();
            //deduction for losed parts
            int fromPenalty = -1 * _ship.Penalty;
            //if the ship didn't lose any parts and it has zero open connections, it gets a bonus based on the gamestage
            int fromBonus = (_ship.Penalty == 0 && _ship.GetOpenConnectorCount() == 0) ? ((int)_client.GameStage + 1) * 2 : 0;

            //-1 if not in list AKA when the player crashed
            int placement =  _client.PlayerOrder.FindIndex(item => item == _client.Player);

            int fromPlacement = placement == -1 ? 0 : (3 - placement) * ((int)_client.GameStage + 1);

            int sum = fromCards + fromWares + fromPenalty + fromBonus + fromPlacement;

            StatusMessage = $"Repülés vége, összesen {sum} pénzt szereztél!" +
                $"\n{fromCards} az elhagyott űrhajókból és fejpénzekből," +
                $"\n{fromWares} az áruk eladásából," +
                $"\n{fromPenalty} büntetés az elvesztett alkatrészekért," +
                $"\n{fromBonus} bónusz a hajó állapotáért," +
                $"\n{fromPlacement} a végső sorrendért.";

            _client.SendCashInfo(sum);
        }

        private void Client_OptionPicked(object sender, int e)
        {
            _client.Card.ApplyOption(_ship, e);
        }

        private void Client_OptionRemoved(object sender, int e)
        {
            //can only be received during a planets card
            StatusMessage = $"Az {e}. ajánlatot elvitték!";
            _client.Card.ApplyOption(_ship, -1 * e);
        }

        private void Client_CardOver(object sender, EventArgs e)
        {
            if (_client.Card.RequiresOrder && !_client.Card.IsResolved())
            {
                StatusMessage = _client.Crashed ? "Kiszálltál a versenyből." : "Más elhasználta a kártyát mielőtt sorra kerültél volna.";
                _client.Card.ApplyOption(_ship, -1);
            }
            else if(!_client.Card.IsResolved())
            {
                StatusMessage = "Az aktuális kártyát még nem játszottad le.";
            }
            else
            {
                StatusMessage = "Az aktuális kártya végig lett játszva, várakozás a kör végére.";
            }
            _roundResolved = true;
        }

        private void Client_OtherTargeted(object sender, PlayerColor e)
        {
            if (_client.Card.RequiresOrder)
            {
                StatusMessage = $"{e.GetDescription()} a soron következő játékos!";
                _isPlayersTurn = false;
            }
            else
            {
                StatusMessage = $"{e.GetDescription()} az aktuális effekt célpontja!";
                _client.Card.ApplyOption(_ship, 1);
            }
        }

        private void Client_PlayerTargeted(object sender, EventArgs e)
        {
            if (_client.Card.RequiresOrder)
            {
                StatusMessage = "Te vagy a soron következő játékos!";
                _isPlayersTurn = true;
            }
            else
            {
                StatusMessage = "Te vagy az aktuális effekt célpontja!";
                _client.Card.ApplyOption(_ship, 0);
            }
        }

        private void Client_PlayerCrashed(object sender, PlayerColor e)
        {
            RefreshTokens();
            AddPopUpMessage($"{e.GetDescription()} játékos kiszállt a versenyből!");
        }

        private void Client_CardPicked(object sender, int e)
        {
            AddPopUpMessage($"Új kártya, hátravan még: {e}");
            OptionsOrSubEvents.Clear();
            Card card = _client.Card;
            CurrentCardDescription = card.GetDescription();
            CurrentCardToolTip = card.ToolTip();
            RequiresAttributes = card.RequiresAttributes;
            var optionsOrSubEvents = card.GetOptionsOrSubEvents();
            _isPlayersTurn = false;
            card.DiceRolled += Card_DiceRolled;
            if(optionsOrSubEvents == null)
            {
                return;
            }
            foreach(OptionOrSubEvent item in optionsOrSubEvents)
            {
                OptionsOrSubEvents.Add(new OptionOrSubEventViewModel
                {
                    Description = item.Description,
                    ClickCommand = new DelegateCommand(param =>
                    {
                        return item.Condition(_ship) && !_client.Crashed && !_gameOver &&
                        (!card.RequiresOrder || (card.RequiresOrder && _isPlayersTurn));
                    }
                    , param => item.Action(_client, _ship))
                });
            }
        }

        #endregion

        #region misc event handlers

        private void Card_DiceRolled(object sender, DiceRolledEventArgs e)
        {
            StatusMessage = $"Aktuális veszély: {e.Direction.GetDescription()} {e.Projectile.GetDescription()} a {e.Number + 2} vonalon.";
            _isWaiting = true;
        }

        private void PlayerList_LostConnection(object sender, EventArgs e)
        {
            StatusMessage = "Megszakadt a kapcsolat a szerverrel!";
            UnsubscribeFromEvents();
        }

        #endregion

        #region private methods

        private async void AddPopUpMessage(string message)
        {
            PopupMessages.Add(message);
            await Task.Delay(5000);
            PopupMessages.Remove(message);
        }

        private void UnsubscribeFromEvents()
        {
            _client.CardPicked -= Client_CardPicked;
            _client.PlayerCrashed -= Client_PlayerCrashed;
            _client.PlayerTargeted -= Client_PlayerTargeted;
            _client.OtherTargeted -= Client_OtherTargeted;
            _client.CardOver -= Client_CardOver;
            _client.OptionRemoved -= Client_OptionRemoved;
            _client.OptionPicked -= Client_OptionPicked;
            _client.FlightEnded -= Client_FlightEnded;
            _client.GameEnded -= Client_GameEnded;
            _playerList.LostConnection -= PlayerList_LostConnection;
            _ship.PartRemoved -= Ship_PartRemoved;
            _ship.Wrecked -= Ship_Wrecked;
            _ship.FlightAttributesChanged -= Ship_FlightAttributesChanged;
        }

        /// <summary>
        /// Method to setup the basic 7x15 hollow square of fields, 
        /// with the first element being the one in the middle on the top, going clockwise
        /// </summary>
        private void InitializeOrderFields()
        {
            //Fill up the collection in that order, to make later use easier
            PlayerOrderFields = new ObservableCollection<OrderFieldViewModel>();
            int placeValue = 0;
            for(int i = 7; i <= 14; ++i)
            {
                PlayerOrderFields.Add(new OrderFieldViewModel(0, i, placeValue++));
            }
            for(int j = 1; j <= 6; ++j)
            {
                PlayerOrderFields.Add(new OrderFieldViewModel(j, 14, placeValue++));
            }
            for(int i = 13; i >= 0; --i)
            {
                PlayerOrderFields.Add(new OrderFieldViewModel(6, i, placeValue++));
            }
            for(int j = 5; j >= 0; --j)
            {
                PlayerOrderFields.Add(new OrderFieldViewModel(j, 0, placeValue++));
            }
            for(int i = 0; i < 7; ++i)
            {
                PlayerOrderFields.Add(new OrderFieldViewModel(0, i, placeValue++));
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
                PlayerOrderFields.First(field => field.PlaceValue == pair.Value.PlaceValue).Token = token;
            }
        }

        #endregion
    }
}
