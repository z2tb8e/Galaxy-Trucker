﻿using GalaxyTrucker.Exceptions;
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

        #endregion

        #region properties

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

        public int Batteries => _parts.Cast<Part>().Where(x => x is Battery).Sum(x => (x as Battery).Capacity);

        public List<(int, int)> InactiveShields => _activatableParts.Cast<Part>()
            .Where(x => x is Shield && !(x as Shield).Activated).Select(x => (x.Row, x.Column)).ToList();

        public List<(int, int)> InactiveLasers => _activatableParts.Cast<Part>()
            .Where(x => x is LaserDouble && !(x as LaserDouble).Activated).Select(x => (x.Row, x.Column)).ToList();

        public List<(int, int)> InactiveEngines => _activatableParts.Cast<Part>()
            .Where(x => x is EngineDouble && !(x as EngineDouble).Activated).Select(x => (x.Row, x.Column)).ToList();

        #endregion

        #region events

        public event EventHandler<WreckedSource> ShipWrecked;

        public event EventHandler<PartRemovedEventArgs> PartRemoved;

        #endregion

        /// <summary>
        /// Constructor for the ship class, setting the fields in which parts can be placed in, as well as placing the cockpit and setting the player colour.
        /// </summary>
        /// <param name="layout">The layout of the ship</param>
        /// <param name="color">The color indicating the owner of the ship</param>
        public Ship(ShipLayout layout, PlayerColor color)
        {
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
            if((alien == Personnel.EngineAlien && _hasEngineAlien) || (alien == Personnel.LaserAlien && _hasLaserAlien))
            {
                throw new ArgumentException("The Personnel argument is not an alien!");
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
            return _parts.Cast<Part>().Where(p => p is Cockpit).FirstOrDefault();
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
            foreach(Part p in _parts)
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
                    cabin = neighbours.Cast<Part>().Where(x => x is EngineCabin).First();
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
                    cabin = neighbours.Cast<Part>().Where(x => x is LaserCabin).First();
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
        }

        /// <summary>
        /// Method to apply the effects of a projectile hitting the ship from a given direction the given line.
        /// </summary>
        /// <param name="projectile">The type of projectile the ship is getting hit by</param>
        /// <param name="dir">The direction the projectile is coming from</param>
        /// <param name="line">The line (vertical or horizontal) in which the projectile is approaching</param>
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

            //determine which directions the ship is shielded from
            bool[] isShielded = new bool[]
            {
                _parts.Cast<Part>().Where(x => x is Shield && (x as Shield).Activated &&
                    (x as Shield).Directions.Item1 == Direction.Top || (x as Shield).Directions.Item2 == Direction.Top).Any(),
                _parts.Cast<Part>().Where(x => x is Shield && (x as Shield).Activated &&
                    (x as Shield).Directions.Item1 == Direction.Right || (x as Shield).Directions.Item2 == Direction.Right).Any(),
                _parts.Cast<Part>().Where(x => x is Shield && (x as Shield).Activated &&
                    (x as Shield).Directions.Item1 == Direction.Bottom || (x as Shield).Directions.Item2 == Direction.Bottom).Any(),
                _parts.Cast<Part>().Where(x => x is Shield && (x as Shield).Activated &&
                    (x as Shield).Directions.Item1 == Direction.Left || (x as Shield).Directions.Item2 == Direction.Left).Any(),
            };

            //check if the part gets removed based on the type of projectile it got hit by
            //small asteroids remove the part only if it has an open connection towards it and the direction isn't shielded
            //large asteroids remove the part unless there is a laser in that line facing it
            //small shots remove the part unless the direction is shielded
            //large shots always remove the part
            switch (projectile)
            {
                case Projectile.MeteorSmall:
                    if(target.GetConnector(dir) != Connector.None && !isShielded[(int)dir])
                        RemovePartAtIndex(row, column);
                    break;
                case Projectile.MeteorLarge:
                    if (IsLaserInLine(line, dir))
                        RemovePartAtIndex(row, column);
                    break;
                case Projectile.ShotSmall:
                    if (!isShielded[(int)dir])
                        RemovePartAtIndex(row, column);
                    break;
                default:
                    RemovePartAtIndex(row, column);
                    break;
            }
        }

        /// <summary>
        /// Method to add a list of wares to the ship's storages while maximizing the value of the stored wares.
        /// </summary>
        /// <param name="wares">The list of wares to add</param>
        public void AddWares(List<Ware> wares)
        {
            foreach(Ware w in wares)
            {
                Ware min = _storages.Min(x => x.Min);
                if(w > min)
                {
                    Storage target = _storages.Find(x => x.Min == min && (w != Ware.Red || (w == Ware.Red && x is SpecialStorage)));
                    target.AddWare(w);
                }
            }
        }

        /// <summary>
        /// Method to remove the supplied number of wares from the ship, prioritizing the highest value wares.
        /// </summary>
        /// <param name="count">The number of wares to remove</param>
        public void RemoveWares(int count)
        {
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
        /// Method to remove a single personnel from cabin at the supplied indices.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns>True, if the part was a cabin, and a crew member could be removed, false otherwise</returns>
        public bool RemoveSinglePersonnel(int row, int column)
        {
            Part selected = _parts[row, column];
            if (selected is Cabin)
                return (selected as Cabin).RemoveSinglePersonnel();
            return false;
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
                        s.Activate();
                    break;
            }
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
                part.CreatePath(matchingPart);
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

            ++_penalty;
            _parts[row, column] = null;

            if (removedPart is Cockpit)
            {
                ShipWrecked?.Invoke(this, WreckedSource.CockpitHit);
                _penalty = Math.Min(_penaltyCap, _parts.Cast<Part>().Where(p => p != null).Count());
                return;
            }
            else if (removedPart is IActivatable)
            {
                _activatableParts.Remove(removedPart);
            }
            else if (removedPart is Storage)
            {
                _storages.Remove(removedPart as Storage);
            }

            PartRemoved?.Invoke(this, new PartRemovedEventArgs(removedPart.Row, removedPart.Column));

            Part[] neighbours = new Part[]
            {
                _parts[row - 1, column],
                _parts[row, column + 1],
                _parts[row + 1, column],
                _parts[row, column - 1]
            };

            if (removedPart is EngineCabin || removedPart is LaserCabin)
            {
                Personnel alienType = removedPart is EngineCabin ? Personnel.EngineAlien : Personnel.LaserAlien;
                foreach (Part p in neighbours)
                {
                    if(p is Cabin && (p as Cabin).Personnel == alienType)
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

            foreach (Part p in neighbours)
            {
                if (p != null && p.Path.Contains(removedPart))
                {
                    RemovePartsRecursive(p, removedPart);
                }
            }
            if (HumanCount == 0)
            {
                ShipWrecked?.Invoke(this, WreckedSource.OutOfHumans);
            }

        }

        #endregion

        #region private methods

        /// <summary>
        /// Function to recursively try to find a new path to the cockpit for the current element, and all parts nearby which have the current element in their path
        /// </summary>
        /// <param name="current">The current part being checked whether it's removed</param>
        /// <returns>A logical value indicating if the current part got removed</returns>
        private bool RemovePartsRecursive(Part current, Part origin)
        {
            (Part,Direction)[] neighbours = new (Part,Direction)[]
            {
                (_parts[current.Row - 1, current.Column], Direction.Bottom),
                (_parts[current.Row, current.Column + 1], Direction.Left),
                (_parts[current.Row + 1, current.Column], Direction.Top),
                (_parts[current.Row, current.Column - 1], Direction.Right)
            };

            //in each direction check if
            foreach((Part,Direction) neighbour in neighbours)
            {
                if(neighbour.Item1 != null && neighbour.Item1 != origin)
                {
                    //we find a part which is connected through the current element, then check deeper down the path if there is an alternative
                    if (neighbour.Item1.Path.Contains(current))
                    {
                        if (RemovePartsRecursive(neighbour.Item1, current))
                        {
                            current.CreatePath(neighbour.Item1);
                            return true;
                        }
                    }
                    //we find a part which is not connected through the current element, but has a connection to it, then rebind to that path
                    else if (1 == ConnectorsMatch(current.GetConnector((Direction)(((int)neighbour.Item2 + 2) % 4)), neighbour.Item1.GetConnector(neighbour.Item2)))
                    {
                        current.CreatePath(neighbour.Item1);
                        return true;
                    }
                }
            }

            //if no alternative path is found, remove the part
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
                foreach ((Part,Direction) p in neighbours)
                {
                    if (p.Item1 is Cabin && (p.Item1 as Cabin).Personnel == alienType)
                    {
                        (p.Item1 as Cabin).Personnel = Personnel.None;
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

            ++_penalty;
            _parts[current.Row, current.Column] = null;
            PartRemoved?.Invoke(this, new PartRemovedEventArgs(current.Row, current.Column));
            return false;
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
                p = _parts.Cast<Part>().Where(x => x is Battery && (x as Battery).Charges > 0).First();
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
