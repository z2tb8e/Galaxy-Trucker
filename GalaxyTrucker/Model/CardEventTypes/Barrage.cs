using System.Collections.Generic;
using System.Text;

namespace GalaxyTrucker.Model.CardEventTypes
{
    public class Barrage : CardEvent
    {
        public List<(Projectile, Direction)> Projectiles { get; }

        public Barrage(GameStage stage, List<(Projectile, Direction)> projectiles) : base(stage) => Projectiles = projectiles;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"{(int)Stage}b{Projectiles.Count}");
            foreach((Projectile, Direction) pair in Projectiles)
            {
                sb.Append(((int)pair.Item1).ToString() + ((int)pair.Item2).ToString());
            }
            return sb.ToString();
        }
    }
}
