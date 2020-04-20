using System.Collections.Generic;

namespace GalaxyTrucker.Client.Model.CardEventTypes
{
    public class AbandonedStation : CardEvent
    {
        public int MinimumCrew { get; }

        public int DayCost { get; }

        public List<Ware> Wares { get; }

        public AbandonedStation(int minimumCrew, int dayCost, List<Ware> wares) =>
            (MinimumCrew, DayCost, Wares) = (minimumCrew, dayCost, wares);
    }
}
