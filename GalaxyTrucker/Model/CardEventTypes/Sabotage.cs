namespace GalaxyTrucker.Model.CardEventTypes
{
    public class Sabotage : CardEvent
    {
        public Sabotage(GameStage stage) : base(stage) { }

        public override string ToString()
        {
            return base.ToString() + "g";
        }
    }
}
