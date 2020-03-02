using System.Windows.Media.Imaging;
using Client.Model;

namespace Client.ViewModels
{
    /// <summary>
    /// Wrapper class for the Part class to be displayed with while hiding the logic components
    /// </summary>
    public class NotifyPart : NotifyBase
    {
        private readonly Part _part;

        public BitmapImage PartImage { get; private set; }

        public bool Highlighted { get; set; }

        public NotifyPart(Part part, int Row, int Column) =>
            (_part, this.Row, this.Column, PartImage, Highlighted) = (part, Row, Column, null, false);

        public void SetImage() => PartImage = PartBuilder.GetPartImage(_part);

        public int Row { get; set; }
        public int Column { get; set; }
    }
}
