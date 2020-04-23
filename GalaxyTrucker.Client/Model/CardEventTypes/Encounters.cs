using System.Collections.Generic;

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
    }

    public class Pirates : Encounter<List<(Projectile,Direction)>, int>
    {
        public Pirates(GameStage stage, int firepower, int dayCost, List<(Projectile, Direction)> projectiles, int reward) : base(stage, firepower, dayCost, projectiles, reward) { }
    }

    public class Smugglers : Encounter<int, List<Ware>>
    {
        public Smugglers(GameStage stage, int firepower, int dayCost, int warePenalty, List<Ware> wares) : base(stage, firepower, dayCost, warePenalty, wares) { }
    }

    public class Slavers : Encounter<int, int>
    {
        public Slavers(GameStage stage, int firepower, int dayCost, int crewPenalty, int reward) : base(stage, firepower, dayCost, crewPenalty, reward) { }
    }
}
