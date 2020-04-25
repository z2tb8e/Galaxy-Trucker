using System.Linq;

namespace GalaxyTrucker.Client.Model.PartTypes
{
    public class Storage : Part
    {
        private readonly Ware[] _storage;

        public int Capacity { get; private set; }

        public Ware Max => _storage.Max();

        public Ware Min => _storage.Min();

        public int Value => _storage.Cast<int>().Sum();

        public Storage(Connector Top, Connector Right, Connector Bottom, Connector Left, int Capacity) : base(Top, Right, Bottom, Left) =>
            _storage = Enumerable.Repeat(Ware.Empty, this.Capacity = Capacity).ToArray();

        protected virtual bool CanAdd(Ware w) => w != Ware.Red && w != Ware.Empty;

        public void AddWare(Ware ware)
        {
            if (ware <= Min || !CanAdd(ware))
                return;
            for(int i = 0; i < Capacity; ++i)
                if(_storage[i] == Min)
                {
                    _storage[i] = ware;
                    return;
                }
        }

        public void RemoveMax()
        {
            for(int i = 0; i < Capacity; ++i)
                if (_storage[i] == Max)
                {
                    _storage[i] = Ware.Empty;
                    return;
                }
        }

        public Ware GetMaxWare() => _storage.Max();

        public override string ToString()
        {
            return base.ToString() + "s" + Capacity.ToString();
        }
    }

    public class SpecialStorage : Storage
    {
        public SpecialStorage(Connector Top, Connector Right, Connector Bottom, Connector Left, int Capacity) : base(Top, Right, Bottom, Left, Capacity) { }

        protected override bool CanAdd(Ware w) => w != Ware.Empty;

        public override string ToString()
        {
            return (this as Part).ToString() + "S" + Capacity.ToString();
        }
    }
}
