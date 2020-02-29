using System;
using System.Collections.Generic;
using System.Linq;

namespace Client.Model.PartTypes
{
    public class Storage : Part
    {
        private Ware[] _storage;

        public int Capacity { get; private set; }

        public Storage(Connector Top, Connector Right, Connector Bottom, Connector Left, int Capacity) : base(Top, Right, Bottom, Left) => _storage = new Ware[this.Capacity = Capacity];

        public List<Ware> GetWares() => _storage.Where(w => w != Ware.Empty).ToList();

        public virtual bool CanAdd(Ware w) => w != Ware.Red && w != Ware.Empty;

        public bool AddWare(Ware ware)
        {
            if (!CanAdd(ware))
                return false;

            var index = Array.FindIndex(_storage, w => w != Ware.Empty);
            if (index != -1)
            {
                _storage[index] = ware;
                return true;
            }
            return false;
        }

        public bool RemoveWare(Ware ware)
        {
            int index = Array.FindIndex(_storage, w => w == ware);
            if (index != -1)
            {
                _storage[index] = Ware.Empty;
                return true;
            }
            return false;
        }

        public Ware GetMaxWare() => _storage.Max();
    }

    public class SpecialStorage : Storage
    {
        public SpecialStorage(Connector Top, Connector Right, Connector Bottom, Connector Left, int Capacity) : base(Top, Right, Bottom, Left, Capacity) { }

        public override bool CanAdd(Ware w) => w != Ware.Empty;
    }
}
