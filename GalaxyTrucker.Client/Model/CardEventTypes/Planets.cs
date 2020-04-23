using System.Collections.Generic;

namespace GalaxyTrucker.Client.Model.CardEventTypes
{
    public class Planets : CardEvent
    {
        public int DayCost { get; }

        public List<List<Ware>> Offers { get; }

        public Planets(GameStage stage, int dayCost, List<List<Ware>> offers) : base(stage) =>
            (DayCost, Offers) = (dayCost, offers);
    }
}
