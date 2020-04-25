namespace GalaxyTrucker.Client.Model.CardEventTypes
{
    public class Pandemic : CardEvent
    {
        public Pandemic(GameStage stage) : base(stage) { }

        public override string ToString()
        {
            return base.ToString() + "p";
        }
    }
}
