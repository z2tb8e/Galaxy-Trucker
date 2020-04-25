﻿using System.Collections.Generic;
using System.Text;

namespace GalaxyTrucker.Client.Model.CardEventTypes
{
    public class AbandonedStation : CardEvent
    {
        public int MinimumCrew { get; }

        public int DayCost { get; }

        public List<Ware> Wares { get; }

        public AbandonedStation(GameStage stage, int minimumCrew, int dayCost, List<Ware> wares) : base(stage) =>
            (MinimumCrew, DayCost, Wares) = (minimumCrew, dayCost, wares);

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(base.ToString() + "A" + MinimumCrew.ToString("X") + DayCost.ToString() + Wares.Count);
            foreach(Ware w in Wares)
            {
                sb.Append((int)w);
            }
            return sb.ToString();
        }
    }
}
