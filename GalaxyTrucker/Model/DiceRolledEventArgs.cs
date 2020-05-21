using System;

namespace GalaxyTrucker.Model
{
    public class DiceRolledEventArgs : EventArgs
    {
        public Projectile Projectile { get; set; }

        public Direction Direction { get; set; }

        public int Number { get; set; }

        public DiceRolledEventArgs(Projectile projectile, Direction direction, int number)
        {
            Projectile = projectile;
            Direction = direction;
            Number = number;
        }
    }
}
