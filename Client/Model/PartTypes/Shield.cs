﻿namespace Client.Model.PartTypes
{
    public class Shield : Part, IActivatable
    {
        public (Direction, Direction) Directions => (Rotation, (Direction)((int)Rotation + 1));

        public Shield(Connector Top, Connector Right, Connector Bottom, Connector Left) : base(Top, Right, Bottom, Left)
            => Activated = false;

        public bool Activated { get; private set; }

        public void Activate() => Activated = true;

        public void Deactivate() => Activated = false;
    }
}
