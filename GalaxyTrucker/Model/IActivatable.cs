namespace GalaxyTrucker.Model
{
    public interface IActivatable
    {
        public bool Activated { get; }

        public void Activate();

        public void Deactivate();
    }
}
