namespace GalaxyTrucker.Client.Model.CardEventTypes
{
    public class OpenSpace : CardEvent
    {
        public OpenSpace(GameStage stage) : base(stage) { }

        public override string ToString()
        {
            return base.ToString() + "o";
        }
    }
}
