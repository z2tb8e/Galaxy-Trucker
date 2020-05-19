using System;
using System.Collections.Generic;

namespace GalaxyTrucker.Model.CardEventTypes
{
    public class AbandonedShip : CardEvent
    {
        public int CrewCost { get; }

        public int DayCost { get; }

        public int Reward { get; }

        public AbandonedShip(GameStage stage, int crewCost, int dayCost, int reward) : base(stage)
        {
            RequiresOrder = true;
            LastResolved = 0;
            CrewCost = crewCost;
            DayCost = dayCost;
            Reward = reward;
        }

        public override bool IsResolved()
        {
            return LastResolved == 2;
        }

        public override string ToString()
        {
            return ((int)Stage).ToString() + "a" + CrewCost.ToString("X") + DayCost.ToString() + Reward.ToString("X");
        }

        public override string GetDescription()
        {
            return $"Elhagyatott űrhajó";
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
                        client.SendCardOption(0);
                        LastResolved = 1;
                    },
                    Condition = ship => LastResolved == 0,
                },
                new OptionOrSubEvent
                {
                    Description = $"Elfogadás, +{Reward} pénz, de -{DayCost} nap és -{CrewCost} legénységi tag",
                    Action = (client, ship) =>
                    {
                        client.SendCardOption(1);
                        LastResolved = 1;
                    },
                    Condition = ship => ship.CrewCount >= CrewCost && LastResolved == 0,
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
            if(LastResolved != 1)
            {
                throw new InvalidOperationException();
            }
            if(option < -1 || option > 1)
            {
                throw new ArgumentOutOfRangeException();
            }
            //offer accepted
            if(option == 1)
            {
                ship.RemovePersonnel(CrewCost);
                ship.Cash += Reward;
            }
            LastResolved = 2;
        }
    }
}
