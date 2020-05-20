using GalaxyTrucker.Network;
using System;

namespace GalaxyTrucker.Model
{
    public class OptionOrSubEvent
    {
        public int Value { get; set; }

        public string Description { get; set; }

        public Func<Ship, bool> Condition { get; set; }
        
        public Action<GTTcpClient, Ship> Action { get; set; }
    }
}
