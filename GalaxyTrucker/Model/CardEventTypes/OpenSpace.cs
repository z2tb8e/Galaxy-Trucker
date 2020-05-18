namespace GalaxyTrucker.Model.CardEventTypes
{
    public class OpenSpace : CardEvent
    {
        public OpenSpace(GameStage stage) : base(stage)
        {
            RequiresAttributes = true;
        }

        public override bool IsResolved()
        {
            return true;
        }

        public override string ToString()
        {
            return $"{(int)Stage}o";
        }

        public override string GetDescription()
        {
            return "Nyílt űr";
        }

        public override string ToolTip()
        {
            return "A motorerődnek megfelelő mennyiségű táv megtétele. Ha a motorerőd 0, akkor kiszállsz a versenyből!";
        }
    }
}
