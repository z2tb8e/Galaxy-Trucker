namespace GalaxyTrucker.Model.CardEventTypes
{
    public class Stardust : CardEvent
    {
        public Stardust(GameStage stage) : base(stage) { }

        public override string ToString()
        {
            return base.ToString() + "s";
        }
    }
}
