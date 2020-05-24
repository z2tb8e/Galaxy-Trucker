namespace GalaxyTrucker.Model.PartTypes
{
    public class Battery : Part
    {
        private int _charges;

        public int Charges
        {
            get
            {
                return _charges;
            }
            set
            {
                _charges = value;
                ContentsDescription = $"Hátralevő töltések: {_charges}";
                OnContentsChanged();
            }
        }

        public int Capacity { get; private set; }

        public Battery(Connector Top, Connector Right, Connector Bottom, Connector Left, int Capacity) : base(Top, Right, Bottom, Left)
        {
            this.Capacity = Charges = Capacity;
        }

        public bool UseCharge()
        {
            if (Charges > 0)
            {
                --Charges;
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return base.ToString() + "b" + Capacity.ToString();
        }
    }
}
