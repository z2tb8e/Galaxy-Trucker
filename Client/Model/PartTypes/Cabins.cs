namespace Client.Model.PartTypes
{
    public class Cabin : Part
    {
        private Personnel _personnel;

        public bool Filled { get; private set; }

        public Cabin(Connector Top, Connector Right, Connector Bottom, Connector Left) : base(Top, Right, Bottom, Left)
            => Filled = false;

        public void AddPersonnel(Personnel p)
        {
            _personnel = p;
            Filled = true;
        }

        public bool RemoveSinglePersonnel()
        {
            if (_personnel == Personnel.HumanDouble)
            {
                _personnel = Personnel.HumanSingle;
                return true;
            }
            else if (_personnel == Personnel.EngineAlien || _personnel == Personnel.LaserAlien || _personnel == Personnel.HumanSingle)
            {
                _personnel = Personnel.None;
                return true;
            }
            else return false;
        }
    }

    public class Cockpit : Cabin
    {
        public Cockpit() : base(Connector.Universal, Connector.Universal, Connector.Universal, Connector.Universal)
            => AppendPath(this);

        public new void Rotate(Direction _) { }
    }
}
