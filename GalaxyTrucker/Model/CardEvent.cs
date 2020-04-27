namespace GalaxyTrucker.Model
{
    public abstract class CardEvent
    {
        public GameStage Stage { get; set; }

        public CardEvent(GameStage stage) => Stage = stage;

        public override string ToString()
        {
            return ((int)Stage).ToString();
        }
    }
}
