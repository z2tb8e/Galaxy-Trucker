﻿using System.Collections.Generic;

namespace GalaxyTrucker.Model.PartTypes
{
    public class Cabin : Part
    {
        private Personnel _personnel;

        public Personnel Personnel
        {
            get
            {
                return _personnel;
            }
            set
            {
                _personnel = value;
                ContentsDescription = $"Legénység: {Personnel.GetDescription()}";
                OnContentsChanged();
            }
        }

        public Cabin(Connector Top, Connector Right, Connector Bottom, Connector Left) : base(Top, Right, Bottom, Left)
        {
            Personnel = Personnel.None;
        }

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
            Neighbours = new List<Part>();
            Player = pc;
        }

        public override string ToString()
        {
            return "C" + ((int)Player).ToString();
        }
    }
}
