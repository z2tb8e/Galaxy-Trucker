using System.Collections.Generic;

namespace GalaxyTrucker.Client.Model.CardEventTypes
{
    public abstract class Encounter<PenaltyT, RewardT> : CardEvent
    {
        public int Firepower { get; }

        public int DayCost { get; }

        public PenaltyT Penalty { get; }

        public RewardT Reward { get; }

        public Encounter(int firepower, int dayCost, PenaltyT penalty, RewardT reward) =>
            (Firepower, DayCost, Penalty, Reward) = (firepower, dayCost, penalty, reward);
    }

    public class Pirates : Encounter<Barrage, int>
    {
        public Pirates(int firepower, int dayCost, Barrage barrage, int reward) : base(firepower, dayCost, barrage, reward) { }
    }

    public class Smugglers : Encounter<int, List<Ware>>
    {
        public Smugglers(int firepower, int dayCost, int warePenalty, List<Ware> wares) : base(firepower, dayCost, warePenalty, wares) { }
    }

    public class Slavers : Encounter<int, int>
    {
        public Slavers(int firepower, int dayCost, int crewPenalty, int reward) : base(firepower, dayCost, crewPenalty, reward) { }
    }
}
