namespace GalaxyTrucker.Model
{
    interface IActivatable
    {
        bool Activated { get; }

        void Activate();

        void Deactivate();
    }
}
