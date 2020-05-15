namespace GalaxyTrucker.Model.CardEventTypes
{
    public class OpenSpace : CardEvent
    {
        public OpenSpace(GameStage stage) : base(stage) { }

        public override string ToString()
        {
            return $"{(int)Stage}o";
        }
    }
}
