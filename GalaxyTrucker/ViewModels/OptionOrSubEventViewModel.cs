namespace GalaxyTrucker.ViewModels
{
    public class OptionOrSubEventViewModel : NotifyBase
    {
        public string Description { get; set; }

        public DelegateCommand ClickCommand { get; set; }
    }
}
