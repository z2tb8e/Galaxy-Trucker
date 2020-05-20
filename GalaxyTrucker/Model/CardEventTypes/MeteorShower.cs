using GalaxyTrucker.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GalaxyTrucker.Model.CardEventTypes
{
    public class MeteorShower : CardEvent
    {
        public IEnumerable<(Projectile, Direction)> Projectiles { get; }

        public MeteorShower(GameStage stage, IEnumerable<(Projectile, Direction)> projectiles) : base(stage)
        {
            Projectiles = projectiles;
            LastResolved = 1;
        }

        public override bool IsResolved()
        {
            return LastResolved == 1 + Projectiles.Count();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"{(int)Stage}m{Projectiles.Count()}");
            foreach((Projectile, Direction) pair in Projectiles)
            {
                sb.Append(((int)pair.Item1).ToString() + ((int)pair.Item2).ToString());
            }
            return sb.ToString();
        }

        public override string GetDescription()
        {
            return "Meteorzápor";
        }

        public override IEnumerable<OptionOrSubEvent> GetOptionsOrSubEvents()
        {
            List<OptionOrSubEvent> waves = new List<OptionOrSubEvent>();
            for(int i = 0; i < Projectiles.Count(); ++i)
            {
                
                waves.Add(new OptionOrSubEvent
                {
                    Value = i,
                });
            }
            foreach(OptionOrSubEvent item in waves)
            {
                (Projectile, Direction) projectile = Projectiles.ElementAt(item.Value);
                item.Description = $"{projectile.Item1} {projectile.Item2}";
                item.Action = new Action<GTTcpClient, Ship>((client, ship) =>
                {
                    //set the wavenumber to negative, so that the command can't be activated again before the option is resolved
                    LastResolved = -1 * LastResolved;
                    ApplyOption(ship, LastResolved);
                });
                item.Condition = new Func<Ship, bool>(ship => LastResolved == (item.Value + 1));
            }

            return waves;
        }

        public async override void ApplyOption(Ship ship, int option)
        {
            int actualOption = -1 * option;
            if (actualOption < 1 || actualOption > Projectiles.Count())
            {
                throw new ArgumentOutOfRangeException();
            }
            if (option != LastResolved)
            {
                throw new InvalidOperationException();
            }
            (Projectile, Direction) projectile = Projectiles.ElementAt(actualOption - 1);
            Random random = new Random();
            int roll1 = random.Next(6);
            int roll2 = random.Next(6);

            //OnDiceRolled sets the thread waiting
            await Task.Run(() => OnDiceRolled(roll1, roll2));

            ship.ApplyProjectile(projectile.Item1, projectile.Item2, roll1 + roll2);
            LastResolved = (-1 * LastResolved) + 1;
        }

        public override string ToolTip()
        {
            return "A kis meteorok csak akkor sebzik a hajódat, ha nyitott csatlakozóba csapódnának." +
                "\nA nagy meteorok csak akkor NEM sebzik a hajódat, ha feléjük néző lézerbe csapódnának, ekkor ha aktiválva van, az lelövi.";
        }
    }
}
