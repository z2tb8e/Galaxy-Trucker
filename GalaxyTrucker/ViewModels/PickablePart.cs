using GalaxyTrucker.Model;
using System.Drawing;

namespace GalaxyTrucker.ViewModels
{
    public class PickablePart : NotifyBase
    {
        private Image _partImage;
        private Part _part;
        private bool _isPickable;

        public int Column { get; set; }

        public int Row { get; set; }

        public Part Part
        {
            get
            {
                return _part;
            }
            set
            {
                if(_part != value)
                {
                    _part = value;
                    PartImage = PartBuilder.GetPartImage(_part);
                }
            }
        }

        public Image PartImage
        {
            get
            {
                return _partImage;
            }
            private set
            {
                if(_partImage != value)
                {
                    _partImage = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsPickable
        {
            get
            {
                return _isPickable;
            }
            set
            {
                _isPickable = value;
                OnPropertyChanged();
            }
        }

        public DelegateCommand PartPickCommand { get; set; }
    }
}
