using GalaxyTrucker.Exceptions;
using GalaxyTrucker.Model.PartTypes;
using System;
using System.Collections.Generic;
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

        private readonly bool[] _shieldedDirections;

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

        /// <summary>
        /// Constructor for the ship class, setting the fields in which parts can be placed in, as well as placing the cockpit and setting the player colour.
        /// </summary>
        /// <param name="layout">The layout of the ship</param>
        /// <param name="color">The color indicating the owner of the ship</param>
        public Ship(ShipLayout layout, PlayerColor color)
        {
            _shieldedDirections = Enumerable.Repeat(false, 4).ToArray();
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

        #region public methods

        public bool HighlightCabinsForAlien(Personnel alien)
        {
            if(alien != Personnel.EngineAlien && alien != Personnel.LaserAlien)
            {
                throw new ArgumentException("The Personnel argument is not an alien!");
            }

            if ((_hasEngineAlien && alien == Personnel.EngineAlien) || (_hasLaserAlien && alien == Personnel.LaserAlien))
            {
                return false;
            }

            bool ret = false;
            IEnumerable<Part> cabins = _parts.Cast<Part>().Where(p => p is Cabin && !(p is Cockpit));
            foreach(Part cabin in cabins)
            {
                (Part, Direction)[] neighbours = new (Part, Direction)[]
                {
                    (_parts[cabin.Row - 1, cabin.Column], Direction.Top),
                    (_parts[cabin.Row, cabin.Column + 1], Direction.Right),
                    (_parts[cabin.Row + 1, cabin.Column], Direction.Bottom),
                    (_parts[cabin.Row, cabin.Column - 1], Direction.Left)
                };
                foreach((Part, Direction) pair in neighbours)
                {
                    if(cabin.GetConnector(pair.Item2) != Connector.None)
                    {
                        if(alien == Personnel.EngineAlien && pair.Item1 is EngineCabin)
                        {
                            ret = true;
                            cabin.Highlight();
                        }
                        else if (alien == Personnel.LaserAlien && pair.Item1 is LaserCabin)
                        {
                            ret = true;
                            cabin.Highlight();
                        }
                    }
                }
            }
            return ret;
        }

        public Part GetCockpit()
        {
            return _parts.Cast<Part>().First(p => p is Cockpit);
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
            int sum = 0;
            foreach (Part p in _parts.Cast<Part>().Where(p => p != null))
            {
                (Part, Direction)[] neighbours = new (Part, Direction)[]
                {
                    (_parts[p.Row - 1, p.Column], Direction.Top),
                    (_parts[p.Row, p.Column + 1], Direction.Right),
                    (_parts[p.Row + 1, p.Column], Direction.Bottom),
                    (_parts[p.Row, p.Column - 1], Direction.Left)
                };
                foreach((Part, Direction) pair in neighbours)
                {
                    if(pair.Item1 == null && p.Connectors[(int)pair.Item2] != Connector.None)
                    {
                        ++sum;
                    }
                }
            }
            return sum;
        }

        /// <summary>
        /// Method to add to the cabin at the supplied indices, given it has the neccessary alien cabin neighbouring it.
        /// </summary>
        /// <param name="row">The row of the cabin</param>
        /// <param name="column">The column of the cabin</param>
        /// <param name="alien">The type of alien to add</param>
        public void AddAlien(int row, int column, Personnel alien)
        {
            if (_hasEngineAlien && alien == Personnel.EngineAlien || _hasLaserAlien && alien == Personnel.LaserAlien)
            {
                throw new DuplicateAlienException(alien, (row, column));
            }

            if (!(_parts[row, column] is Cabin))
                throw new InvalidIndexException($"Part at ({row},{column}) is not a cabin.");

            else if(_parts[row,column] is Cockpit)
                throw new InvalidIndexException("The cockpit must have humans as personnel");

            Part[] neighbours = new Part[]
            {
                _parts[row - 1, column],
                _parts[row, column + 1],
                _parts[row + 1, column],
                _parts[row, column - 1]
            };

            if (alien == Personnel.EngineAlien)
            {
                Part cabin;
                try
                {
                    cabin = neighbours.Cast<Part>().First(x => x is EngineCabin);
                    _hasEngineAlien = true;
                    (_parts[row, column] as Cabin).Personnel = Personnel.EngineAlien;
                }
                catch (Exception)
                {
                    throw new InvalidIndexException($"Cabin at ({row},{column}) does not have the required neighbouring alien cabin");
                }
            }
            else if (alien == Personnel.LaserAlien)
            {
                Part cabin;
                try
                {
                    cabin = neighbours.Cast<Part>().First(x => x is LaserCabin);
                    _hasLaserAlien = true;
                    (_parts[row, column] as Cabin).Personnel = Personnel.LaserAlien;
                }
                catch (Exception)
                {
                    throw new InvalidIndexException($"Cabin at ({row},{column}) does not have the required neighbouring alien cabin");
                }
            }
            else throw new ArgumentException("Argument is not an alien", nameof(alien));
        }

        /// <summary>
        /// Method to fill the unfilled cabins with human personnel.
        /// </summary>
        public void FillCabins()
        {
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
            for (int i = 0; i < 4; ++i)
            {
                _shieldedDirections[i] = false;
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
            List<Part> alreadyApplied = new List<Part>();
            for (int i = 0; i < _parts.GetLength(1); ++i)
            {
                for (int j = 0; j < _parts.GetLength(2); ++j)
                {
                    Part top = _parts[i - 1, j];
                    Part right = _parts[i, j + 1];
                    Part bottom = _parts[i + 1, j];
                    Part left = _parts[i, j - 1];
                    Part current = _parts[i, j];

                    if (current is Cabin && (current as Cabin).Personnel != Personnel.None && !alreadyApplied.Contains(current))
                    {
                        bool getsInfected =
                            (top is Cabin && alreadyApplied.Contains(top)
                                    && 1 == ConnectorsMatch(current.GetConnector(Direction.Top), top.GetConnector(Direction.Bottom)))
                            || (right is Cabin && (right as Cabin).Personnel != Personnel.None
                                    && 1 == ConnectorsMatch(current.GetConnector(Direction.Right), right.GetConnector(Direction.Left)))
                            || (bottom is Cabin && (bottom as Cabin).Personnel != Personnel.None
                                    && 1 == ConnectorsMatch(current.GetConnector(Direction.Bottom), bottom.GetConnector(Direction.Top)))
                            || (left is Cabin && alreadyApplied.Contains(left)
                                    && 1 == ConnectorsMatch(current.GetConnector(Direction.Left), left.GetConnector(Direction.Right)));
                        if (getsInfected)
                        {
                            alreadyApplied.Add(current);
                            (current as Cabin).RemoveSinglePersonnel();
                        }
                    }
                }
            }
            if(HumanCount == 0)
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
            //if the projectile comes from the left or from the top, we check starting from the index 0, else from the maximum index
            int row = dir switch
            {
                Direction.Top => 0,
                Direction.Bottom => _parts.GetLength(0),
                _ => line
            };
            int column = dir switch
            {
                Direction.Left => 0,
                Direction.Right => _parts.GetLength(1),
                _ => line
            };
            bool endOfLine = false;
            Part target = null;
            while (!endOfLine && target == null)
            {
                target = _parts[row, column];
                if(target != null)
                {
                    endOfLine = dir switch
                    {
                        Direction.Top => ++row < _parts.GetLength(0),
                        Direction.Right => --column >= 0,
                        Direction.Bottom => --row >= 0,
                        _ => ++column < _parts.GetLength(1)
                    };
                }
            }

            //if no part was in the way no further action is required
            if (target == null)
                return;

            //check if the part gets removed based on the type of projectile it got hit by
            //small asteroids remove the part only if it has an open connection towards it and the direction isn't shielded
            //large asteroids remove the part unless there is a laser in that line facing it
            //small shots remove the part unless the direction is shielded
            //large shots always remove the part
            switch (projectile)
            {
                case Projectile.MeteorSmall:
                    if(target.GetConnector(dir) != Connector.None && !_shieldedDirections[(int)dir])
                        RemovePartAtIndex(row, column);
                    break;
                case Projectile.MeteorLarge:
                    if (IsLaserInLine(line, dir))
                        RemovePartAtIndex(row, column);
                    break;
                case Projectile.ShotSmall:
                    if (!_shieldedDirections[(int)dir])
                        RemovePartAtIndex(row, column);
                    break;
                default:
                    RemovePartAtIndex(row, column);
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
        public void RemoveWares(int count)
        {
            if(_storages.Count == 0)
            {
                return;
            }
            int amountLeft = count;
            Ware max;
            while (amountLeft > 0 && (max = _storages.Max(x => x.Max)) != Ware.Empty)
            {
                int i = 0;
                while(i < _storages.Count() && _storages[i].Max == max && amountLeft > 0)
                    _storages[i].RemoveMax();
                ++i;
            }
        }

        /// <summary>
        /// Method to try to remove personnel ship until there is none left or the supplied amount was removed
        /// </summary>
        /// <param name="number">The number of personnel to remove</param>
        /// <returns>The number of personnel actually removed</returns>
        public int RemovePersonnel(int number)
        {
            int removeLeft = number;
            while(CrewCount > 0 || removeLeft > 0)
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
        public void ActivatePart(int row, int column)
        {
            Part current = _parts[row, column];
            if (!(current is IActivatable))
                throw new InvalidIndexException($"Part at ({row},{column}) is not an activatable part.");
            switch (current)
            {
                case LaserDouble l:
                    if (l.Activated)
                        return;
                    if(SpendEnergy())
                        l.Activate();
                    break;
                case EngineDouble e:
                    if (e.Activated)
                        return;
                    if (SpendEnergy())
                        e.Activate();
                    break;
                case Shield s:
                    if (s.Activated)
                        return;
                    if (SpendEnergy())
                    {
                        s.Activate();
                        _shieldedDirections[(int)s.Directions.Item1] = true;
                        _shieldedDirections[(int)s.Directions.Item2] = true;
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
            Part removedPart = _parts[row, column] ?? throw new InvalidIndexException($"Part at ({row},{column}) is null.");

            if (removedPart is Cockpit)
            {
                _parts[row, column] = null;
                Wrecked?.Invoke(this, WreckedSource.CockpitHit);
                _penalty = Math.Min(_penaltyCap, _parts.Cast<Part>().Where(p => p != null).Count());
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
        /// Method to actually remove a part from the ship, while removing other references and influences
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
        /// Function to check if there is an active laser facing the given direction in the given line
        /// </summary>
        /// <param name="line">The line in which the laser should be</param>
        /// <param name="dir">The direction the laser should be facing</param>
        /// <returns>A logical value indicating if the laser exists</returns>
        private bool IsLaserInLine(int line, Direction dir)
        {
            Part current;
            switch (dir)
            {
                case Direction.Top:
                    for (int i = 0; i < _parts.GetLength(0); ++i)
                    {
                        current = _parts[i, line];
                        if (current is Laser && current.Rotation == dir && (current as Laser).Firepower > 0)
                            return true;
                    }
                    break;
                case Direction.Right:
                    for (int j = _parts.GetLength(1) - 1; j >= 0; --j)
                    {
                        current = _parts[line, j];
                        if (current is Laser && current.Rotation == dir && (current as Laser).Firepower > 0)
                            return true;
                    }
                    break;
                case Direction.Bottom:
                    for (int i = _parts.GetLength(0) - 1; i >= 0; --i)
                    {
                        current = _parts[i, line];
                        if (current is Laser && current.Rotation == dir && (current as Laser).Firepower > 0)
                            return true;
                    }
                    break;
                default:
                    for (int j = 0; j < _parts.GetLength(1); ++j)
                    {
                        current = _parts[line, j];
                        if (current is Laser && current.Rotation == dir && (current as Laser).Firepower > 0)
                            return true;
                    }
                    break;
            }
            return false;
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
            Part p;
            try
            {
                p = _parts.Cast<Part>().First(x => x is Battery && (x as Battery).Charges > 0);
            }
            catch (Exception)
            {
                return false;
            }
            (p as Battery).UseCharge();
            return true;
        }
    }

    #endregion
}
