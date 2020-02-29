using System.Collections.Generic;

namespace Client.Model
{
    public abstract class Part
    {
        private readonly Connector[] _connectors;

        private List<Part> _path;

        public Direction Rotation { get; private set; }

        public List<Part> Path { get { return new List<Part>(_path); } }

        protected Part(Connector Top, Connector Right, Connector Bottom, Connector Left)
        {
            _path = new List<Part>();
            _connectors = new Connector[4];
            (_connectors[0], _connectors[1], _connectors[2], _connectors[3], this.Rotation) = (Top, Right, Bottom, Left, default);
        }

        public Connector GetConnector(Direction dir) => _connectors[((int)dir + (int)Rotation) % 4];

        public void Rotate(Direction dir) => this.Rotation = (Direction)((int)Rotation + (int)dir % 4);

        public void AppendPath(Part p) => _path.Add(p);

        public bool IsInPath(Part p) => _path.Contains(p);
    }
}
