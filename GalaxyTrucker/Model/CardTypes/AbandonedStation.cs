using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GalaxyTrucker.Model.CardTypes
{
    public class AbandonedStation : Card
    {
        public int MinimumCrew { get; }

        public int DayCost { get; }

        public IEnumerable<Ware> Wares { get; }

        public AbandonedStation(GameStage stage, int minimumCrew, int dayCost, IEnumerable<Ware> wares) : base(stage)
        {
            RequiresOrder = true;
            LastResolved = 0;
            MinimumCrew = minimumCrew;
            DayCost = dayCost;
            Wares = wares;
        }

        public override bool IsResolved()
        {
            return LastResolved == 2;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(((int)Stage).ToString() + "A" + MinimumCrew.ToString("X") + DayCost.ToString() + Wares.Count());
            foreach(Ware w in Wares)
            {
                sb.Append((int)w);
            }
            return sb.ToString();
        }

        public override string GetDescription()
        {
            return $"Elhagyatott űrállomás | szükséges {MinimumCrew} legénységi tag";
        }

        public override IEnumerable<OptionOrSubEvent> GetOptionsOrSubEvents()
        {
            return new List<OptionOrSubEvent>()
            {
                new OptionOrSubEvent
                {
                    Description = "Kihagyás",
                    Action = (client, ship) =>
                    {
                        LastResolved = 1;
                        client.SendCardOption(0);
                    },
                    Condition = ship => LastResolved == 0,
                },
                new OptionOrSubEvent
                {
                    Description = $"{string.Join(" ", Wares)} -{DayCost} nap",
                    Action = (client, ship) =>
                    {
                        LastResolved = 1;
                        client.SendCardOption(1);
                    },
                    Condition = ship => ship.CrewCount >= MinimumCrew && LastResolved == 0,
                }
            };
        }

        /*options
         * -1: other player took it
         * 0: ignored
         * 1: accepted
         */
        public override void ApplyOption(Ship ship, int option)
        {
            if (option < -1 || option > 1)
            {
                throw new ArgumentOutOfRangeException();
            }
            //offer accepted
            if (option == 1)
            {
                ship.AddWares(Wares);
            }
            LastResolved = 2;
        }
    }
}
