using System.Collections.Generic;

namespace GalaxyTrucker.Client.Model.CardEventTypes
{
    public class Barrage : CardEvent
    {
        public List<(Projectile, Direction)> Projectiles { get; }

        public Barrage(GameStage stage, List<(Projectile, Direction)> projectiles) : base(stage) => Projectiles = projectiles;
    }
}
