using GalaxyTrucker.Model.PartTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace GalaxyTrucker.Model
{
    public class Ship
    {
        #region fields

        private readonly bool[,] _movableFields;
        private readonly Part[,] _parts;
        private readonly int _penaltyCap;
        private int _penalty;
        private readonly List<Part> _activatableParts;
        private readonly List<Storage> _storages;
        private bool _hasEngineAlien;
        private bool _hasLaserAlien;
        private int _cash;

        #endregion

        #region properties

        public int Cash
        {
            get
            {
                return _cash;
            }
            set
            {
                _cash = value;
                CashChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public IReadOnlyCollection<Part> Parts
        {
            get
            {
                return _parts.Cast<Part>().ToList().AsReadOnly();
            }
        }

        public int Penalty => _penalty > _penaltyCap ? _penaltyCap : _penalty;

        public int Firepower
        {
            get
            {
                int sum = _parts.Cast<Part>().Where(x => x is Laser).Sum(x => (x as Laser).Firepower);
                sum += sum > 0 && _hasLaserAlien ? 2 : 0;
                return sum;
            }
        }

        public int Enginepower
        {
            get
            {
                int sum = _parts.Cast<Part>().Where(x => x is Engine).Sum(x => (x as Engine).Enginepower);
                sum += sum > 0 && _hasEngineAlien ? 2 : 0;
                return sum;
            }
        }

        public int HumanCount => _parts.Cast<Part>().Where(x => x is Cabin).Sum(x => (x as Cabin).Personnel switch
        {
            Personnel.HumanSingle => 1,
            Personnel.HumanDouble => 2,
            _ => 0
        });

        public int CrewCount => _parts.Cast<Part>().Where(x => x is Cabin).Sum(x => (x as Cabin).Personnel switch
        {
            Personnel.None => 0,
            Personnel.HumanDouble => 2,
            _ => 1
        });

        public int StorageCount => _parts.Cast<Part>().Where(x => x is Storage).Sum(x => (x as Storage).Capacity);

        public int Batteries => _parts.Cast<Part>().Where(x => x is Battery).Sum(x => (x as Battery).Charges);

        #endregion

        #region events

        public event EventHandler<WreckedSource> Wrecked;

        public event EventHandler<PartRemovedEventArgs> PartRemoved;

        public event EventHandler CashChanged;

        public event EventHandler FlightAttributesChanged;

        #endregion

        #region ctor

        /// <summary>
        /// Constructor for the ship class, setting the fields in which parts can be placed in, as well as placing the cockpit and setting the player colour.
        /// </summary>
        /// <param name="layout">The layout of the ship</param>
        /// <param name="color">The color indicating the owner of the ship</param>
        public Ship(ShipLayout layout, PlayerColor color)
        {
            Cash = 0;
            _hasEngineAlien = false;
            _hasLaserAlien = false;
            (int, int) cockpit;
            (cockpit, _movableFields) = LayoutReader.GetLayout(layout);
            _activatableParts = new List<Part>();
            _storages = new List<Storage>();
            _parts = new Part[11, 11];
            _parts[cockpit.Item1, cockpit.Item2] = new Cockpit(color)
            {
                Row = cockpit.Item1,
                Column = cockpit.Item2
            };
            _penalty = 0;
            _penaltyCap = layout switch
            {
                ShipLayout.Small => 5,
                ShipLayout.Medium => 8,
                _ => 11,
            };
        }

        #endregion

        #region public methods

        public bool HighlightCabinsForAlien(Personnel alien)
        {
            if(alien != Personnel.EngineAlien && alien != Personnel.LaserAlien)
            {
                return false;
            }

            if ((_hasEngineAlien && alien == Personnel.EngineAlien) || (_hasLaserAlien && alien == Personnel.LaserAlien))
            {
                return false;
            }

            bool ret = false;
            IEnumerable<Part> cabins = _parts.Cast<Part>().Where(p => p is Cabin && !(p is Cockpit));
            foreach(Part cabin in cabins)
            {
                if (cabin.Neighbours.Any(p => (p is EngineCabin && alien == Personnel.EngineAlien) || (p is LaserCabin && alien == Personnel.LaserAlien)))
                {
                    ret = true;
                    cabin.Highlight();
                }
            }
            return ret;
        }

        public Part GetCockpit()
        {
            return _parts.Cast<Part>().FirstOrDefault(p => p is Cockpit);
        }

        public bool IsValidField(int row, int column)
        {
            return _movableFields[row, column];
        }

        /// <summary>
        /// Method to get the number of connectors facing empty fields
        /// </summary>
        /// <returns>The number of these open connectors</returns>
        public int GetOpenConnectorCount()
        {
            return _parts.Cast<Part>()
                .Where(p => p != null)
                .Sum(p => p.Connectors.Where(conn => conn != Connector.None).Count() - p.Neighbours.Count);
        }

        /// <summary>
        /// Method to add to the cabin at the supplied indices, given it has the neccessary alien cabin neighbouring it.
        /// </summary>
        /// <param name="row">The row of the cabin</param>
        /// <param name="column">The column of the cabin</param>
        /// <param name="alien">The type of alien to add</param>
        public bool AddAlien(int row, int column, Personnel alien)
        {
            if (_hasEngineAlien && alien == Personnel.EngineAlien || _hasLaserAlien && alien == Personnel.LaserAlien)
            {
                return false;
            }

            if (!(_parts[row, column] is Cabin))
            {
                return false;
            }

            else if (_parts[row, column] is Cockpit)
            {
                return false;
            }

            if(alien == Personnel.EngineAlien)
            {
                if (_parts[row, column].Neighbours.Any(p => p is EngineCabin))
                {
                    (_parts[row, column] as Cabin).Personnel = alien;
                    _hasEngineAlien = true;
                    return true;
                }
            }

            else if(alien == Personnel.LaserAlien)
            {
                if (_parts[row, column].Neighbours.Any(p => p is LaserCabin))
                {
                    (_parts[row, column] as Cabin).Personnel = alien;
                    _hasLaserAlien = true;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Method to fill the unfilled cabins with human personnel.
        /// </summary>
        public void FillCabins()
        {
            _penalty = 0;
            foreach(Cabin c in _parts.Cast<Part>().Where(x => x is Cabin && (x as Cabin).Personnel == Personnel.None))
            {
                c.Personnel = Personnel.HumanDouble;
            }
        }

        /// <summary>
        /// Method to deactivate all activated parts.
        /// </summary>
        public void ResetActivatables()
        {
            foreach(Part p in _activatableParts)
            {
                switch (p)
                {
                    case Shield s:
                        if (s.Activated)
                            s.Deactivate();
                        break;
                    case LaserDouble l:
                        if (l.Activated)
                            l.Deactivate();
                        break;
                    case EngineDouble e:
                        if (e.Activated)
                            e.Deactivate();
                        break;
                }
            }
            FlightAttributesChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool HighlightActivatables()
        {
            bool ret = false;
            foreach (Part p in _activatableParts)
            {
                switch (p)
                {
                    case Shield s:
                        if (!s.Activated)
                            s.Highlight();
                        ret = true;
                        break;
                    case LaserDouble l:
                        if (!l.Activated)
                            l.Highlight();
                        ret = true;
                        break;
                    case EngineDouble e:
                        if (!e.Activated)
                            e.Highlight();
                        ret = true;
                        break;
                }
            }
            return ret;
        }

        /// <summary>
        /// Method to apply the effects of a pandemic event.
        /// In case of a pandemic all cabins which are directly connected and have at least a single crew member, lose one member of personnel each.
        /// </summary>
        public void ApplyPandemic()
        {
            List<Cabin> applicableCabins = new List<Cabin>();

            IEnumerable<Cabin> cabins = _parts.Cast<Part>().Where(p => p is Cabin).Select(p => p as Cabin);
            foreach(Cabin item in cabins)
            {
                if (item.Personnel != Personnel.None &&
                    item.Neighbours.Any(p => p is Cabin && (p as Cabin).Personnel != Personnel.None))
                {
                    //if crew was removed here, and the cabin becomes empty,
                    //the next cabin would register it as empty, thus not removing their personnel
                    applicableCabins.Add(item);
                }
            }

            foreach(Cabin item in applicableCabins)
            {
                item.RemoveSinglePersonnel();
            }

            if (HumanCount == 0)
            {
                Wrecked?.Invoke(this, WreckedSource.OutOfHumans);
            }
            FlightAttributesChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Method to apply the effects of a projectile hitting the ship from a given direction the given line.
        /// </summary>
        /// <param name="projectile">The type of projectile the ship is getting hit by</param>
        /// <param name="dir">The direction the projectile is coming from</param>
        /// <param name="line">The line (vertical or horizontal) in which the projectile is approaching, ranging from 0 to 10</param>
        public void ApplyProjectile(Projectile projectile, Direction dir, int line)
        {
            //determine the part about to be hit

            IEnumerable<Part> partsInLine = dir switch
            {
                Direction.Top => _parts.Cast<Part>().Where(p => p != null).Where(p => p.Column == line),
                Direction.Right => _parts.Cast<Part>().Where(p => p != null).Where(p => p.Row == line).Reverse(),
                Direction.Bottom => _parts.Cast<Part>().Where(p => p != null).Where(p => p.Column == line).Reverse(),
                Direction.Left => _parts.Cast<Part>().Where(p => p != null).Where(p => p.Row == line),
                _ => throw new InvalidEnumArgumentException()
            };

            //no element is in the line
            if (!partsInLine.Any())
            {
                return;
            }

            int targetRow = partsInLine.First().Row;
            int targetColumn = partsInLine.First().Column;

            HashSet<Direction> shieldedDirections = GetShieldedDirections();

            switch (projectile)
            {
                case Projectile.MeteorSmall:
                    if(partsInLine.First().GetConnector(dir) != Connector.None && !shieldedDirections.Contains(dir))
                    {
                        RemovePartAtIndex(targetRow, targetColumn);
                    }
                    break;
                case Projectile.MeteorLarge:
                    if (!partsInLine.Any(p => p is Laser && p.Rotation == dir && (p as Laser).Firepower > 0))
                    {
                        RemovePartAtIndex(targetRow, targetColumn);
                    }
                    break;
                case Projectile.ShotSmall:
                    if (!shieldedDirections.Contains(dir))
                    {
                        RemovePartAtIndex(targetRow, targetColumn);
                    }
                    break;
                case Projectile.ShotLarge:
                    RemovePartAtIndex(targetRow, targetColumn);
                    break;
            }
        }

        /// <summary>
        /// Method to get the base worth of the stored wares
        /// </summary>
        /// <returns></returns>
        public int GetWaresValue()
        {
            return _storages.Sum(s => s.Value);
        }

        /// <summary>
        /// Method to add a list of wares to the ship's storages while maximizing the value of the stored wares.
        /// </summary>
        /// <param name="wares">The list of wares to add</param>
        public void AddWares(IEnumerable<Ware> wares)
        {
            if (_storages.Count == 0)
            {
                return;
            }
            foreach(Ware w in wares)
            {
                Ware min = _storages.Min(x => x.Min);
                if(w > min)
                {
                    Storage target = _storages.FirstOrDefault(x => x.Min == min && (w != Ware.Red || (w == Ware.Red && x is SpecialStorage)));
                    target?.AddWare(w);
                }
            }
        }

        /// <summary>
        /// Method to remove the supplied number of wares from the ship, prioritizing the highest value wares.
        /// </summary>
        /// <param name="count">The number of wares to remove</param>
        public int RemoveWares(int count)
        {
            if(_storages.Count == 0)
            {
                return 0;
            }
            int amountLeft = count;
            Ware max = _storages.Max(storage => storage.Max);
            while (amountLeft > 0 && max != Ware.Empty)
            {
                Storage storage = _storages.First(st => st.Max == max);
                storage.RemoveMax();
                --amountLeft;

                max = _storages.Max(storage => storage.Max);
            }
            return (count - amountLeft);
        }

        /// <summary>
        /// Method to try to remove personnel ship until there is none left or the supplied amount was removed
        /// </summary>
        /// <param name="number">The number of personnel to remove</param>
        /// <returns>The number of personnel actually removed</returns>
        public int RemovePersonnel(int number)
        {
            int removeLeft = number;
            while(CrewCount > 0 && removeLeft > 0)
            {
                //Non-cockpit cabins with human personnel are prioritized
                IEnumerable<Cabin> cabins = _parts.Cast<Part>().Where(p => p is Cabin).Select(p => p as Cabin);
                IEnumerable<Cabin> cabinsWithHumans = cabins.Where(c => !(c is Cockpit) && (c.Personnel == Personnel.HumanSingle || c.Personnel == Personnel.HumanDouble));

                if (cabinsWithHumans.Any())
                {
                    cabinsWithHumans.First().RemoveSinglePersonnel();
                    --removeLeft;
                }
                else
                {
                    //If there is none of those, aliens are secondary(those can't be in the cockpit so no filtering for that)
                    IEnumerable<Cabin> cabinsWithAliens = cabins.Where(c => c.Personnel == Personnel.EngineAlien || c.Personnel == Personnel.LaserAlien);
                    if (cabinsWithAliens.Any())
                    {
                        cabinsWithAliens.First().RemoveSinglePersonnel();
                        --removeLeft;
                    }
                    else
                    {
                        //last, cockpit personnel is removed
                        //since CrewCount > 0, the cockpit must have personnel
                        (GetCockpit() as Cockpit).RemoveSinglePersonnel();
                        --removeLeft;
                    }
                }
            }
            if(HumanCount == 0)
            {
                Wrecked?.Invoke(this, WreckedSource.OutOfHumans);
            }
            FlightAttributesChanged?.Invoke(this, EventArgs.Empty);
            return number - removeLeft;
        }

        /// <summary>
        /// Method to activate the part at the given indices, given it is an activatable part and it's currently inactive.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        public void ActivatePartAt(int row, int column)
        {
            Part current = _parts[row, column];
            if (!(current is IActivatable))
            {
                return;
            }
            switch (current)
            {
                case LaserDouble l:
                    if (l.Activated)
                    {
                        return;
                    }
                    if (SpendEnergy())
                    {
                        l.Activate();
                    }
                    break;
                case EngineDouble e:
                    if (e.Activated)
                    {
                        return;
                    }
                    if (SpendEnergy())
                    {
                        e.Activate();
                    }
                    break;
                case Shield s:
                    if (s.Activated)
                    {
                        return;
                    }
                    if (SpendEnergy())
                    {
                        s.Activate();
                    }
                    break;
            }
            FlightAttributesChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Function to add a new part to the ship at the supplied indices.
        /// </summary>
        /// <param name="part"></param>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns>A logical value indicating whether the part could be added at the given location</returns>
        public PartAddProblems AddPart(Part part, int row, int column)
        {
            PartAddProblems ret = PartAddProblems.None;
            //check if the target is within bounds and is not occupied yet
            if (!_movableFields[row, column] || _parts[row, column] != null)
            {
                ret |= PartAddProblems.Occupied;
            }

            (Part, Direction)[] neighbours = new (Part, Direction)[]
            {
                (_parts[row - 1, column], Direction.Bottom),
                (_parts[row, column + 1], Direction.Left),
                (_parts[row + 1, column], Direction.Top),
                (_parts[row, column - 1], Direction.Right)
            };

            //if the part is a laser it can't have a part right in front of it
            if (part is Laser)
            {
                switch (part.Rotation)
                {
                    case Direction.Top:
                        if (neighbours[0].Item1 != null) ret |= PartAddProblems.BlockedAsLaser;
                        break;
                    case Direction.Right:
                        if (neighbours[1].Item1 != null) ret |= PartAddProblems.BlockedAsLaser;
                        break;
                    case Direction.Bottom:
                        if (neighbours[2].Item1 != null) ret |= PartAddProblems.BlockedAsLaser;
                        break;
                    default:
                        if (neighbours[3].Item1 != null) ret |= PartAddProblems.BlockedAsLaser;
                        break;
                }
            }

            //if the part is an engine it can't have a part right behind it (note: you can only have engines facing top, thus no other directions need be checked)
            if (part is Engine && neighbours[2].Item1 != null)
            {
                ret |= PartAddProblems.BlockedAsEngine;
            }

            //check if the part is not obscured from any direction but it has at least one valid connection, also check if the field is being blocked by a laser or engine
            Part matchingPart = null;

            foreach ((Part, Direction) neighbour in neighbours)
            {
                if (neighbour.Item1 != null)
                {
                    if (neighbour.Item2 == Direction.Bottom && neighbour.Item1 is Engine)
                    {
                        ret |= PartAddProblems.BlocksEngine;
                    }

                    if (neighbour.Item1 is Laser && neighbour.Item1.Rotation == neighbour.Item2)
                    {
                        ret |= PartAddProblems.BlocksLaser;
                    }

                    Connector onPart = part.GetConnector((Direction)(((int)neighbour.Item2 + 2) % 4));
                    int match = ConnectorsMatch(onPart, neighbour.Item1.GetConnector(neighbour.Item2));
                    if (matchingPart == null && match == 1)
                    {
                        matchingPart = neighbour.Item1;
                    }
                    else if (match == -1)
                    {
                        ret |= PartAddProblems.ConnectorsDontMatch;
                    }
                }
            }
            if (matchingPart == null)
            {
                ret |= PartAddProblems.HasNoConnection;
            }

            if (ret == PartAddProblems.None)
            {
                if (part is IActivatable)
                {
                    _activatableParts.Add(part);
                }
                else if (part is Storage)
                {
                    _storages.Add(part as Storage);
                }
                _parts[row, column] = part;
                part.Row = row;
                part.Column = column;
                foreach((Part, Direction) tuple in neighbours)
                {
                    if(tuple.Item1 != null && tuple.Item1.GetConnector(tuple.Item2) != Connector.None)
                    {
                        tuple.Item1.Neighbours.Add(part);
                        part.Neighbours.Add(tuple.Item1);
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Method to remove the part at the supplied indices, 
        /// as well as removing all the parts which are no longer connected to the cockpit without the removed part.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        public void RemovePartAtIndex(int row, int column)
        {
            Part removedPart = _parts[row, column];
            if (removedPart == null)
            {
                return;
            }

            if (removedPart is Cockpit)
            {
                _parts[row, column] = null;
                Wrecked?.Invoke(this, WreckedSource.CockpitHit);
                ++_penalty;
                return;
            }

            RemovePart(removedPart);

            CheckForNotConnectedParts();
            if (HumanCount == 0)
            {
                Wrecked?.Invoke(this, WreckedSource.OutOfHumans);
            }

        }

        #endregion

        #region private methods

        /// <summary>
        /// Breadth-first search to recognize which parts to remove
        /// </summary>
        private void CheckForNotConnectedParts()
        {
            List<Part> undiscoveredParts = _parts.Cast<Part>().Where(p => p != null).ToList();
            Queue<Part> queue = new Queue<Part>();

            Part startingPart = GetCockpit();
            undiscoveredParts.Remove(startingPart);
            queue.Enqueue(startingPart);
            while(queue.Count > 0)
            {
                Part item = queue.Dequeue();
                foreach(Part neighbour in item.Neighbours)
                {
                    if (undiscoveredParts.Contains(neighbour))
                    {
                        queue.Enqueue(neighbour);
                        undiscoveredParts.Remove(neighbour);
                    }
                }
            }

            foreach(Part p in undiscoveredParts)
            {
                RemovePart(p);
            }
            if (HumanCount == 0)
            {
                Wrecked?.Invoke(this, WreckedSource.OutOfHumans);
            }
            FlightAttributesChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Method to actually remove a part from the ship, while removing other references and effects
        /// </summary>
        /// <param name="current"></param>
        /// <param name="neighbours"></param>
        private void RemovePart(Part current)
        {
            if (current is IActivatable)
            {
                _activatableParts.Remove(current);
            }
            else if (current is Storage)
            {
                _storages.Remove(current as Storage);
            }
            if (current is EngineCabin || current is LaserCabin)
            {
                Personnel alienType = current is EngineCabin ? Personnel.EngineAlien : Personnel.LaserAlien;
                foreach (Part p in current.Neighbours)
                {
                    if (p is Cabin && (p as Cabin).Personnel == alienType)
                    {
                        (p as Cabin).Personnel = Personnel.None;
                        if (alienType == Personnel.LaserAlien)
                        {
                            _hasLaserAlien = false;
                        }
                        else
                        {
                            _hasEngineAlien = false;
                        }
                    }
                }
            }

            foreach(Part p in current.Neighbours)
            {
                p.Neighbours.Remove(current);
            }
            ++_penalty;
            _parts[current.Row, current.Column] = null;
            PartRemoved?.Invoke(this, new PartRemovedEventArgs(current.Row, current.Column));
        }

        /// <summary>
        /// Function to determine what the relation between two connectors is
        /// </summary>
        /// <param name="c1">The first connector</param>
        /// <param name="c2">The second connector</param>
        /// <returns>1 if the two connectors connect, 0 if they don't connect but they don't block each other either, -1 if they block each other</returns>
        private int ConnectorsMatch(Connector c1, Connector c2)
            => (c1, c2) switch
            {
                (Connector.Single, Connector.Double) => -1,
                (Connector.Double, Connector.Single) => -1,
                (Connector.None, Connector.None) => 0,
                (Connector.None, _) => -1,
                (_, Connector.None) => -1,
                (_, _) => 1
            };

        /// <summary>
        /// Function to spend a single energy if there is one available
        /// </summary>
        /// <returns>A logical value indicating whether an energy was spent</returns>
        private bool SpendEnergy()
        {
            Part battery = _parts.Cast<Part>().FirstOrDefault(p => p is Battery && (p as Battery).Charges > 0);
            if(battery == null)
            {
                return false;
            }
            (battery as Battery).UseCharge();
            return true;
        }

        private HashSet<Direction> GetShieldedDirections()
        {
            IEnumerable<Shield> activeShields = _parts.Cast<Part>().Where(p => p is Shield && (p as Shield).Activated).Select(p => p as Shield);
            HashSet<Direction> ret = new HashSet<Direction>();
            foreach(Shield item in activeShields)
            {
                ret.Add(item.Directions.Item1);
                ret.Add(item.Directions.Item2);
            }
            return ret;
        }
    }

    #endregion
}
