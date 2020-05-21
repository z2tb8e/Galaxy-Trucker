using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GalaxyTrucker.Model.CardEventTypes
{
    public class WarzoneEvent<T>
    {
        public CardCheckAttribute Attribute { get; }

        public CardEventPenalty PenaltyType { get; }

        public T Penalty { get; }

        public WarzoneEvent(CardCheckAttribute attribute, CardEventPenalty penaltyType, T penalty)
        {
            Attribute = attribute;
            PenaltyType = penaltyType;
            Penalty = penalty;
        }

        public override string ToString()
        {
            return $"{(int)Attribute}{(int)PenaltyType}";
        }
    }

    public class Warzone : CardEvent
    {
        public WarzoneEvent<int> Event1 { get; }

        public WarzoneEvent<int> Event2 { get; }

        public WarzoneEvent<List<(Projectile, Direction)>> Event3 { get; }

        public Warzone(GameStage stage, WarzoneEvent<int> event1, WarzoneEvent<int> event2, WarzoneEvent<List<(Projectile, Direction)>> event3) : base(stage)
        {
            LastResolved = 0;
            Event1 = event1;
            Event2 = event2;
            Event3 = event3;
        }

        public override bool IsResolved()
        {
            return LastResolved == 3;
        }

        public override string GetDescription()
        {
            return "Harci övezet";
        }

        public override string ToolTip()
        {
            return "A három esemény mindegyikénél az adott szempont szerint leggyengébb (holtverseny esetén az előrébb álló) játékos a leírt büntetést kapja.";
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"{(int)Stage}w");
            sb.Append(Event1.ToString() + Event1.Penalty.ToString());
            sb.Append(Event2.ToString() + Event2.Penalty.ToString());
            sb.Append(Event3.ToString() + Event3.Penalty.Count().ToString());
            foreach((Projectile, Direction) pair in Event3.Penalty)
            {
                sb.Append(((int)pair.Item1).ToString() + ((int)pair.Item2).ToString());
            }
            return sb.ToString();
        }

        public override IEnumerable<OptionOrSubEvent> GetOptionsOrSubEvents()
        {
            return new List<OptionOrSubEvent>
            {
                new OptionOrSubEvent
                {
                    Description = $"Legkisebb {Event1.Attribute.GetDescription()}, -{Event1.Penalty} {Event1.PenaltyType.GetDescription()}",
                    Action = (client, ship) =>
                    {
                        client.UpdateAttributes(ship.Firepower, ship.Enginepower, ship.CrewCount, ship.StorageCount, ship.Batteries);
                        LastResolved = -1;
                    },
                    Condition = ship => LastResolved == 0
                },
                new OptionOrSubEvent
                {
                    Description = $"Legkisebb {Event2.Attribute.GetDescription()}, -{Event2.Penalty} {Event2.PenaltyType.GetDescription()}",
                    Action = (client, ship) =>
                    {
                        client.UpdateAttributes(ship.Firepower, ship.Enginepower, ship.CrewCount, ship.StorageCount, ship.Batteries);
                        LastResolved = -2;
                    },
                    Condition = ship => LastResolved == 1
                },
                new OptionOrSubEvent
                {
                    Description = $"Legkisebb {Event3.Attribute.GetDescription()}, {Event3.PenaltyType.GetDescription()}:" +
                    $"\n {string.Join(" \n ", Event3.Penalty.Select(pair => $"{pair.Item1} {pair.Item2}"))}",
                    Action = (client, ship) =>
                    {
                        client.UpdateAttributes(ship.Firepower, ship.Enginepower, ship.CrewCount, ship.StorageCount, ship.Batteries);
                        LastResolved = -3;
                    },
                    Condition = ship => LastResolved == 2
                }
            };
        }

        /*option
         * 0: this player is the target
         * 1: other player is the target
         */
        public async override void ApplyOption(Ship ship, int option)
        {
            if(option < 0 || option > 1)
            {
                throw new ArgumentOutOfRangeException();
            }
            if(option == 1)
            {
                LastResolved = -1 * LastResolved;
                return;
            }

            //Barrage only occurs in 3rd event, but there all the time
            //Delay is handled by the server
            switch (LastResolved)
            {
                case -1:
                    if(Event1.PenaltyType == CardEventPenalty.Crew)
                    {
                        ship.RemovePersonnel(Event1.Penalty);
                    }
                    else if(Event1.PenaltyType == CardEventPenalty.Wares)
                    {
                        ship.RemoveWares(Event1.Penalty);
                    }
                    break;
                case -2:
                    if (Event2.PenaltyType == CardEventPenalty.Crew)
                    {
                        ship.RemovePersonnel(Event2.Penalty);
                    }
                    else if (Event2.PenaltyType == CardEventPenalty.Wares)
                    {
                        ship.RemoveWares(Event2.Penalty);
                    }
                    break;
                    //penalty always is barrage
                case -3:
                    Random random = new Random();
                    foreach((Projectile, Direction) projectile in Event3.Penalty)
                    {
                        int roll1 = random.Next(6);
                        int roll2 = random.Next(6);

                        //OnDiceRolled sets the thread waiting
                        await Task.Run(() => OnDiceRolled(projectile.Item1, projectile.Item2, roll1 + roll2));

                        ship.ApplyProjectile(projectile.Item1, projectile.Item2, roll1 + roll2);
                    }
                    break;
            }
            LastResolved = -1 * LastResolved;
        }
    }
}
