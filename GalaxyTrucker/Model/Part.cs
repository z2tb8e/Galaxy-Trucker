﻿using System.Collections.Generic;

namespace GalaxyTrucker.Model
{
    public abstract class Part
    {
        private readonly Connector[] _connectors;

        private Stack<Part> _path;

        public int Pos1 { get; set; }

        public int Pos2 { get; set; }

        public Direction Rotation { get; private set; }

        public Connector[] Connectors => _connectors;

        public Stack<Part> Path { get { return new Stack<Part>(_path); } set { _path = value; } }

        protected Part(Connector Top, Connector Right, Connector Bottom, Connector Left)
        {
            _path = new Stack<Part>();
            _connectors = new Connector[4]
            {
                Top, Right, Bottom, Left
            };
        }

        public Connector GetConnector(Direction dir) => _connectors[((int)dir + (int)Rotation) % 4];

        public void Rotate(Direction dir) => this.Rotation = (Direction)((int)Rotation + (int)dir % 4);

        public void AddToPath(Part p) => _path.Push(p);

        public bool IsInPath(Part p) => _path.Contains(p);

        public override string ToString()
        {
            return ((int)_connectors[0]).ToString() + ((int)_connectors[1]).ToString() + ((int)_connectors[2]).ToString() + ((int)_connectors[3]).ToString();
        }
    }
}