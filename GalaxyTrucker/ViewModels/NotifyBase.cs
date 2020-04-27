using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GalaxyTrucker.ViewModels
{
    public abstract class NotifyBase : INotifyPropertyChanged
    {
        protected NotifyBase() { }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
