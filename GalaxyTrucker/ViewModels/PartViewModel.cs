using System;
using System.Drawing;
using GalaxyTrucker.Model;

namespace GalaxyTrucker.ViewModels
{
    public class PartViewModel : NotifyBase
    {
        private Part _part;
        private bool _highlighted;
        private bool _isValidField;
        private Image _partImage;
        private int _shipRow;
        private int _shipColumn;

        #region properties

        public Image PartImage
        {
            get
            {
                return _partImage;
            }
            set
            {
                if (_partImage != value)
                {
                    _partImage = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsValidField
        {
            get
            {
                return _isValidField;
            }
            set
            {
                _isValidField = value;
                OnPropertyChanged();
            }
        }

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

        public Part Part
        {
            get
            {
                return _part;
            }
            set
            {
                if (_part != value)
                {
                    _part = value;
                    OnPropertyChanged(nameof(Rotation));
                    if(value != null)
                    {
                        Part.HighlightToggled += Part_HighlightToggled;
                    }
                }
            }
        }

        public Direction Rotation
        {
            get
            {
                if (Part == null)
                {
                    return Direction.Top;
                }
                else
                {
                    return Part.Rotation;
                }
            }
        }

        public int BuildRow { get; set; }

        public int BuildColumn { get; set; }

        public int ShipRow
        {
            get
            {
                return _shipRow;
            }
            set
            {
                if (_shipRow != value)
                {
                    _shipRow = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ShipColumn
        {
            get
            {
                return _shipColumn;
            }
            set
            {
                if (_shipColumn != value)
                {
                    _shipColumn = value;
                    OnPropertyChanged();
                }
            }
        }
        public DelegateCommand PartClickCommand { get; set; }

        #endregion

        public PartViewModel()
        {
            Highlighted = false;
        }

        public void Rotate(int leftOrRight)
        {
            Part.Rotate(leftOrRight);
            OnPropertyChanged(nameof(Rotation));
        }

        private void Part_HighlightToggled(object sender, EventArgs e)
        {
            Highlighted = !Highlighted;
        }
    }
}
