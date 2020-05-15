﻿namespace GalaxyTrucker.Model.CardEventTypes
{
    public class Pandemic : CardEvent
    {
        public Pandemic(GameStage stage) : base(stage) { }

        public override string ToString()
        {
            return $"{(int)Stage}p";
        }
    }
}
