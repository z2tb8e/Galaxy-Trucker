using GalaxyTrucker.Model;
using GalaxyTrucker.Views.Utils;
using System;
using System.Drawing;

namespace GalaxyTrucker.ViewModels
{
    public class FlightPartViewModel : NotifyBase
    {
        private Part _part;
        private Image _partImage;
        private bool _highlighted;
        private string _partContentsDescription;

        public Image PartImage
        {
            get
            {
                return _partImage;
            }
            private set
            {
                _partImage = value;
                OnPropertyChanged();
            }
        }

        public int Angle => (int)_part.Rotation * 90;

        public bool Highlighted
        {
            get
            {
                return _highlighted;
            }
            private set
            {
                _highlighted = value;
                OnPropertyChanged();
            }
        }

        public string PartContentsDescription
        {
            get
            {
                return _partContentsDescription;
            }
            private set
            {
                _partContentsDescription = value;
                OnPropertyChanged();
            }
        }

        public bool HasContent { get; private set; }

        public DelegateCommand ClickCommand { get; set; }

        public FlightPartViewModel(Part part)
        {
            if(_part == null)
            {
                HasContent = false;
                return;
            }

            HasContent = true;
            _part = part;
            PartImage = PartBuilder.GetPartImage(_part);
            Highlighted = false;

            _part.HighlightToggled += Part_HighlightToggled;
            _part.ContentsChanged += Part_ContentsChanged;
        }

        public void Remove()
        {
            _part = null;
            _partImage = null;
            ClickCommand = null;
            HasContent = false;
        }

        private void Part_ContentsChanged(object sender, EventArgs e)
        {
            PartContentsDescription = _part.ContentsDescription;
        }

        private void Part_HighlightToggled(object sender, EventArgs e)
        {
            Highlighted = true;
        }
    }
}
