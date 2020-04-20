namespace GalaxyTrucker.Client.Model.CardEventTypes
{
    public class AbandonedShip : CardEvent
    {
        public int CrewCost { get; }

        public int DayCost { get; }

        public int Reward { get; }

        public AbandonedShip(int crewCost, int dayCost, int reward) =>
            (CrewCost, DayCost, Reward) = (crewCost, dayCost, reward);
    }
}
