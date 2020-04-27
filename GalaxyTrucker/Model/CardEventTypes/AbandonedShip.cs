namespace GalaxyTrucker.Model.CardEventTypes
{
    public class AbandonedShip : CardEvent
    {
        public int CrewCost { get; }

        public int DayCost { get; }

        public int Reward { get; }

        public AbandonedShip(GameStage stage, int crewCost, int dayCost, int reward) : base(stage)
        {
            CrewCost = crewCost;
            DayCost = dayCost;
            Reward = reward;
        }

        public override string ToString()
        {
            return base.ToString() + "a" + CrewCost.ToString("X") + DayCost.ToString() + Reward.ToString("X");
        }
    }
}
