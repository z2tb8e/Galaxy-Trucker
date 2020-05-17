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
        const int StartingValue = 20;
        const int LapSize = 40;
        private readonly PlaceProperty[] _playerPlaces;
        private readonly Dictionary<PlayerColor, PlaceProperty> _properties;
        private readonly Semaphore _sem;

        public event EventHandler<PlayerColor> PlayerCrashed;

        public event EventHandler PlacesChanged;

        public Dictionary<PlayerColor, PlaceProperty> Properties
        {
            get
            {
                return _properties;
            }
        }

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
                int spaceValue = StartingValue - (i * spaceBetween);
                _playerPlaces[spaceValue] = new PlaceProperty(initialOrder[i], spaceValue);
                _properties.Add(initialOrder[i], _playerPlaces[spaceValue]);
            }
        }

        public void AddDistance(PlayerColor player, int value)
        {
            _sem.WaitOne();

            if (value == 0)
            {
                OnPlayerCrashed(player);
                return;
            }

            bool negative = value < 0;
            _playerPlaces[_properties[player].PlaceValue] = null;

            int nextPlace = _properties[player].PlaceValue + (negative ? -1 : 1);
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

                while (IsOccupied(nextPlace))
                {
                    PlaceProperty occupier = _playerPlaces[nextPlace];

                    if(occupier.LapCount < _properties[player].LapCount && !negative)
                    {
                        OnPlayerCrashed(occupier.Player);
                    }
                    else if(occupier.LapCount > _properties[player].LapCount && negative)
                    {
                        OnPlayerCrashed(player);
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
                _properties[player].PlaceValue = nextPlace;
                _playerPlaces[nextPlace] = _properties[player];
            }
            PlacesChanged?.Invoke(this, EventArgs.Empty);
            _sem.Release();
        }

        /// <summary>
        /// Method to get the turn order for the round, which excludes already immobilised players
        /// </summary>
        /// <returns></returns>
        public List<PlayerColor> GetCurrentOrder()
        {
            return _properties
                .OrderByDescending(pair => pair.Value.LapCount * LapSize + pair.Value.PlaceValue)
                .Select(pair => pair.Key).ToList();
        }

        /// <summary>
        /// Method to get the final order of the ships, which includes immobilised players
        /// </summary>
        /// <returns></returns>
        public List<PlayerColor> GetFinalOrder()
        {
            return _properties.OrderByDescending(pair => pair.Value.LapCount * LapSize + pair.Value.PlaceValue)
                .Select(pair => pair.Key).ToList();
        }

        private bool IsOccupied(int placeValue)
        {
            return _properties.Values.Where(val => val.PlaceValue == placeValue).Any();
        }

        private void OnPlayerCrashed(PlayerColor player)
        {
            Properties.Remove(player);
            PlayerCrashed?.Invoke(this, player);
        }
    }
}
