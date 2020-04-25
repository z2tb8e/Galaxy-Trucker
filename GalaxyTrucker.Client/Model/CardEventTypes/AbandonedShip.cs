﻿namespace GalaxyTrucker.Client.Model.CardEventTypes
{
    public class AbandonedShip : CardEvent
    {
        public int CrewCost { get; }

        public int DayCost { get; }

        public int Reward { get; }

        public AbandonedShip(GameStage stage, int crewCost, int dayCost, int reward) : base(stage) =>
            (CrewCost, DayCost, Reward) = (crewCost, dayCost, reward);

        public override string ToString()
        {
            return base.ToString() + "a" + CrewCost.ToString("X") + DayCost.ToString() + Reward.ToString("X");
        }
    }
}
