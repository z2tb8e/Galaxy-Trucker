﻿namespace GalaxyTrucker.Model.PartTypes
{
    public class Laser : Part
    {
        public virtual int Firepower => Rotation == Direction.Top ? 2 : 1;

        public Laser(Connector Top, Connector Right, Connector Bottom, Connector Left) : base(Top, Right, Bottom, Left)
        {
            if (Top != Connector.None)
                throw new System.ArgumentException("Parameter can only be Connector.None for lasers", "Top");
        }

        public override string ToString()
        {
            return base.ToString() + "l";
        }
    }

    public class LaserDouble : Laser, IActivatable
    {
        private bool _activated;

        public override int Firepower => Activated ? base.Firepower * 2 : 0;

        public LaserDouble(Connector Top, Connector Right, Connector Bottom, Connector Left) : base(Top, Right, Bottom, Left)
        {
            Activated = false;
        }

        public bool Activated
        {
            get
            {
                return _activated;
            }
            set
            {
                _activated = value;
                ContentsDescription = _activated ? "Aktív" : "Inaktív";
                OnContentsChanged();
            }
        }

        public void Activate() => Activated = true;

        public void Deactivate() => Activated = false;

        public override string ToString()
        {
            return (this as Part).ToString() + "L";
        }
    }
}
