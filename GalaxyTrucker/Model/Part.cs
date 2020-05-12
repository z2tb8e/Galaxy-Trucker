using System;
using System.Collections.Generic;

namespace GalaxyTrucker.Model
{
    public abstract class Part
    {
        private readonly Connector[] _connectors;

        private Stack<Part> _path;

        public int Row { get; set; }

        public int Column { get; set; }

        public string ContentsDescription { get; set; }

        public Direction Rotation { get; private set; }

        public Connector[] Connectors => _connectors;

        public Stack<Part> Path { get { return _path; } protected set { _path = value; } }

        public event EventHandler HighlightToggled;

        public event EventHandler ContentsChanged;

        protected Part(Connector Top, Connector Right, Connector Bottom, Connector Left)
        {
            _connectors = new Connector[4]
            {
                Top, Right, Bottom, Left
            };
        }

        public Connector GetConnector(Direction dir)
        {
            int index = ((int)dir - (int)Rotation + 4) % 4;
            return _connectors[index];
        }

        public void Highlight()
        {
            HighlightToggled?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnContentsChanged()
        {
            ContentsChanged?.Invoke(this, EventArgs.Empty);
        }

        public virtual void Rotate(int leftOrRight)
        {
            int enumValue = ((int)Rotation + leftOrRight + 4) % 4;
            Rotation = (Direction)enumValue;
        }

        public void AddToPath(Part part)
        {
            Path = new Stack<Part>(part.Path);
            Path.Push(part);
        }

        public bool IsInPath(Part p) => _path.Contains(p);

        public override string ToString()
        {
            return $"{(int)_connectors[0]}{(int)_connectors[1]}{(int)_connectors[2]}{(int)_connectors[3]}";
        }
    }
}
