using Client.Model.PartTypes;
using System.Collections.Generic;
using System.Linq;

namespace Client.Model
{
    public class Ship
    {
        private readonly bool[,] _movableFields;
        private Part[,] _parts;
        private readonly int _penaltyCap;
        private Part _cockpit;

        public List<Part> Penalty { get; set; }

        public int Firepower => _parts.Cast<Part>().Where(x => x is Laser).Sum(x => (x as Laser).Firepower);

        public int Enginepower => _parts.Cast<Part>().Where(x => x is Engine).Sum(x => (x as Engine).Enginepower);

        public int CrewCount => _parts.Cast<Part>().Where(x => x is Cabin).Sum(x => (x as Cabin).Personnel switch
        {
            Personnel.None => 0,
            Personnel.HumanDouble => 2,
            _ => 1
        });

        public Ship(ShipLayout layout)
        {
            (int, int) cockpit;
            (cockpit, _movableFields) = LayoutReader.GetLayout(layout);
            _parts = new Part[11, 11];
            _cockpit = _parts[cockpit.Item1, cockpit.Item2] = new Cockpit();
            Penalty = new List<Part>();
            _penaltyCap = layout switch
            {
                ShipLayout.Small => 5,
                ShipLayout.Medium => 8,
                _ => 11,
            };
        }

        public void ApplyPandemic()
        {
            List<(int, int)> alreadyApplied = new List<(int,int)>();
            for (int i = 0; i < _parts.GetLength(1); ++i)
            {
                for (int j = 0; j < _parts.GetLength(2); ++j)
                {
                    if(_parts[i,j] is Cabin && (_parts[i,j] as Cabin).Personnel != Personnel.None && !alreadyApplied.Contains((i,j)))
                    {
                        bool getsInfected =
                            (_parts[i - 1, j] is Cabin && alreadyApplied.Contains((i - 1, j))
                                    && 1 == ConnectorsMatch(_parts[i, j].GetConnector(Direction.Top), _parts[i - 1, j].GetConnector(Direction.Bottom)))
                            || (_parts[i, j + 1] is Cabin && (_parts[i, j + 1] as Cabin).Personnel != Personnel.None
                                    && 1 == ConnectorsMatch(_parts[i, j].GetConnector(Direction.Right), _parts[i, j + 1].GetConnector(Direction.Left)))
                            || (_parts[i + 1, j] is Cabin && (_parts[i + 1, j] as Cabin).Personnel != Personnel.None
                                    && 1 == ConnectorsMatch(_parts[i, j].GetConnector(Direction.Bottom), _parts[i + 1, j].GetConnector(Direction.Top)))
                            || (_parts[i, j - 1] is Cabin && alreadyApplied.Contains((i, j - 1))
                                    && 1 == ConnectorsMatch(_parts[i, j].GetConnector(Direction.Left), _parts[i, j - 1].GetConnector(Direction.Right)));
                        if (getsInfected)
                        {
                            alreadyApplied.Add((i, j));
                            (_parts[i, j] as Cabin).RemoveSinglePersonnel();
                        }
                    }
                }
            }
        }

        public bool AddPart(Part part, int pos1, int pos2)
        {
            if (!_movableFields[pos1, pos2] || _parts[pos1, pos2] != null)
                return false;

            Part matchingPart = null;
            bool isMismatched = false;
            if (_parts[pos1 - 1, pos2] != null)
            {
                int match = ConnectorsMatch(part.GetConnector(Direction.Top), _parts[pos1 - 1, pos2].GetConnector(Direction.Bottom));
                if (matchingPart == null && match == 1) matchingPart = _parts[pos1 - 1, pos2];
                else if (match == -1) isMismatched = true;
            }
            if (_parts[pos1, pos2 + 1] != null)
            {
                int match = ConnectorsMatch(part.GetConnector(Direction.Right), _parts[pos1, pos2 + 1].GetConnector(Direction.Left));
                if (matchingPart == null && match == 1) matchingPart = _parts[pos1, pos2 + 1];
                else if (match == -1) isMismatched = true;
            }
            if (_parts[pos1 + 1, pos2] != null)
            {
                int match = ConnectorsMatch(part.GetConnector(Direction.Bottom), _parts[pos1 + 1, pos2].GetConnector(Direction.Top));
                if (matchingPart == null && match == 1) matchingPart = _parts[pos1 - 1, pos2];
                else if (match == -1) isMismatched = true;
            }
            if (_parts[pos1, pos2 - 1] != null)
            {
                int match = ConnectorsMatch(part.GetConnector(Direction.Left), _parts[pos1, pos2 - 1].GetConnector(Direction.Right));
                if (matchingPart == null && match == 1) matchingPart = _parts[pos1, pos2 - 1];
                else if (match == -1) isMismatched = true;
            }

            if (matchingPart != null && !isMismatched)
            {
                _parts[pos1, pos2] = part;
                part.Path = matchingPart.Path;
                part.AddToPath(matchingPart);
                return true;
            }
            else return false;
        }

        public void RemovePartAtIndex(int pos1, int pos2)
        {
            if (_parts[pos1, pos2] == null)
                return;

            Part removedPart = _parts[pos1, pos2];
            _parts[pos1, pos2] = null;

            if (_parts[pos1 - 1, pos2] != null && _parts[pos1 - 1, pos2].Path.Contains(removedPart))
            {
                RemovePartsRecursive(pos1 - 1, pos2);
            }
            if (_parts[pos1, pos2 + 1] != null && _parts[pos1, pos2 + 1].Path.Contains(removedPart))
            {
                RemovePartsRecursive(pos1, pos2 + 1);
            }
            if (_parts[pos1 + 1, pos2] != null && _parts[pos1 + 1, pos2].Path.Contains(removedPart))
            {
                RemovePartsRecursive(pos1 + 1, pos2);
            }
            if (_parts[pos1, pos2 - 1] != null && _parts[pos1, pos2 - 1].Path.Contains(removedPart))
            {
                RemovePartsRecursive(pos1, pos2 - 1);
            }
        }

        private bool RemovePartsRecursive(int pos1, int pos2)
        {
            Part current = _parts[pos1, pos2];
            //In each direction check if
            if (_parts[pos1 - 1, pos2] != null)
            {
                //we find a part which is connected through the current element, then check deeper down the path if there is an alternative
                if(_parts[pos1 - 1, pos2].Path.Contains(current))
                {
                    if(RemovePartsRecursive(pos1 - 1, pos2))
                    {
                        current.Path = _parts[pos1 - 1, pos2].Path;
                        current.AddToPath(_parts[pos1 - 1, pos2]);
                        return true;
                    }
                }
                //we find a part which is not connected through the current element, but has a connection to it, then rebind to that path
                else if (1 == ConnectorsMatch(current.GetConnector(Direction.Top), _parts[pos1 - 1, pos2].GetConnector(Direction.Bottom)))
                {
                    current.Path = _parts[pos1 - 1, pos2].Path;
                    current.AddToPath(_parts[pos1 - 1, pos2]);
                    return true;
                }
            }
            if (_parts[pos1, pos2 + 1] != null)
            {
                if (_parts[pos1, pos2 + 1].Path.Contains(current))
                {
                    if(RemovePartsRecursive(pos1, pos2 + 1))
                    {
                        current.Path = _parts[pos1, pos2 + 1].Path;
                        current.AddToPath(_parts[pos1, pos2 + 1]);
                        return true;
                    }
                }
                else if (1 == ConnectorsMatch(current.GetConnector(Direction.Right), _parts[pos1, pos2 + 1].GetConnector(Direction.Left)))
                {
                    current.Path = _parts[pos1, pos2 + 1].Path;
                    current.AddToPath(_parts[pos1, pos2 + 1]);
                    return true;
                }
            }
            if (_parts[pos1 + 1, pos2] != null)
            {
                if (_parts[pos1 + 1, pos2].Path.Contains(current))
                {
                    if(RemovePartsRecursive(pos1 + 1, pos2))
                    {
                        current.Path = _parts[pos1 + 1, pos2].Path;
                        current.AddToPath(_parts[pos1 + 1, pos2]);
                        return true;
                    }
                }
                else if (1 == ConnectorsMatch(current.GetConnector(Direction.Bottom), _parts[pos1 + 1, pos2].GetConnector(Direction.Top)))
                {
                    current.Path = _parts[pos1 + 1, pos2].Path;
                    current.AddToPath(_parts[pos1 + 1, pos2]);
                    return true;
                }
            }
            if (_parts[pos1, pos2 - 1] != null)
            {

                if (_parts[pos1, pos2 - 1].Path.Contains(current))
                {
                    current.Path = _parts[pos1, pos2 - 1].Path;
                    current.AddToPath(_parts[pos1, pos2 - 1]);
                    return true;
                }
                else if (1 == ConnectorsMatch(current.GetConnector(Direction.Left), _parts[pos1, pos2 - 1].GetConnector(Direction.Right)))
                {
                    current.Path = _parts[pos1, pos2 - 1].Path;
                    current.AddToPath(_parts[pos1, pos2 - 1]);
                    return true;
                }
            }
            //if no alternative path is found, remove the part
            _parts[pos1, pos2] = null;
            return false;
        }

        private int ConnectorsMatch(Connector c1, Connector c2)
            => (c1, c2) switch
            {
                (Connector.Single, Connector.Double) => -1,
                (Connector.None, Connector.None) => 0,
                (Connector.None, _) => -1,
                (_, _) => 1
            };
    }
}
