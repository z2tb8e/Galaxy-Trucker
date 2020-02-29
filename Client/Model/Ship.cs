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
            _parts[cockpit.Item1, cockpit.Item2] = new Cockpit();
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

        public bool AddPart(Part part, int pos1, int pos2)
        {
            if (!_movableFields[pos1, pos2] || _parts[pos1, pos2] != null)
                return false;

            Part matchingPart = null;
            Part top = _parts[pos1 - 1, pos2];
            Part right = _parts[pos1, pos2 + 1];
            Part bottom = _parts[pos1 + 1, pos2];
            Part left = _parts[pos1, pos2 - 1];

            bool isMismatched = false;
            if (top != null)
            {
                int match = ConnectorsMatch(part.GetConnector(Direction.Top), top.GetConnector(Direction.Bottom));
                if (matchingPart == null && match == 1) matchingPart = top;
                else if (match == -1) isMismatched = true;
            }
            if (right != null)
            {
                int match = ConnectorsMatch(part.GetConnector(Direction.Right), right.GetConnector(Direction.Left));
                if (matchingPart == null && match == 1) matchingPart = right;
                else if (match == -1) isMismatched = true;
            }
            if (bottom != null)
            {
                int match = ConnectorsMatch(part.GetConnector(Direction.Bottom), bottom.GetConnector(Direction.Top));
                if (matchingPart == null && match == 1) matchingPart = bottom;
                else if (match == -1) isMismatched = true;
            }
            if (left != null)
            {
                int match = ConnectorsMatch(part.GetConnector(Direction.Left), left.GetConnector(Direction.Right));
                if (matchingPart == null && match == 1) matchingPart = left;
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
            Penalty.Add(removedPart);
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
            Part top = _parts[pos1 - 1, pos2];
            Part right = _parts[pos1, pos2 + 1];
            Part bottom = _parts[pos1 + 1, pos2];
            Part left = _parts[pos1, pos2 - 1];
            Part current = _parts[pos1, pos2];
            //In each direction check if
            if (top != null)
            {
                //we find a part which is connected through the current element, then check deeper down the path if there is an alternative
                if (top.Path.Contains(current))
                {
                    if (RemovePartsRecursive(pos1 - 1, pos2))
                    {
                        current.Path = top.Path;
                        current.AddToPath(top);
                        return true;
                    }
                }
                //we find a part which is not connected through the current element, but has a connection to it, then rebind to that path
                else if (1 == ConnectorsMatch(current.GetConnector(Direction.Top), top.GetConnector(Direction.Bottom)))
                {
                    current.Path = top.Path;
                    current.AddToPath(top);
                    return true;
                }
            }
            if (right != null)
            {
                if (right.Path.Contains(current))
                {
                    if(RemovePartsRecursive(pos1, pos2 + 1))
                    {
                        current.Path = right.Path;
                        current.AddToPath(right);
                        return true;
                    }
                }
                else if (1 == ConnectorsMatch(current.GetConnector(Direction.Right), right.GetConnector(Direction.Left)))
                {
                    current.Path = right.Path;
                    current.AddToPath(right);
                    return true;
                }
            }
            if (bottom != null)
            {
                if (bottom.Path.Contains(current))
                {
                    if(RemovePartsRecursive(pos1 + 1, pos2))
                    {
                        current.Path = bottom.Path;
                        current.AddToPath(bottom);
                        return true;
                    }
                }
                else if (1 == ConnectorsMatch(current.GetConnector(Direction.Bottom), bottom.GetConnector(Direction.Top)))
                {
                    current.Path = bottom.Path;
                    current.AddToPath(bottom);
                    return true;
                }
            }
            if (left != null)
            {

                if (left.Path.Contains(current))
                {
                    if(RemovePartsRecursive(pos1, pos2 - 2))
                    {
                        current.Path = left.Path;
                        current.AddToPath(left);
                        return true;
                    }
                }
                else if (1 == ConnectorsMatch(current.GetConnector(Direction.Left), left.GetConnector(Direction.Right)))
                {
                    current.Path = left.Path;
                    current.AddToPath(left);
                    return true;
                }
            }
            //if no alternative path is found, remove the part
            Penalty.Add(current);
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
