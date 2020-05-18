using System.Collections.Generic;

namespace GalaxyTrucker.Model.CardEventTypes
{
    public class Pandemic : CardEvent
    {
        public Pandemic(GameStage stage) : base(stage)
        {
            RequiresAttributes = false;
        }

        public override bool IsResolved()
        {
            return LastResolved == 2;
        }

        public override string ToString()
        {
            return $"{(int)Stage}p";
        }

        public override string GetDescription()
        {
            return "Járvány";
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
                        LastResolved = 1;
                        ship.ApplyPandemic();
                        LastResolved = 2;
                    },
                    Condition = ship => LastResolved == 2
                }
            };
        }

        public override string ToolTip()
        {
            return "Minden egymáshoz kapcsolódó, szomszédos, lakott kabinból 1-1 legénységi tagot vesztesz!";
        }
    }
}
