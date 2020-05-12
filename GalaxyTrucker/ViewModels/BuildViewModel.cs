using GalaxyTrucker.Model;
using GalaxyTrucker.Network;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace GalaxyTrucker.ViewModels
{
    public class BuildViewModel : NotifyBase
    {
        private readonly PlayerListViewModel _playerList;
        private readonly GTTcpClient _client;
        private readonly Ship _ship;
        private readonly object _pickablePartsLock;
        private (int, int)? _lastPicked;
        private PartViewModel _selectedPart;
        private ObservableCollection<PartViewModel> _shipParts;
        private string _toggleReadyContent;
        private string _error;
        private bool _buildingEnded;
        private Personnel _currentAlien;
        private bool _lastPickResolved;

        private ObservableCollection<PickablePart> _pickableParts;

        public PlayerListViewModel PlayerList
        {
            get
            {
                return _playerList;
            }
        }

        public bool BuildingEnded
        {
            get
            {
                return _buildingEnded;
            }
            set
            {
                _buildingEnded = value;
                OnPropertyChanged();
            }
        }

        public string Error
        {
            get
            {
                return _error;
            }
            private set
            {
                _error = value;
                OnPropertyChanged();
            }
        }

        public string ToggleReadyContent
        {
            get
            {
                return _toggleReadyContent;
            }
            set
            {
                _toggleReadyContent = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<PickablePart> PickableParts
        {
            get { return _pickableParts; }
            private set
            {
                _pickableParts = value;
                BindingOperations.EnableCollectionSynchronization(_pickableParts, _pickablePartsLock);
                OnPropertyChanged();
            }
        }

        public ObservableCollection<PartViewModel> ShipParts
        {
            get
            {
                return _shipParts;
            }
            private set
            {
                if (_shipParts != value)
                {
                    _shipParts = value;
                    OnPropertyChanged();
                }
            }
        }

        public PartViewModel SelectedPart
        {
            get
            {
                return _selectedPart;
            }
            private set
            {
                if(_selectedPart != value)
                {
                    _selectedPart = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedPartImage));
                    OnPropertyChanged(nameof(SelectedPartAngle));
                }
            }
        }

        public Image SelectedPartImage
        {
            get
            {
                if(SelectedPart != null)
                {
                    return SelectedPart.PartImage;
                }
                return null;
            }
        }

        public DelegateCommand PutBackSelectedCommand { get; private set; }

        public DelegateCommand RotateSelectedCommand { get; private set; }

        public DelegateCommand ToggleReadyCommand { get; private set; }

        public DelegateCommand AddAlienCommand { get; private set; }

        public int SelectedPartAngle
        {
            get
            {
                if (SelectedPart == null)
                {
                    return 0;
                }
                return SelectedPart.Angle;
            }
        }

        public event EventHandler FatalErrorOccured;

        public BuildViewModel(GTTcpClient client, PlayerListViewModel playerList, ShipLayout layout)
        {
            _playerList = playerList;
            _playerList.SynchronizeListWithClient();
            _playerList.LostConnection += PlayerList_LostConnection;

            _lastPickResolved = true;
            _currentAlien = Personnel.None;
            BuildingEnded = false;
            _lastPicked = null;
            _pickablePartsLock = new object();
            _client = client;
            _client.PartPicked += Client_PartPicked;
            _client.PartPutBack += Client_PartPutBack;
            _client.PartTaken += Client_PartTaken;
            _client.BuildingEnded += Client_BuildingEnded;

            ToggleReadyContent = "Építkezés befejezése";
            _ship = new Ship(layout, _client.Player);
            _ship.PartRemoved += Ship_PartRemoved;
            ShipParts = new ObservableCollection<PartViewModel>();

            for(int i = 0; i < 11; ++i)
            {
                for(int j = 0; j < 11; ++j)
                {
                    ShipParts.Add(new PartViewModel
                    {
                        ShipRow = i,
                        ShipColumn = j,
                        IsValidField = _ship.IsFieldValid(i, j)
                    });
                }
            }

            foreach(PartViewModel item in ShipParts)
            {
                item.PartClickCommand = new DelegateCommand(param => !_client.IsReady, param => ShipPartClick(param as PartViewModel));
            }

            Part cockpit = _ship.GetCockpit();
            PartViewModel cockpitViewModel = ShipParts.Where(p => p.ShipRow == cockpit.Row && p.ShipColumn == cockpit.Column).First();
            cockpitViewModel.Part = cockpit;
            cockpitViewModel.PartImage = PartBuilder.GetPartImage(cockpit);
            cockpitViewModel.PartClickCommand = null;

            PickableParts = new ObservableCollection<PickablePart>();
            for(int i = 0; i < 10; ++i)
            {
                for(int j = 0; j < 14; ++j)
                {
                    PickableParts.Add(new PickablePart
                    {
                        Row = i,
                        Column = j,
                        IsPickable = true
                    });
                }
            }

            foreach(PickablePart item in PickableParts)
            {
                item.PartPickCommand = new DelegateCommand(param => !_client.IsReady, param =>
                {
                    if (!_lastPickResolved)
                    {
                        return;
                    }
                    _lastPickResolved = false;
                    if (_lastPicked != null)
                    {   
                        PutBackSelected();
                    }
                    PickablePart pickedPart = param as PickablePart;
                    _client.PickPart(pickedPart.Row, pickedPart.Column);
                    _lastPicked = (item.Row, item.Column);
                });
            }

            PutBackSelectedCommand = new DelegateCommand(param => SelectedPart != null && !_client.IsReady, param => PutBackSelected());

            RotateSelectedCommand = new DelegateCommand(param => SelectedPart != null && !_client.IsReady, param => RotateSelected(int.Parse(param as string)));

            ToggleReadyCommand = new DelegateCommand(param =>
            {
                try
                {
                    _client.ToggleReady(ServerStage.Build);
                    _playerList.ToggleClientReady();
                    ToggleReadyContent = (_client.IsReady, _buildingEnded) switch
                    {
                        (true, _) => "Mégsem kész",
                        (false, true) => "Tovább a repülésre",
                        (false, false) => "Építkezés befejezése"
                    };
                }
                catch (Exception e)
                {
                    Error = $"Hiba a szerverrel való kommunikáció közben:\n{e.Message}";
                }
            });

            AddAlienCommand = new DelegateCommand(param => BuildingEnded && !_client.IsReady, param => AddAlien(param as string));
        }

        #region private methods

        /// <summary>
        /// Method called through the PlayerListViewModel when the player loses connection to the server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayerList_LostConnection(object sender, EventArgs e)
        {
            Error = "Nincs kapcsolat.";
            MessageBox.Show("A szerverrel való kapcsolat megszakadt!\n");
            FatalErrorOccured?.Invoke(this, EventArgs.Empty);
            UnsubscribeFromEvents();
        }

        private void AddAlien(string alien)
        {
            _currentAlien = Enum.Parse<Personnel>(alien);
            _ship.HighlightCabinsForAlien(_currentAlien);
        }

        /// <summary>
        /// Method called when the building stage ends
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_BuildingEnded(object sender, BuildingEndedEventArgs e)
        {
            BuildingEnded = true;
            _playerList.SynchronizeListWithClient();
            ToggleReadyContent = "Tovább a repülésre";
        }

        /// <summary>
        /// Method called when a part is removed from the ship either from a chain-reaction or by being directly removed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Ship_PartRemoved(object sender, PartRemovedEventArgs e)
        {
            PartViewModel removedPart = ShipParts.Where(p => p.ShipRow == e.Row && p.ShipColumn == e.Column).First();
            removedPart.Part = null;
            removedPart.PartImage = null;
            _client.PutBackPart(removedPart.BuildRow, removedPart.BuildColumn);
            PickablePart origin = PickableParts.Where(p => p.Row == removedPart.BuildRow && p.Column == removedPart.BuildColumn).First();
            origin.IsPickable = true;
        }

        /// <summary>
        /// Method called when the client clicks on a ship part
        /// </summary>
        /// <param name="partViewModel"></param>
        private void ShipPartClick(PartViewModel partViewModel)
        {
            //No part is selected, and the clicked partviewmodel has no associated part - do nothing
            if(partViewModel.Part == null && SelectedPart == null)
            {
                return;
            }

            //A part is selected, and the clicked partviewmodel has no associated part - try to put the part into the ship
            if(SelectedPart != null && partViewModel.Part == null && !_buildingEnded)
            {
                PartAddProblems result = _ship.AddPart(SelectedPart.Part, partViewModel.ShipRow, partViewModel.ShipColumn);
                if (result != PartAddProblems.None)
                {
                    List<string> errorList = new List<string>();
                    if (result.HasFlag(PartAddProblems.Occupied))
                    {
                        errorList.Add("A megadott mezőn már van elem!");
                    }
                    if (result.HasFlag(PartAddProblems.ConnectorsDontMatch))
                    {
                        errorList.Add("A megadott helyen a csatlakozók ütköznek!");
                    }
                    if (result.HasFlag(PartAddProblems.HasNoConnection))
                    {
                        errorList.Add("Az megadott helyen az elem nem csatlakozik a hajóhoz!");
                    }
                    if (result.HasFlag(PartAddProblems.BlocksEngine))
                    {
                        errorList.Add("A megadott helyen az elem közvetlenül egy motor alatt van!");
                    }
                    if (result.HasFlag(PartAddProblems.BlocksLaser))
                    {
                        errorList.Add("A megadott helyen az elem közvetlenül egy lézer előtt van!");
                    }
                    if (result.HasFlag(PartAddProblems.BlockedAsLaser))
                    {
                        errorList.Add("A megadott helyen a lézer előtt nem lenne üres hely!");
                    }
                    if (result.HasFlag(PartAddProblems.BlockedAsEngine))
                    {
                        errorList.Add("A megadott helyen a motor alatt nem lenne üres hely!");
                    }

                    MessageBox.Show(string.Join('\n', errorList));
                    return;
                }
                partViewModel.Part = SelectedPart.Part;
                partViewModel.BuildRow = SelectedPart.BuildRow;
                partViewModel.BuildColumn = SelectedPart.BuildColumn;
                partViewModel.PartImage = SelectedPart.PartImage;
                SelectedPart = null;
                return;
            }

            //No part is selected, and the clicked partviewmodel has an associated part - put the part back into the set
            if(partViewModel.Part != null && !_buildingEnded)
            {
                _ship.RemovePartAtIndex(partViewModel.ShipRow, partViewModel.ShipColumn);
                return;
            }

            //Building is over and an alien is being put on
            if(_currentAlien != Personnel.None && _buildingEnded && partViewModel.Highlighted)
            {
                _ship.AddAlien(partViewModel.ShipRow, partViewModel.ShipColumn, _currentAlien);
                _currentAlien = Personnel.None;
                foreach(PartViewModel part in ShipParts)
                {
                    if (part.Highlighted)
                    {
                        part.Part.Highlight();
                    }
                }
                return;
            }
        }

        private void RotateSelected(int leftOrRight)
        {
            if(SelectedPart == null)
            {
                return;
            }
            SelectedPart.Rotate(leftOrRight);
            OnPropertyChanged(nameof(SelectedPartAngle));
        }

        /// <summary>
        /// Method called when the client clicks on the selected part to put it back
        /// </summary>
        private void PutBackSelected()
        {
            if (SelectedPart == null)
            {
                return;
            }
            _client.PutBackPart(SelectedPart.BuildRow, SelectedPart.BuildColumn);
            PickablePart pickedPart = PickableParts.ElementAt(SelectedPart.BuildRow * 14 + SelectedPart.BuildColumn);
            pickedPart.IsPickable = true;
            SelectedPart = null;
            _lastPicked = null;
        }

        /// <summary>
        /// Method called when another player picks a part
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_PartTaken(object sender, PartTakenEventArgs e)
        {
            PickablePart pickedPart = PickableParts.ElementAt(e.Row * 14 + e.Column);
            pickedPart.IsPickable = false;
        }

        /// <summary>
        /// Method called when another player puts back a part
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_PartPutBack(object sender, PartPutBackEventArgs e)
        {
            PickablePart putBackPart = PickableParts.ElementAt(e.Row * 14 + e.Column);
            putBackPart.Part = e.Part;
            putBackPart.IsPickable = true;
        }

        /// <summary>
        /// Method called when this client receives the response to picking a part
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_PartPicked(object sender, PartPickedEventArgs e)
        {
            //If another client just took the part, the Part component is null
            //The part taken event will be raised, so the part won't be removed here
            if (e.Part == null)
            {
                MessageBox.Show("Az adott alkatrészt már más elvitte!");
                return;
            }
            PickablePart pickedPart = PickableParts.Where(part => part.Row == _lastPicked.Value.Item1 && part.Column == _lastPicked.Value.Item2).First();
            pickedPart.IsPickable = false;
            pickedPart.Part = e.Part;
            SelectedPart = new PartViewModel()
            {
                BuildColumn = pickedPart.Column,
                BuildRow = pickedPart.Row,
                Part = pickedPart.Part,
                PartImage = pickedPart.PartImage
            };
            _lastPickResolved = true;
        }

        private void UnsubscribeFromEvents()
        {
            _client.PartPicked -= Client_PartPicked;
            _client.PartPutBack -= Client_PartPutBack;
            _client.PartTaken -= Client_PartTaken;
            _client.BuildingEnded -= Client_BuildingEnded;
        }

        #endregion
    }
}
