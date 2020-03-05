namespace GalaxyTrucker.Client.Model.PartTypes
{
    public class Pipe : Part
    {
        public Pipe(Connector Top, Connector Right, Connector Bottom, Connector Left) : base(Top, Right, Bottom, Left) { }
    }

    public class LaserCabin : Part
    {
        public bool EffectUsed { get; set; }

        public LaserCabin(Connector Top, Connector Right, Connector Bottom, Connector Left) : base(Top, Right, Bottom, Left) => EffectUsed = false;
    }

    public class EngineCabin : Part
    {
        public bool EffectUsed { get; set; }

        public EngineCabin(Connector Top, Connector Right, Connector Bottom, Connector Left) : base(Top, Right, Bottom, Left) => EffectUsed = false;
    }
}
