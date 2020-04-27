using System;
using System.Collections.Generic;
using System.Text;

namespace GalaxyTrucker.Model
{
    public class GameModel
    {
        private PlayerColor _player;

        public Ship Ship { get; }

        public List<PlayerColor> PlayerOrder { get; set; }

        public Dictionary<PlayerColor, PlayerAttributes> PlayerAttributes { get; set; }

        public GameModel()
        {
            PlayerOrder = new List<PlayerColor>();
        }
    }
}
