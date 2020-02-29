using Client.Model.PartTypes;
using System.Windows;

namespace Client.Model
{
    public class Ship
    {
        private readonly bool[,] _movableFields;
        private Part[,] _parts;
        private readonly int _penaltyCap;

        public Ship(ShipLayout layout)
        {
            (int, int) cockpit;
            (cockpit, _movableFields) = LayoutReader.GetLayout(layout);
            _parts = new Part[11, 11];
            _parts[cockpit.Item1, cockpit.Item2] = new Cockpit();
            _penaltyCap = layout switch
            {
                ShipLayout.Small => 5,
                ShipLayout.Medium => 8,
                _ => 11,
            };
            string parasztdebug = "";
            for(int i = 0; i < _movableFields.GetLength(0); ++i)
            {
                for(int j = 0; j < _movableFields.GetLength(1); ++j)
                {
                    parasztdebug += (_movableFields[i, j] ? '1' : '0');
                }
                parasztdebug += '\n';
            }
            MessageBox.Show(parasztdebug, "cső");
        }
    }
}
