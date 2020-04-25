namespace GalaxyTrucker.Client.Model.PartTypes
{
    public class Cabin : Part
    {
        public Personnel Personnel { get; set; }

        public Cabin(Connector Top, Connector Right, Connector Bottom, Connector Left) : base(Top, Right, Bottom, Left)
            => Personnel = Personnel.None;

        public bool RemoveSinglePersonnel()
        {
            bool ret = (Personnel == Personnel.None);
            Personnel = Personnel switch
            {
                Personnel.HumanDouble => Personnel.HumanSingle,
                _ => Personnel.None
            };
            return ret;
        }

        public override string ToString()
        {
            return base.ToString() + "c";
        }
    }

    public class Cockpit : Cabin
    {
        public PlayerColor Player { get; private set; }

        public Cockpit(PlayerColor pc) : base(Connector.Universal, Connector.Universal, Connector.Universal, Connector.Universal)
        {
            AddToPath(this);
            Player = pc;
        }

        public new void Rotate(Direction _) { }

        public override string ToString()
        {
            return "C" + ((int)Player).ToString();
        }
    }
}
