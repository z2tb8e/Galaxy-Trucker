using System.Collections.Generic;

namespace GalaxyTrucker.Client.Model.CardEventTypes
{
    public class Barrage : CardEvent
    {
        public List<(Projectile, Direction)> Projectiles { get; }

        public Barrage(List<(Projectile, Direction)> projectiles) => Projectiles = projectiles;
    }
}
