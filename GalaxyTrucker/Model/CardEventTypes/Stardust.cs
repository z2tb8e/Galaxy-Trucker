﻿using System.Collections.Generic;

namespace GalaxyTrucker.Model.CardEventTypes
{
    public class Stardust : CardEvent
    {
        public Stardust(GameStage stage) : base(stage)
        {
            RequiresAttributes = false;
            LastResolved = 0;
        }

        public override bool IsResolved()
        {
            return LastResolved == 1;
        }

        public override string ToString()
        {
            return $"{(int)Stage}s";
        }

        public override string GetDescription()
        {
            return "Csillagpor.";
        }

        public override string ToolTip()
        {
            return "Minden nyitott csatlakozó után veszítesz egy napot.";
        }

        public override IEnumerable<OptionOrSubEvent> GetOptionsOrSubEvents()
        {
            return new List<OptionOrSubEvent>
            {
                new OptionOrSubEvent
                {
                    Description = "Lejátszás",
                    Action = (client, ship) =>
                    {
                        client.SendStardustInfo(ship.GetOpenConnectorCount());
                        LastResolved = 1;
                    },
                    Condition = ship => LastResolved == 0
                }
            };
        }
    }
}
