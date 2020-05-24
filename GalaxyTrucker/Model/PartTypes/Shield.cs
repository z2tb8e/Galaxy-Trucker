namespace GalaxyTrucker.Model.PartTypes
{
    public class Shield : Part, IActivatable
    {
        private bool _activated;

        public (Direction, Direction) Directions => (Rotation, (Direction)(((int)Rotation + 1) % 4));

        public Shield(Connector Top, Connector Right, Connector Bottom, Connector Left) : base(Top, Right, Bottom, Left)
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
            return base.ToString() + "d";
        }
    }
}
