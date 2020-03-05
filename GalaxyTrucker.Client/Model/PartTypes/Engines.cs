namespace GalaxyTrucker.Client.Model.PartTypes
{
    public class Engine : Part
    {
        public virtual int Enginepower => 1;

        public Engine(Connector Top, Connector Right, Connector Bottom, Connector Left) : base(Top, Right, Bottom, Left)
        {
            if (Bottom != Connector.None)
                throw new System.ArgumentException("Parameter can only be Connector.None for engines", "Bottom");
        }

        public new void Rotate(Direction _) { }
    };

    public class EngineDouble : Engine, IActivatable
    {
        public override int Enginepower => Activated ? 2 : 0;

        public EngineDouble(Connector Top, Connector Right, Connector Bottom, Connector Left) : base(Top, Right, Bottom, Left)
            => Activated = false;

        public bool Activated { get; private set; }

        public void Activate() => Activated = true;

        public void Deactivate() => Activated = false;
    }
}
