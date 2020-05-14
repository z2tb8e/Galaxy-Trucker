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

        public int Angle => _part == null ? 0 : (int)_part.Rotation * 90;

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

        public int Row { get; }

        public int Column { get; }

        public DelegateCommand PartClickCommand { get; set; }

        public FlightPartViewModel(Part part)
        {
            if(part == null)
            {
                return;
            }

            _part = part;
            Row = _part.Row;
            Column = _part.Column;
            PartImage = PartBuilder.GetPartImage(_part);
            Highlighted = false;
            PartContentsDescription = _part.ContentsDescription;

            _part.HighlightToggled += Part_HighlightToggled;
            _part.ContentsChanged += Part_ContentsChanged;
        }

        public void Remove()
        {
            _part = null;
            _partImage = null;
            PartClickCommand = null;
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
