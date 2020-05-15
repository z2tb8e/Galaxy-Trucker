using System.Collections.Generic;
using System.Text;

namespace GalaxyTrucker.Model.CardEventTypes
{
    public class Planets : CardEvent
    {
        public int DayCost { get; }

        public List<List<Ware>> Offers { get; }

        public Planets(GameStage stage, int dayCost, List<List<Ware>> offers) : base(stage)
        {
            DayCost = dayCost;
            Offers = offers;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"{(int)Stage}P{DayCost}{Offers.Count}");
            foreach(List<Ware> offer in Offers)
            {
                sb.Append(offer.Count);
                foreach(Ware w in offer)
                {
                    sb.Append((int)w);
                }
            }
            return sb.ToString();
        }
    }
}
