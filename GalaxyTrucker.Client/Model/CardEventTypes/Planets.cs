using System.Collections.Generic;

namespace GalaxyTrucker.Client.Model.CardEventTypes
{
    public class Planets : CardEvent
    {
        public int DayCost { get; }

        public List<Ware> Offers { get; }

        public Planets(int dayCost, List<Ware> offers) =>
            (DayCost, Offers) = (dayCost, offers);
    }
}
