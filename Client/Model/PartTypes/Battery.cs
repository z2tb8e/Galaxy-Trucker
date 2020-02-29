namespace Client.Model.PartTypes
{
    public class Battery : Part
    {
        public int Charges { get; private set; }

        public int Capacity { get; private set; }

        public Battery(Connector Top, Connector Right, Connector Bottom, Connector Left, int Capacity) : base(Top, Right, Bottom, Left)
            => this.Capacity = Charges = Capacity;

        public bool UseCharge()
        {
            if(Charges > 0)
            {
                --Charges;
                return true;
            }
            return false;
        }
    }
}
