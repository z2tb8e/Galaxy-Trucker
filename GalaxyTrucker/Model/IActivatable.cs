namespace GalaxyTrucker.Model
{
    public interface IActivatable
    {
        bool Activated { get; }

        void Activate();

        void Deactivate();
    }
}
