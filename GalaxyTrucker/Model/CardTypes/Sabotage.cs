using System;
using System.Linq;

namespace GalaxyTrucker.Model.CardTypes
{
    public class Sabotage : Card
    {
        public Sabotage(GameStage stage) : base(stage)
        {
            RequiresAttributes = true;
            LastResolved = 0;
        }

        public override bool IsResolved()
        {
            return LastResolved == 1;
        }

        public override string ToString()
        {
            return $"{(int)Stage}g";
        }

        public override string GetDescription()
        {
            return "Szabotázs";
        }

        /*option
         * 0: this ship is the target
         * 1: other ship was the target
         */
        public override void ApplyOption(Ship ship, int option)
        {
            if(option < 0 || option > 1)
            {
                throw new ArgumentOutOfRangeException();
            }
            if(option == 0)
            {
                Random random = new Random();
                for(int i = 0; i < 3; ++i)
                {
                    int row = random.Next(6) + random.Next(6);
                    int column = random.Next(6) + random.Next(6);
                    if(ship.Parts.FirstOrDefault(p => p != null && p.Row == row && p.Column == column) != null)
                    {
                        ship.RemovePartAtIndex(row, column);
                        break;
                    }
                }
            }
            LastResolved = 1;
        }

        public override string ToolTip()
        {
            return "A játékos a legkevesebb legénységgel (holtverseny esetén az előbb álló) veszít egy alkatrészt." +
                "\nHáromszor próbál a játék egy véletlen alkatrészt eltávolítani, ha sikerül egyet, akkor tovább nem próbálkozik.";
        }
    }
}
