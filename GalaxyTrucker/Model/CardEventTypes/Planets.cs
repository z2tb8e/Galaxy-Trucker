﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GalaxyTrucker.Model.CardEventTypes
{
    public class Planets : CardEvent
    {
        private readonly bool[] _offersAvailable;

        public int DayCost { get; }

        public IEnumerable<IEnumerable<Ware>> Offers { get; }

        public Planets(GameStage stage, int dayCost, IEnumerable<IEnumerable<Ware>> offers) : base(stage)
        {
            LastResolved = 0;
            RequiresOrder = true;
            DayCost = dayCost;
            Offers = offers;
            _offersAvailable = new bool[Offers.Count()];
            for(int i = 0; i < Offers.Count(); ++i)
            {
                _offersAvailable[i] = true;
            }
        }

        public override bool IsResolved()
        {
            return LastResolved == 2;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"{(int)Stage}P{DayCost}{Offers.Count()}");
            foreach(List<Ware> offer in Offers)
            {
                sb.Append(offer.Count);
                foreach(Ware w in offer)
                {
                    sb.Append((int)w);
                }
            }
            return sb.ToString();
        }

        public override string GetDescription()
        {
            return $"Bolygók | -{DayCost} nap";
        }

        public override string ToolTip()
        {
            return $"Válassz egy ajánlatot a felsoroltak közül. Lemaradsz {DayCost} napot, de megkapod a felsorolt árukat.";
        }

        public override IEnumerable<OptionOrSubEvent> GetOptionsOrSubEvents()
        {
            List<OptionOrSubEvent> ret = new List<OptionOrSubEvent>()
            {
                new OptionOrSubEvent
                {
                    Description = "Semmi",
                    Action = (client, ship) =>
                    {
                        client.SendCardOption(0);
                        LastResolved = 1;
                    },
                    Condition = ship => LastResolved == 0
                }
            };

            for(int i = 0; i < Offers.Count(); ++i)
            {
                int index = i;
                IEnumerable<Ware> offer = Offers.ElementAt(index);
                ret.Add(new OptionOrSubEvent
                {
                    Description = $"Áruk: {string.Join(", ", offer.Select(ware => ware.GetDescription()))}",
                    Action = (client, ship) =>
                    {
                        client.SendCardOption(index);
                        LastResolved = 1;
                    },
                    Condition = ship => LastResolved == 0 && _offersAvailable.ElementAt(index)
                });
            }
            return ret;
        }

        /*option
         * 0: ignored
         * [1..Offers.Count()] = i, the (i-1)th offer
         * -i, the (i-1)th offer got taken by someone else
         */
        public override void ApplyOption(Ship ship, int option)
        {
            if (option < (-1 * Offers.Count()) || option > Offers.Count())
            {
                throw new ArgumentOutOfRangeException();
            }
            if(option < 0)
            {
                _offersAvailable[(-1 * option) - 1] = false;
                return;
            }
            if (option > 0)
            {
                ship.AddWares(Offers.ElementAt(option - 1));
            }
            LastResolved = 2;
        }
    }
}
