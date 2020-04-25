namespace GalaxyTrucker.Client.Model.PartTypes
{
    public class Shield : Part, IActivatable
    {
        public (Direction, Direction) Directions => (Rotation, (Direction)((int)Rotation + 1));

        public bool Activated { get; private set; }

        public Shield(Connector Top, Connector Right, Connector Bottom, Connector Left) : base(Top, Right, Bottom, Left) => Activated = false;

        public void Activate() => Activated = true;

        public void Deactivate() => Activated = false;

        public override string ToString()
        {
            return base.ToString() + "d";
        }
    }
}
