using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace GalaxyTrucker.Model
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
        private static readonly string _path = "Resources/ShipLayouts.json";

        public static ((int,int),bool[,]) GetLayout(ShipLayout layout)
        {
            string jsonString = File.ReadAllText(_path);
            Layouts obj = JsonSerializer.Deserialize<Layouts>(jsonString);
            bool[,] ret = new bool[11, 11];
            var singleLayout = layout switch
            {
                ShipLayout.Small => obj.Small,
                ShipLayout.Medium => obj.Medium,
                ShipLayout.BigWide => obj.BigWide,
                ShipLayout.BigLong => obj.BigLong,
                _ => obj.Small,
            };
            foreach (Elem e in singleLayout)
            {
                for(int i = e.Start; i <= e.End; ++i)
                {
                    ret[e.Row, i] = true;
                }
            }
            return ((obj.Cockpit[0], obj.Cockpit[1]), ret);
        }

    }
}
