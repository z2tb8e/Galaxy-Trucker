using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GalaxyTrucker.Model.CardEventTypes
{
    public abstract class Encounter<PenaltyT, RewardT> : CardEvent
    {
        public int Firepower { get; }

        public int DayCost { get; }

        public PenaltyT Penalty { get; }

        public RewardT Reward { get; }

        public Encounter(GameStage stage, int firepower, int dayCost, PenaltyT penalty, RewardT reward) : base(stage)
        {
            RequiresOrder = true;
            LastResolved = 0;
            Firepower = firepower;
            DayCost = dayCost;
            Penalty = penalty;
            Reward = reward;
        }

        public override bool IsResolved()
        {
            return LastResolved == 2;
        }

        public abstract override string ToString();

        public abstract override string GetDescription();

        public abstract override IEnumerable<OptionOrSubEvent> GetOptionsOrSubEvents();

        public abstract override void ApplyOption(Ship ship, int option);

        public override string ToolTip()
        {
            return "Amennyiben a saját tűzerő kisebb, mint az előírt, veszítesz." +
                "\nHa nagyobb, lehetőséged van legyőzni jutalomért, de ekkor napokat veszítesz, vagy figyelmen kívül hagyhatod." +
                "\nHa ugyanakkora, akkor figyelmen kívül hagyod.";
        }
    }

    public class Pirates : Encounter<IEnumerable<(Projectile,Direction)>, int>
    {
        public Pirates(GameStage stage, int firepower, int dayCost, IEnumerable<(Projectile, Direction)> projectiles, int reward) : base(stage, firepower, dayCost, projectiles, reward) { }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(((int)Stage).ToString() + "y" + Firepower.ToString("X") + DayCost.ToString() + Reward.ToString("X") + Penalty.Count().ToString());
            foreach((Projectile, Direction) pair in Penalty)
            {
                sb.Append(((int)pair.Item1).ToString() + ((int)pair.Item2).ToString());
            }
            return sb.ToString();
        }

        public override string GetDescription()
        {
            return $"Kalózok | {Firepower * 2} tűzerő";
        }

        public override IEnumerable<OptionOrSubEvent> GetOptionsOrSubEvents()
        {
            return new List<OptionOrSubEvent>()
            {
                new OptionOrSubEvent
                {
                    Description = $"{string.Join(" \n ", Penalty.Select(pair => $"{pair.Item1} {pair.Item2}"))}",
                    Action = (client, ship) =>
                    {
                        LastResolved = 1;
                        client.SendCardOption(0);
                    },
                    Condition = ship => ship.Firepower < (Firepower * 2) && LastResolved == 0,
                },
                new OptionOrSubEvent
                {
                    Description = $"+{Reward} pénz, de -{DayCost} nap",
                    Action = (client, ship) =>
                    {
                        LastResolved = 1;
                        client.SendCardOption(1);
                    },
                    Condition = ship => ship.Firepower > (Firepower * 2) && LastResolved == 0,
                },
                new OptionOrSubEvent
                {
                    Description = "Figyelmen kívül hagyás",
                    Action = (client, ship) =>
                    {
                        LastResolved = 1;
                        client.SendCardOption(2);
                    },
                    Condition = ship => ship.Firepower >= (Firepower * 2) && LastResolved == 0,
                }
            };
        }

        /*options
         * -1: other player took it
         * 0: player beaten
         * 1: pirate beaten
         * 2: ignored by player
         */
        public async override void ApplyOption(Ship ship, int option)
        {
            if (option < -1 || option > 2)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (option == 0)
            {
                Random random = new Random();
                foreach ((Projectile, Direction) pair in Penalty)
                {
                    int roll1 = random.Next(6);
                    int roll2 = random.Next(6);
                    //OnDiceRolled sets the thread waiting
                    await Task.Run(() => OnDiceRolled(pair.Item1, pair.Item2, roll1 + roll2));
                    
                    ship.ApplyProjectile(pair.Item1, pair.Item2, roll1 + roll2);
                }
            }

            //the days get applied through a server message, not here
            else if(option == 1)
            {
                ship.Cash += Reward;
            }
            LastResolved = 2;
        }
    }

    public class Smugglers : Encounter<int, IEnumerable<Ware>>
    {
        public Smugglers(GameStage stage, int firepower, int dayCost, int warePenalty, IEnumerable<Ware> wares) : base(stage, firepower, dayCost, warePenalty, wares) { }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(((int)Stage).ToString() + "d" + Firepower.ToString("X") + DayCost.ToString() + Reward.Count().ToString());
            foreach (Ware w in Reward)
            {
                sb.Append((int)w);
            }
            sb.Append(Penalty);
            return sb.ToString();
        }

        public override string GetDescription()
        {
            return $"Csempészek | {Firepower * 2} tűzerő";
        }

        public override IEnumerable<OptionOrSubEvent> GetOptionsOrSubEvents()
        {
            return new List<OptionOrSubEvent>()
            {
                new OptionOrSubEvent
                {
                    Description = $"Elvett áruk száma: {Penalty}",
                    Action = (client, ship) =>
                    {
                        LastResolved = 1;
                        client.SendCardOption(0);
                    },
                    Condition = ship => ship.Firepower < (Firepower * 2) && LastResolved == 0,
                },
                new OptionOrSubEvent
                {
                    Description = $"{string.Join(" ", Reward)} -{DayCost} nap",
                    Action = (client, ship) =>
                    {
                        LastResolved = 1;
                        client.SendCardOption(1);
                    },
                    Condition = ship => ship.Firepower > (Firepower * 2) && LastResolved == 0,
                },
                new OptionOrSubEvent
                {
                    Description = "Figyelmen kívül hagyás",
                    Action = (client, ship) =>
                    {
                        LastResolved = 1;
                        client.SendCardOption(2);
                    },
                    Condition = ship => ship.Firepower >= (Firepower * 2) && LastResolved == 0,
                }
            };
        }

        /*options
         * -1: other player took it
         * 0: player beaten
         * 1: smuggler beaten
         * 2: ignored by player
         */
        public override void ApplyOption(Ship ship, int option)
        {
            if (option < -1 || option > 2)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (option == 0)
            {
                ship.RemoveWares(Penalty);
            }
            //the days get applied through a server message, not here
            else if (option == 1)
            {
                ship.AddWares(Reward);
            }
            LastResolved = 2;
        }
    }

    public class Slavers : Encounter<int, int>
    {
        public Slavers(GameStage stage, int firepower, int dayCost, int crewPenalty, int reward) : base(stage, firepower, dayCost, crewPenalty, reward) { }

        public override string ToString()
        {
            return ((int)Stage).ToString() + "S" + Firepower.ToString("X") + DayCost.ToString() + Reward.ToString("X") + Penalty.ToString();
        }
        public override string GetDescription()
        {
            return $"Rabszolgakereskedők | {Firepower * 2} tűzerő";
        }

        public override IEnumerable<OptionOrSubEvent> GetOptionsOrSubEvents()
        {
            return new List<OptionOrSubEvent>()
            {
                new OptionOrSubEvent
                {
                    Description = $"Elvett legénység száma: {Penalty}",
                    Action = (client, ship) =>
                    {
                        LastResolved = 1;
                        client.SendCardOption(0);
                    },
                    Condition = ship => ship.Firepower < (Firepower * 2) && LastResolved == 0,
                },
                new OptionOrSubEvent
                {
                    Description = $"+{Reward} pénz, de -{DayCost} nap",
                    Action = (client, ship) =>
                    {
                        LastResolved = 1;
                        client.SendCardOption(1);
                    },
                    Condition = ship => ship.Firepower > (Firepower * 2) && LastResolved == 0,
                },
                new OptionOrSubEvent
                {
                    Description = "Figyelmen kívül hagyás",
                    Action = (client, ship) =>
                    {
                        LastResolved = 1;
                        client.SendCardOption(2);
                    },
                    Condition = ship => ship.Firepower >= (Firepower * 2) && LastResolved == 0,
                }
            };
        }

        /*options
         * -1: other player took it
         * 0: player beaten
         * 1: slaver beaten
         * 2: ignored by player
         */
        public override void ApplyOption(Ship ship, int option)
        {
            if (option < -1 || option > 2)
            {
                throw new ArgumentOutOfRangeException();
            }
            
            if (option == 0)
            {
                ship.RemovePersonnel(Penalty);
            }

            //the days get applied through a server message, not here
            else if (option == 1)
            {
                ship.Cash += Reward;
            }
            LastResolved = 2;
        }
    }
}
