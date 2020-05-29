using System;
using System.Collections.Generic;

namespace GalaxyTrucker.Model
{
    public abstract class Part
    {
        private readonly Connector[] _connectors;

        public int Row { get; set; }

        public int Column { get; set; }

        public string ContentsDescription { get; set; }

        public Direction Rotation { get; private set; }

        public Connector[] Connectors => _connectors;

        public List<Part> Neighbours { get; set; }

        public event EventHandler HighlightToggled;

        public event EventHandler ContentsChanged;

        public Part(Connector Top, Connector Right, Connector Bottom, Connector Left)
        {
            _connectors = new Connector[4]
            {
                Top, Right, Bottom, Left
            };
            Neighbours = new List<Part>();
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

        /// <summary>
        /// Method to rotate the part 90° left or right
        /// </summary>
        /// <param name="leftOrRight">-1, if left, 1, if right</param>
        public virtual void Rotate(int leftOrRight)
        {
            if(leftOrRight != 1 && leftOrRight != -1)
            {
                throw new ArgumentOutOfRangeException();
            }
            int enumValue = ((int)Rotation + leftOrRight + 4) % 4;
            Rotation = (Direction)enumValue;
        }

        public override string ToString()
        {
            return $"{(int)_connectors[0]}{(int)_connectors[1]}{(int)_connectors[2]}{(int)_connectors[3]}";
        }
    }
}
