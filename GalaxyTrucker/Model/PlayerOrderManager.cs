using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GalaxyTrucker.Model
{
    public class PlaceProperty
    {
        public PlayerColor Player { get; }

        public int PlaceValue { get; set; }

        public int LapCount { get; set; }

        public PlaceProperty(PlayerColor player, int placeValue)
        {
            Player = player;
            PlaceValue = placeValue;
            LapCount = 0;
        }
    }

    public class PlayerOrderManager
    {
        private const int StartingValue = 20;
        private const int LapSize = 40;
        private readonly PlaceProperty[] _playerPlaces;
        private readonly Dictionary<PlayerColor, PlaceProperty> _properties;
        private readonly Semaphore _sem;

        public Dictionary<PlayerColor, PlaceProperty> Properties
        {
            get
            {
                return _properties;
            }
        }

        public event EventHandler<PlayerColor> PlayerCrashed;

        public event EventHandler PlacesChanged;

        public PlayerOrderManager(List<PlayerColor> initialOrder, GameStage stage)
        {
            if(initialOrder.Distinct().Count() != initialOrder.Count)
            {
                throw new ArgumentException("The given list has duplicate elements!");
            }

            _sem = new Semaphore(1, 1);
            _playerPlaces = new PlaceProperty[40];
            _properties = new Dictionary<PlayerColor, PlaceProperty>();

            int spaceBetween = (int)stage + 2;
            for(int i = 0; i < initialOrder.Count; ++i)
            {
                int placeValue = StartingValue - (i * spaceBetween);
                _playerPlaces[placeValue] = new PlaceProperty(initialOrder[i], placeValue);
                _properties.Add(initialOrder[i], _playerPlaces[placeValue]);
            }
        }

        public void AddDistance(PlayerColor player, int value)
        {
            if (!Properties.ContainsKey(player))
            {
                return;
            }

            _sem.WaitOne();

            //if the player moves 0 distance, they crash
            if (value == 0)
            {
                //remove the player from the grid
                _playerPlaces[_properties[player].PlaceValue] = null;
                //OnPlayerCrashed removes the player from the properties list
                OnPlayerCrashed(player);
                _sem.Release();
                return;
            }

            bool negative = value < 0;
            _playerPlaces[_properties[player].PlaceValue] = null;

            int nextPlace = _properties[player].PlaceValue;
            for(int i = 0; i < Math.Abs(value); ++i)
            {
                nextPlace += negative ? -1 : 1;
                if(nextPlace >= LapSize)
                {
                    nextPlace = 0;
                    ++_properties[player].LapCount;
                }
                else if(nextPlace < 0)
                {
                    nextPlace = LapSize - 1;
                    --_properties[player].LapCount;
                }

                while (_playerPlaces[nextPlace] != null)
                {
                    PlaceProperty occupier = _playerPlaces[nextPlace];

                    //if the moving player steps forwards and ups a lap on someone else
                    if(occupier.LapCount < _properties[player].LapCount && !negative)
                    {
                        //also remove the other player from the grid
                        _playerPlaces[nextPlace] = null;
                        OnPlayerCrashed(occupier.Player);
                    }
                    //if the moving player steps backwards and gets upped a lap on
                    else if(occupier.LapCount > _properties[player].LapCount && negative)
                    {
                        OnPlayerCrashed(player);
                        _sem.Release();
                        return;
                    }

                    nextPlace += negative ? -1 : 1;
                    if (nextPlace >= LapSize)
                    {
                        nextPlace = 0;
                        ++_properties[player].LapCount;
                    }
                    else if (nextPlace < 0)
                    {
                        nextPlace = LapSize - 1;
                        --_properties[player].LapCount;
                    }
                }
            }
            _properties[player].PlaceValue = nextPlace;
            _playerPlaces[nextPlace] = _properties[player];
            PlacesChanged?.Invoke(this, EventArgs.Empty);
            _sem.Release();
        }

        /// <summary>
        /// Method to get the turn order for the round, which excludes already immobilised players
        /// </summary>
        /// <returns></returns>
        public List<PlayerColor> GetOrder()
        {
            return _properties
                .OrderByDescending(pair => pair.Value.LapCount * LapSize + pair.Value.PlaceValue)
                .Select(pair => pair.Key).ToList();
        }

        private void OnPlayerCrashed(PlayerColor player)
        {
            Properties.Remove(player);
            PlayerCrashed?.Invoke(this, player);
            PlacesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
