using System.Drawing;

namespace GalaxyTrucker.ViewModels
{

    public class OrderFieldViewModel : NotifyBase
    {
        private Image _token;

        public Image Token
        {
            get
            {
                return _token;
            }
            set
            {
                _token = value;
                OnPropertyChanged();
            }
        }

        public int Row { get; }

        public int Column { get; }

        public OrderFieldViewModel(int row, int column)
        {
            Row = row;
            Column = column;
        }
    }
}
