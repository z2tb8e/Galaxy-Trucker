namespace Client.Model.PartTypes
{
    public class Laser : Part
    {
        public virtual int Firepower => Rotation == Direction.Top ? 2 : 1;

        public Laser(Connector Top, Connector Right, Connector Bottom, Connector Left) : base(Top, Right, Bottom, Left)
        {
            if (Top != Connector.None)
                throw new System.ArgumentException("Parameter can only be Connector.None for lasers", "Top");
        }
    }

    public class LaserDouble : Laser, IActivatable
    {
        public override int Firepower => Activated ? base.Firepower * 2 : 0;

        public LaserDouble(Connector Top, Connector Right, Connector Bottom, Connector Left) : base(Top, Right, Bottom, Left)
            => Activated = false;

        public bool Activated { get; private set; }

        public void Activate() => Activated = true;

        public void Deactivate() => Activated = false;
    }
}
