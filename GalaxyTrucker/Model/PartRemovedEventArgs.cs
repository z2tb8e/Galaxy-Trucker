using System;

namespace GalaxyTrucker.Model
{
    public class PartRemovedEventArgs : EventArgs
    {
        public int Row { get; set; }

        public int Column { get; set; }

        public PartRemovedEventArgs(int row, int column)
        {
            Row = row;
            Column = column;
        }
    }
}
