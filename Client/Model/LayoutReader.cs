using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Client.Model
{
    public class Layouts
    {
        public IList<int> Cockpit { get; set; }
        public IList<Elem> Small { get; set; }
        public IList<Elem> Medium { get; set; }
        public IList<Elem> BigWide { get; set; }
        public IList<Elem> BigLong { get; set; }
    };

    public class Elem
    {
        public int Row { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
    };

    public static class LayoutReader
    {
        private static readonly string _path = "../../../Resources/ShipLayouts.json";

        public static ((int,int),bool[,]) GetLayout(ShipLayout layout)
        {
            string jsonString = File.ReadAllText(_path);
            Layouts _obj = JsonSerializer.Deserialize<Layouts>(jsonString);
            bool[,] ret = new bool[11, 11];
            var singleLayout = layout switch
            {
                ShipLayout.Small => _obj.Small,
                ShipLayout.Medium => _obj.Medium,
                ShipLayout.BigWide => _obj.BigWide,
                ShipLayout.BigLong => _obj.BigLong,
                _ => _obj.Small,
            };
            foreach (Elem e in singleLayout)
            {
                for(int i = e.Start; i <= e.End; ++i)
                {
                    ret[e.Row, i] = true;
                }
            }
            return ((_obj.Cockpit[0], _obj.Cockpit[1]), ret);
        }

    }
}
