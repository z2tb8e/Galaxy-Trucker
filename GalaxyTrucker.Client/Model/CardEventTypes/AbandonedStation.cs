using System.Collections.Generic;

namespace GalaxyTrucker.Client.Model.CardEventTypes
{
    public class AbandonedStation : CardEvent
    {
        public int MinimumCrew { get; }

        public int DayCost { get; }

        public List<Ware> Wares { get; }

        public AbandonedStation(GameStage stage, int minimumCrew, int dayCost, List<Ware> wares) : base(stage) =>
            (MinimumCrew, DayCost, Wares) = (minimumCrew, dayCost, wares);
    }
}
