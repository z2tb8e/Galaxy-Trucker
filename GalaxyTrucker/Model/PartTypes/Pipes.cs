namespace GalaxyTrucker.Model.PartTypes
{
    public class Pipe : Part
    {
        public Pipe(Connector Top, Connector Right, Connector Bottom, Connector Left) : base(Top, Right, Bottom, Left) { }

        public override string ToString()
        {
            return base.ToString() + "p";
        }
    }

    public class LaserCabin : Part
    {
        public LaserCabin(Connector Top, Connector Right, Connector Bottom, Connector Left) : base(Top, Right, Bottom, Left) { }

        public override string ToString()
        {
            return base.ToString() + "A";
        }
    }

    public class EngineCabin : Part
    {
        public EngineCabin(Connector Top, Connector Right, Connector Bottom, Connector Left) : base(Top, Right, Bottom, Left) { }

        public override string ToString()
        {
            return base.ToString() + "a";
        }
    }
}
