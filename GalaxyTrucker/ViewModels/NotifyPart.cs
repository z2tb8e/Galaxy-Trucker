using System.Windows.Media.Imaging;
using GalaxyTrucker.Model;

namespace GalaxyTrucker.ViewModels
{
    /// <summary>
    /// Wrapper class for the Part class to be displayed with while hiding the logic components
    /// </summary>
    public class NotifyPart : NotifyBase
    {
        private readonly Part _part;

        public BitmapImage PartImage { get; private set; }

        public bool Highlighted { get; set; }

        public NotifyPart(Part part, int row, int column)
        {
            _part = part;
            Row = row;
            Column = column;
            PartImage = null;
            Highlighted = false;
        }

        public void SetImage() => PartImage = PartBuilder.GetPartImage(_part);

        public void Rotate(Direction d) => _part.Rotate(d);

        public Direction Rotation => _part.Rotation;

        public int Row
        {
            get => _part.Pos1;
            set => _part.Pos1 = value;
        }

        public int Column
        {
            get => _part.Pos2;
            set => _part.Pos2 = value;
        }
    }
}
