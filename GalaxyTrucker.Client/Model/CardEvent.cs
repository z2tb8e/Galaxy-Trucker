namespace GalaxyTrucker.Client.Model
{
    public abstract class CardEvent
    {
        public GameStage Stage { get; set; }

        public CardEvent(GameStage stage) => Stage = stage;
    }
}
