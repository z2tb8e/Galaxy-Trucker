using System.Collections.Generic;
using System.Text;

namespace GalaxyTrucker.Client.Model.CardEventTypes
{
    public abstract class Encounter<PenaltyT, RewardT> : CardEvent
    {
        public int Firepower { get; }

        public int DayCost { get; }

        public PenaltyT Penalty { get; }

        public RewardT Reward { get; }

        public Encounter(GameStage stage, int firepower, int dayCost, PenaltyT penalty, RewardT reward) : base(stage) =>
            (Firepower, DayCost, Penalty, Reward) = (firepower, dayCost, penalty, reward);

        public override string ToString()
        {
            return base.ToString();
        }
    }

    public class Pirates : Encounter<List<(Projectile,Direction)>, int>
    {
        public Pirates(GameStage stage, int firepower, int dayCost, List<(Projectile, Direction)> projectiles, int reward) : base(stage, firepower, dayCost, projectiles, reward) { }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(base.ToString() + "y" + Firepower.ToString("X") + DayCost.ToString() + Reward.ToString("X") + Penalty.Count.ToString());
            foreach((Projectile, Direction) pair in Penalty)
            {
                sb.Append(((int)pair.Item1).ToString() + ((int)pair.Item2).ToString());
            }
            return sb.ToString();
        }
    }

    public class Smugglers : Encounter<int, List<Ware>>
    {
        public Smugglers(GameStage stage, int firepower, int dayCost, int warePenalty, List<Ware> wares) : base(stage, firepower, dayCost, warePenalty, wares) { }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(base.ToString() + "d" + Firepower.ToString("X") + DayCost.ToString() + Reward.Count.ToString());
            foreach(Ware w in Reward)
            {
                sb.Append((int)w);
            }
            sb.Append(Penalty);
            return sb.ToString();
        }
    }

    public class Slavers : Encounter<int, int>
    {
        public Slavers(GameStage stage, int firepower, int dayCost, int crewPenalty, int reward) : base(stage, firepower, dayCost, crewPenalty, reward) { }

        public override string ToString()
        {
            return base.ToString() + "S" + Firepower.ToString("X") + DayCost.ToString() + Reward.ToString("X") + Penalty.ToString();
        }
    }
}
