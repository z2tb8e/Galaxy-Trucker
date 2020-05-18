﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                (Projectile, Direction) projectile = Projectiles.ElementAt(i);
                waves.Add(new OptionOrSubEvent
                {
                    Description = $"{projectile.Item1.GetDescription()}, {projectile.Item2.GetDescription()}",
                    Action = (client, ship) =>
                    {
                        //set the wavenumber to negative, so that the command can't be activated again before the option is resolved
                        LastResolved = -1 * LastResolved;
                        ApplyOption(ship, LastResolved);
                        LastResolved = (-1 * LastResolved) + 1;
                    },
                    Condition = ship => LastResolved == (i + 1)
                });
            }

            return waves;
        }

        public override void ApplyOption(Ship ship, int option)
        {
            int actualOption = -1 * option;
            if(actualOption < 1 || actualOption > Projectiles.Count())
            {
                throw new ArgumentOutOfRangeException();
            }
            if(option != LastResolved)
            {
                throw new InvalidOperationException();
            }
            (Projectile, Direction) projectile = Projectiles.ElementAt(actualOption - 1);
            Random random = new Random();
            int roll1 = random.Next(6);
            int roll2 = random.Next(6);

            //OnDiceRolled sets the thread waiting
            OnDiceRolled(roll1, roll2);

            ship.ApplyProjectile(projectile.Item1, projectile.Item2, roll1 + roll2);
            ++LastResolved;
        }

        public override string ToolTip()
        {
            return "A kis meteorok csak akkor sebzik a hajódat, ha nyitott csatlakozóba csapódnának." +
                "\nA nagy meteorok csak akkor NEM sebzik a hajódat, ha feléjük néző lézerbe csapódnának, ekkor ha aktiválva van, az lelövi.";
        }
    }
}
