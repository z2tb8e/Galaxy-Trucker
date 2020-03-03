using Client.Exceptions;
using Client.Model.PartTypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Client.Model
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

        #endregion

        #region properties

        public int Penalty => _penalty > _penaltyCap ? _penaltyCap : _penalty;

        public int Firepower
        {
            get
            {
                int sum = _parts.Cast<Part>().Where(x => x is Laser).Sum(x => (x as Laser).Firepower);
                sum += sum > 0 && HasLaserAlien ? 2 : 0;
                return sum;
            }
        }

        public int Enginepower
        {
            get
            {
                int sum = _parts.Cast<Part>().Where(x => x is Engine).Sum(x => (x as Engine).Enginepower);
                sum += sum > 0 && HasEngineAlien ? 2 : 0;
                return sum;
            }
        }

        public int CrewCount => _parts.Cast<Part>().Where(x => x is Cabin).Sum(x => (x as Cabin).Personnel switch
        {
            Personnel.None => 0,
            Personnel.HumanDouble => 2,
            _ => 1
        });

        public List<(int, int)> InactiveShields => _activatableParts.Cast<Part>()
            .Where(x => x is Shield && !(x as Shield).Activated).Select(x => (x.Pos1, x.Pos2)).ToList();

        public List<(int, int)> InactiveLasers => _activatableParts.Cast<Part>()
            .Where(x => x is LaserDouble && !(x as LaserDouble).Activated).Select(x => (x.Pos1, x.Pos2)).ToList();

        public List<(int, int)> InactiveEngines => _activatableParts.Cast<Part>()
            .Where(x => x is EngineDouble && !(x as EngineDouble).Activated).Select(x => (x.Pos1, x.Pos2)).ToList();

        private bool HasEngineAlien => _parts.Cast<Part>().Where(x => x is Cabin).Any(x => (x as Cabin).Personnel == Personnel.EngineAlien);

        private bool HasLaserAlien => _parts.Cast<Part>().Where(x => x is Cabin).Any(x => (x as Cabin).Personnel == Personnel.LaserAlien);

        #endregion

        /// <summary>
        /// Constructor for the ship class, setting the fields in which parts can be placed in, as well as placing the cockpit and setting the player colour
        /// </summary>
        /// <param name="layout">The layout of the ship</param>
        /// <param name="color">The color indicating the owner of the ship</param>
        public Ship(ShipLayout layout, PlayerColor color)
        {
            (int, int) cockpit;
            (cockpit, _movableFields) = LayoutReader.GetLayout(layout);
            _activatableParts = new List<Part>();
            _storages = new List<Storage>();
            _parts = new Part[11, 11];
            _parts[cockpit.Item1, cockpit.Item2] = new Cockpit(color);
            _penalty = 0;
            _penaltyCap = layout switch
            {
                ShipLayout.Small => 5,
                ShipLayout.Medium => 8,
                _ => 11,
            };
        }

        #region public methods

        /// <summary>
        /// Method to add to the cabin at the supplied indices, given it has the neccessary alien cabin neighbouring it
        /// </summary>
        /// <param name="pos1">The first index of the cabin</param>
        /// <param name="pos2">The second index of the cabin</param>
        /// <param name="alien">The type of alien to add</param>
        public void AddAlien(int pos1, int pos2, Personnel alien)
        {
            if (!(_parts[pos1, pos2] is Cabin))
                throw new InvalidIndexException("Part at (" + pos1.ToString() + "," + pos2.ToString() + ") is not a cabin.");

            Part[] neighbours = new Part[]
            {
                _parts[pos1 - 1, pos2],
                _parts[pos1, pos2 + 1],
                _parts[pos1 + 1, pos2],
                _parts[pos1, pos2 - 1]
            };

            if (alien == Personnel.EngineAlien)
            {
                Part cabin;
                try
                {
                    cabin = neighbours.Cast<Part>().Where(x => x != null && x is EngineCabin && !(x as EngineCabin).EffectUsed).First();
                }
                catch (Exception)
                {
                    cabin = null;
                }
                if (cabin != null)
                {
                    (cabin as EngineCabin).EffectUsed = true;
                    (_parts[pos1, pos2] as Cabin).Personnel = Personnel.EngineAlien;
                }
                else throw new InvalidIndexException("Cabin at (" + pos1.ToString() + "," + pos2.ToString() + ") does not have the required neighbouring alien cabin");
            }
            else if (alien == Personnel.LaserAlien)
            {
                Part cabin;
                try
                {
                    cabin = neighbours.Cast<Part>().Where(x => x != null && x is LaserCabin && !(x as LaserCabin).EffectUsed).First();
                }
                catch (Exception)
                {
                    cabin = null;
                }
                if (cabin != null)
                {
                    (cabin as LaserCabin).EffectUsed = true;
                    (_parts[pos1, pos2] as Cabin).Personnel = Personnel.LaserAlien;
                }
                else throw new InvalidIndexException("Cabin at (" + pos1.ToString() + "," + pos2.ToString() + ") does not have the required neighbouring alien cabin");
            }
            else throw new ArgumentException("Argument is not an alien", "alien");
        }

        /// <summary>
        /// Method to fill the unfilled cabins with regular personnel
        /// </summary>
        public void FillCabins()
            => _parts.Cast<Part>().Where(x => x is Cabin && (x as Cabin).Personnel == Personnel.None)
            .Select(x => (x as Cabin).Personnel = Personnel.HumanDouble);

        /// <summary>
        /// Method to deactivate all activated parts
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
        /// Method to apply the effects of a pandemic event
        /// In case of a pandemic all cabins which are directly connected and have at least a single crew member, lose one personnel each
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
        /// Method to apply the effects of a projectile hitting the ship from a given direction the given line
        /// </summary>
        /// <param name="pj">The type of projectile the ship is getting hit by</param>
        /// <param name="dir">The direction the projectile is coming from</param>
        /// <param name="line">The line (vertical or horizontal) in which the projectile is approaching</param>
        public void ApplyProjectile(Projectile pj, Direction dir, int line)
        {
            //determine the part about to be hit
            //if the projectile comes from the left or from the top, we check starting from the index 0, else from the maximum index
            int ind1 = dir switch
            {
                Direction.Top => 0,
                Direction.Bottom => _parts.GetLength(0),
                _ => line
            };
            int ind2 = dir switch
            {
                Direction.Left => 0,
                Direction.Right => _parts.GetLength(1),
                _ => line
            };
            bool endOfLine = false;
            Part target = null;
            while (!endOfLine && target == null)
            {
                target = _parts[ind1, ind2];
                if(target != null)
                {
                    endOfLine = dir switch
                    {
                        Direction.Top => ++ind1 < _parts.GetLength(0),
                        Direction.Right => --ind2 >= 0,
                        Direction.Bottom => --ind1 >= 0,
                        _ => ++ind2 < _parts.GetLength(1)
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
            switch (pj)
            {
                case Projectile.AsteroidSmall:
                    if(target.GetConnector(dir) != Connector.None && !isShielded[(int)dir])
                        RemovePartAtIndex(ind1, ind2);
                    break;
                case Projectile.AsteroidLarge:
                    if (IsLaserInLine(line, dir))
                        RemovePartAtIndex(ind1, ind2);
                    break;
                case Projectile.ShotSmall:
                    if (!isShielded[(int)dir])
                        RemovePartAtIndex(ind1, ind2);
                    break;
                default:
                    RemovePartAtIndex(ind1, ind2);
                    break;
            }
        }

        /// <summary>
        /// Method to add a list of wares to the ship's storages while maximizing the value of the stored wares
        /// </summary>
        /// <param name="wares">The list of wares to add</param>
        public void AddWares(List<Ware> wares)
        {
            foreach(Ware w in wares)
            {
                Ware min = _storages.Min(x => x.Min);
                if(w > min)
                {
                    try
                    {
                        Storage target = _storages.Find(x => x.Min == min && (w != Ware.Red || (w == Ware.Red && x is SpecialStorage)));
                        target.AddWare(w);
                    }catch(Exception) { }
                }
            }
        }

        /// <summary>
        /// Method to remove the supplied number of wares from the ship, prioritizing the highest value wares
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
        /// Method to remove a single personnel from cabin at the supplied indices
        /// </summary>
        /// <param name="pos1">The first index of the cabin</param>
        /// <param name="pos2">The first index of the cabin</param>
        /// <returns>True, if the part was a cabin, and a crew member could be removed, false otherwise</returns>
        public bool RemoveSinglePersonnel(int pos1, int pos2)
        {
            Part selected = _parts[pos1, pos2];
            if (selected is Cabin)
                return (selected as Cabin).RemoveSinglePersonnel();
            return false;
        }

        /// <summary>
        /// Method to activate the part at the given indices, given it is an activatable part and it's currently inactive
        /// </summary>
        /// <param name="pos1"></param>
        /// <param name="pos2"></param>
        public void ActivatePart(int pos1, int pos2)
        {
            Part current = _parts[pos1, pos2];
            if (!(current is IActivatable))
                throw new InvalidIndexException("Part at (" + pos1.ToString() + "," + pos2.ToString() + ") is not an activatable part.");
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
        /// Function to add a new part to the ship at the supplied indices
        /// </summary>
        /// <param name="part">The part to add</param>
        /// <param name="pos1">The first index to add the part at</param>
        /// <param name="pos2">The second index to add the part at</param>
        /// <returns>A logical value indicating whether the part could be added at the given location</returns>
        public bool AddPart(Part part, int pos1, int pos2)
        {
            //check if the target is within bounds and is not occupied yet
            if (!_movableFields[pos1, pos2] || _parts[pos1, pos2] != null)
                return false;

            (Part, Direction)[] neighbours = new (Part, Direction)[]
            {
                (_parts[pos1 - 1, pos2], Direction.Bottom),
                (_parts[pos1, pos2 + 1], Direction.Left),
                (_parts[pos1 + 1, pos2], Direction.Top),
                (_parts[pos1, pos2 - 1], Direction.Right)
            };

            //if the part is a laser it can't have a part right in front of it
            if (part is Laser)
            {
                switch (part.Rotation)
                {
                    case Direction.Top:
                        if (neighbours[1].Item1 != null) return false;
                        break;
                    case Direction.Right:
                        if (neighbours[2].Item1 != null) return false;
                        break;
                    case Direction.Bottom:
                        if (neighbours[3].Item1 != null) return false;
                        break;
                    default:
                        if (neighbours[4].Item1 != null) return false;
                        break;
                }
            }

            //if the part is an engine it can't have a part right behind it (note: you can only have engines facing top, thus no other directions need be checked)
            if (part is Engine && neighbours[3].Item1 != null)
                return false;

            //check if the part is not obscured from any direction but it has at least one valid connection, also check if the field is being blocked by a laser or engine
            Part matchingPart = null;
            bool isMismatched = false;

            foreach((Part, Direction) neighbour in neighbours)
            {
                if(neighbour.Item1 is null)
                {
                    if (neighbour.Item2 == Direction.Bottom && neighbour.Item1 is Engine)
                        return false;
                    if (neighbour.Item1 is Laser && neighbour.Item1.Rotation == neighbour.Item2)
                        return false;
                    Connector onPart = part.GetConnector((Direction)((int)neighbour.Item2 + 2 % 4));
                    int match = ConnectorsMatch(onPart, neighbour.Item1.GetConnector(neighbour.Item2));
                    if (matchingPart == null && match == 1) matchingPart = neighbour.Item1;
                    else if (match == -1) isMismatched = true;
                }
            }

            if (matchingPart != null && !isMismatched)
            {
                if (part is IActivatable)
                    _activatableParts.Add(part);
                else if (part is Storage)
                    _storages.Add(part as Storage);
                _parts[pos1, pos2] = part;
                part.Pos1 = pos1;
                part.Pos2 = pos2;
                part.Path = matchingPart.Path;
                part.AddToPath(matchingPart);
                return true;
            }
            else return false;
        }

        /// <summary>
        /// Method to remove the part at the supplied indices, 
        /// as well as removing all the parts which are no longer connected to the cockpit without the removed part
        /// </summary>
        /// <param name="pos1">The first index of the part to remove</param>
        /// <param name="pos2">The second index of the part to remove</param>
        public void RemovePartAtIndex(int pos1, int pos2)
        {
            if (_parts[pos1, pos2] == null)
                throw new InvalidIndexException("Part at (" + pos1.ToString() + "," + pos2.ToString() + ") is null.");

            Part removedPart = _parts[pos1, pos2];
            if (removedPart is IActivatable)
                _activatableParts.Remove(removedPart);
            else if (removedPart is Storage)
                _storages.Remove(removedPart as Storage);
            ++_penalty;
            _parts[pos1, pos2] = null;

            Part[] neighbours = new Part[]
            {
                _parts[pos1 - 1, pos2],
                _parts[pos1, pos2 + 1],
                _parts[pos1 + 1, pos2],
                _parts[pos1, pos2 - 1]
            };

            foreach (Part p in neighbours)
                if (p != null && p.Path.Contains(removedPart))
                    RemovePartsRecursive(p);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Function to recursively try to find a new path to the cockpit for the current element, and all parts nearby which have the current element in their path
        /// </summary>
        /// <param name="current">The current part being checked whether it's removed</param>
        /// <returns>A logical value indicating if the current part got removed</returns>
        private bool RemovePartsRecursive(Part current)
        {
            (Part,Direction)[] neighbours = new (Part,Direction)[]
            {
                (_parts[current.Pos1 - 1, current.Pos2], Direction.Bottom),
                (_parts[current.Pos1, current.Pos2 + 1], Direction.Left),
                (_parts[current.Pos1 + 1, current.Pos2], Direction.Top),
                (_parts[current.Pos1, current.Pos2 - 1], Direction.Right)
            };

            //in each direction check if
            foreach((Part,Direction) neighbour in neighbours)
            {
                if(neighbour.Item1 != null)
                {
                    //we find a part which is connected through the current element, then check deeper down the path if there is an alternative
                    if (neighbour.Item1.Path.Contains(current))
                    {
                        if (RemovePartsRecursive(neighbour.Item1))
                        {
                            current.Path = neighbour.Item1.Path;
                            current.AddToPath(neighbour.Item1);
                            return true;
                        }
                    }
                    //we find a part which is not connected through the current element, but has a connection to it, then rebind to that path
                    else if (1 == ConnectorsMatch(current.GetConnector((Direction)((int)neighbour.Item2 + 2 % 4)), neighbour.Item1.GetConnector(neighbour.Item2)))
                    {
                        current.Path = neighbour.Item1.Path;
                        current.AddToPath(neighbour.Item1);
                        return true;
                    }
                }
            }

            //if no alternative path is found, remove the part
            if (current is IActivatable)
                _activatableParts.Remove(current);
            else if (current is Storage)
                _storages.Remove(current as Storage);
            ++_penalty;
            _parts[current.Pos1, current.Pos2] = null;
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
                (Connector.None, Connector.None) => 0,
                (Connector.None, _) => -1,
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
