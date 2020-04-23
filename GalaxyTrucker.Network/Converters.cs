using GalaxyTrucker.Client.Model;
using GalaxyTrucker.Client.Model.CardEventTypes;
using GalaxyTrucker.Client.Model.PartTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace GalaxyTrucker.Network
{
    public static class Converters
    {
        public static string PartToString(Part p)
        {
            if(p is Cockpit)
            {
                int playerColor = (int)(p as Cockpit).Player;
                return ("C" + playerColor);
            }

            StringBuilder stringBuilder = new StringBuilder();
            foreach (Connector c in p.Connectors)
            {
                stringBuilder.Append((int)c);
            }

            stringBuilder.Append(p switch
            {
                Battery b => ("b" + b.Capacity),
                Cabin _ => ("c"),
                EngineDouble _ => ("E"),
                Engine _ => ("e"),
                LaserDouble _ => ("L"),
                Laser _ => ("l"),
                Pipe _ => ("p"),
                EngineCabin _ => ("a"),
                LaserCabin _ => ("A"),
                Shield _ => ("d"),
                SpecialStorage spStorage => ("S" + spStorage.Capacity),
                Storage storage => ("s" + storage.Capacity),
                _ => throw new InvalidCastException("Unrecognized Part type")
            });

            return stringBuilder.ToString();
        }

        public static Part StringToPart(string str)
        {
            if(str[0] == 'C')
            {
                PlayerColor color = (PlayerColor)(int.Parse("" + str[0]));
                return new Cockpit(color);
            }

            Connector[] connectors = new Connector[4];
            for(int i = 0; i < 4; ++i)
            {
                connectors[i] = (Connector)int.Parse("" + str[i]);
            }

            Part p = (str[4]) switch
            {
                'b' => new Battery(connectors[0], connectors[1], connectors[2], connectors[3], int.Parse("" + str[5])),
                'c' => new Cabin(connectors[0], connectors[1], connectors[2], connectors[3]),
                'E' => new EngineDouble(connectors[0], connectors[1], connectors[2], connectors[3]),
                'e' => new Engine(connectors[0], connectors[1], connectors[2], connectors[3]),
                'L' => new LaserDouble(connectors[0], connectors[1], connectors[2], connectors[3]),
                'l' => new Laser(connectors[0], connectors[1], connectors[2], connectors[3]),
                'p' => new Pipe(connectors[0], connectors[1], connectors[2], connectors[3]),
                'a' => new EngineCabin(connectors[0], connectors[1], connectors[2], connectors[3]),
                'A' => new LaserCabin(connectors[0], connectors[1], connectors[2], connectors[3]),
                'd' => new Shield(connectors[0], connectors[1], connectors[2], connectors[3]),
                'S' => new SpecialStorage(connectors[0], connectors[1], connectors[2], connectors[3], int.Parse("" + str[5])),
                's' => new Storage(connectors[0], connectors[1], connectors[2], connectors[3], int.Parse("" + str[5])),
                _ => throw new InvalidCastException("Unrecognized Part type character"),
            };
            return p;
        }

        public static string CardEventToString(CardEvent c)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append((int)c.Stage);

            switch (c)
            {
                case AbandonedShip ship:
                    stringBuilder.Append("a");
                    stringBuilder.Append(ship.CrewCost.ToString("X") + ship.DayCost + ship.Reward.ToString("X"));
                    break;
                case AbandonedStation station:
                    stringBuilder.Append("A");
                    stringBuilder.Append(station.MinimumCrew.ToString("X") + station.DayCost + station.Wares.Count);
                    foreach (Ware w in station.Wares)
                    {
                        stringBuilder.Append((int)w);
                    }
                    break;
                case Barrage barrage:
                    stringBuilder.Append("b");
                    stringBuilder.Append(barrage.Projectiles.Count);
                    foreach ((Projectile, Direction) pair in barrage.Projectiles)
                    {
                        stringBuilder.Append((int)pair.Item1 + "" + (int)pair.Item2);
                    }
                    break;
                case Pirates pirates:
                    stringBuilder.Append("y");
                    stringBuilder.Append(pirates.Firepower.ToString("X") + pirates.DayCost + pirates.Reward.ToString("X") + pirates.Penalty.Count);
                    foreach ((Projectile, Direction) pair in pirates.Penalty)
                    {
                        stringBuilder.Append((int)pair.Item1 + "" + (int)pair.Item2);
                    }
                    break;
                case Smugglers smugglers:
                    stringBuilder.Append("d");
                    stringBuilder.Append(smugglers.Firepower.ToString("X") + smugglers.DayCost + smugglers.Reward.Count);
                    foreach(Ware w in smugglers.Reward)
                    {
                        stringBuilder.Append((int)w);
                    }
                    stringBuilder.Append(smugglers.Penalty);
                    break;
                case Slavers slavers:
                    stringBuilder.Append("S");
                    stringBuilder.Append(slavers.Firepower.ToString("X") + slavers.DayCost + slavers.Reward.ToString("X") + slavers.Penalty);
                    break;
                case OpenSpace _:
                    stringBuilder.Append("o");
                    break;
                case Pandemic _:
                    stringBuilder.Append("p");
                    break;
                case Sabotage _:
                    stringBuilder.Append("g");
                    break;
                case Planets planets:
                    stringBuilder.Append("P");
                    stringBuilder.Append(planets.DayCost + "" + planets.Offers.Count);
                    foreach (List<Ware> offer in planets.Offers)
                    {
                        stringBuilder.Append(offer.Count);
                        foreach(Ware w in offer)
                        {
                            stringBuilder.Append((int)w);
                        }
                    }
                    break;
                case Stardust _:
                    stringBuilder.Append("s");
                    break;
                case Warzone warzone:
                    stringBuilder.Append("w");
                    stringBuilder.Append((int)warzone.Event1.Attribute + "" + (int)warzone.Event1.PenaltyType + "" + warzone.Event1.Penalty);
                    stringBuilder.Append((int)warzone.Event2.Attribute + "" + (int)warzone.Event2.PenaltyType + "" + warzone.Event2.Penalty);
                    stringBuilder.Append((int)warzone.Event3.Attribute + "" + (int)warzone.Event3.PenaltyType + "" + warzone.Event3.Penalty.Count);
                    foreach((Projectile, Direction) pair in warzone.Event3.Penalty)
                    {
                        stringBuilder.Append((int)pair.Item1 + "" + (int)pair.Item2);
                    }
                    break;
                default:
                    throw new InvalidCastException("Unrecognized CardEvent type");
            }

            return stringBuilder.ToString();
        }

        public static CardEvent StringToCardEvent(string str)
        {
            CardEvent ret;

            //First character indicates stage

            GameStage stage = (GameStage)int.Parse("" + str[0]);

            //Second character indicates subclass
            switch (str[1])
            {
                case 'a':
                    //format: 'Stage''a''CrewCost''DayCost''Reward'
                    ret = new AbandonedShip
                    (
                        stage,
                        Convert.ToInt32("" + str[2], 16),
                        int.Parse("" + str[3]),
                        Convert.ToInt32("" + str[4], 16)
                    );
                    break;
                case 'A':
                    //format: 'Stage''A''MinimumCrew''DayCost''Wares.Count'('Ware')+
                    List<Ware> stationWares = new List<Ware>();
                    int wareCount = int.Parse("" + str[4]);
                    for(int i = 0; i < wareCount; ++i)
                    {
                        stationWares.Add((Ware)(int.Parse("" + str[i + 5])));
                    }
                    ret = new AbandonedStation
                    (
                        stage,
                        Convert.ToInt32("" + str[2], 16),
                        int.Parse("" + str[3]),
                        stationWares
                    );
                    break;
                case 'b':
                    //format: 'Stage''b''Projectiles.Count'('Projectile','Direction')+
                    List<(Projectile, Direction)> projectiles = new List<(Projectile, Direction)>();
                    int projectileCount = int.Parse("" + str[2]);
                    for(int i = 0; i < projectileCount; ++i)
                    {
                        projectiles.Add(((Projectile)(int.Parse("" + str[3 + 2*i])), (Direction)int.Parse("" + str[4 + 2*i])));
                    }
                    ret = new Barrage(stage, projectiles);
                    break;
                case 'y':
                    //format: 'Stage''y''Firepower''DayCost''Reward''Penalty.Projectiles.Count'('Projectile','Direction')+
                    List<(Projectile, Direction)> pirateProjectiles = new List<(Projectile, Direction)>();
                    int pirateProjectileCount = int.Parse("" + str[5]);
                    for(int i = 0; i < pirateProjectileCount; ++i)
                    {
                        pirateProjectiles.Add(((Projectile)(int.Parse("" + str[6 + 2*i])), (Direction)int.Parse("" + str[7 + 2*i])));
                    }
                    ret = new Pirates
                    (
                        stage,
                        Convert.ToInt32("" + str[2], 16),
                        int.Parse("" + str[3]),
                        pirateProjectiles,
                        Convert.ToInt32("" + str[4], 16)
                    );
                    break;
                case 'd':
                    //format: 'Stage''d''Firepower''DayCost''Reward.Count'('Ware')+'Penalty'
                    List<Ware> smugglerWares = new List<Ware>();
                    int smugglerWareCount = int.Parse("" + str[4]);
                    for(int i = 0; i < smugglerWareCount; ++i)
                    {
                        smugglerWares.Add((Ware)int.Parse("" + str[5 + i]));
                    }
                    ret = new Smugglers
                    (
                        stage,
                        Convert.ToInt32("" + str[2], 16),
                        int.Parse("" + str[3]),
                        int.Parse("" + str[5 + smugglerWareCount]),
                        smugglerWares
                    );
                    break;
                case 'S':
                    //format: 'Stage''S''Firepower''DayCost''Reward''Penalty'
                    ret = new Slavers
                    (
                        stage,
                        Convert.ToInt32("" + str[2], 16),
                        int.Parse("" + str[3]),
                        int.Parse("" + str[5]),
                        Convert.ToInt32("" + str[4], 16)
                    );
                    break;
                case 'o':
                    //format: 'Stage''o'
                    ret = new OpenSpace(stage);
                    break;
                case 'p':
                    //format: 'Stage''p'
                    ret = new Pandemic(stage);
                    break;
                case 'g':
                    //format: 'Stage''g'
                    ret = new Sabotage(stage);
                    break;
                case 'P':
                    //format: 'Stage''P''DayCost''Offers.Count'('offer.count'('Ware')+)+
                    List<List<Ware>> offers = new List<List<Ware>>();
                    int offerCount = int.Parse("" + str[3]);
                    int index = 0;
                    for(int i = 0; i < offerCount; ++i)
                    {
                        int offerWareCount = int.Parse("" + str[4 + index]);
                        List<Ware> offer = new List<Ware>();
                        for(int j = 0; j < offerWareCount; ++j)
                        {
                            offer.Add((Ware)int.Parse("" + str[5 + index + j]));
                        }
                        offers.Add(offer);
                        index += offerWareCount + 1;
                    }
                    ret = new Planets
                    (
                        stage,
                        int.Parse("" + str[2]),
                        offers
                    );
                    break;
                case 's':
                    //format: 'Stage''s'
                    ret = new Stardust(stage);
                    break;
                case 'w':
                    //format: 'Stage''w''Event1.Attribute''Event1.PenaltyType''Event1.Penalty'
                    //                  'Event2.Attribute''Event2.PenaltyType''Event2.Penalty"
                    //                  'Event3.Attribute''Event3.PenaltyType''Event3.Penalty.Projectiles.Count'('Projectile','Direction')+
                    WarzoneEvent<int> event1 = new WarzoneEvent<int>
                    (
                        (CardCheckAttribute)int.Parse("" + str[2]),
                        (CardEventPenalty)int.Parse("" + str[3]),
                        int.Parse("" + str[4])
                    );
                    WarzoneEvent<int> event2 = new WarzoneEvent<int>
                    (
                        (CardCheckAttribute)int.Parse("" + str[5]),
                        (CardEventPenalty)int.Parse("" + str[6]),
                        int.Parse("" + str[7])
                    );
                    List<(Projectile, Direction)> event3Projectiles = new List<(Projectile, Direction)>();
                    int event3ProjectilesCount = int.Parse("" + str[10]);
                    for(int i = 0; i < event3ProjectilesCount; ++i)
                    {
                        event3Projectiles.Add(((Projectile)int.Parse("" + str[11 + 2*i]), (Direction)int.Parse("" + str[12 + 2*i])));
                    }
                    WarzoneEvent<List<(Projectile, Direction)>> event3 = new WarzoneEvent<List<(Projectile, Direction)>>
                    (
                        (CardCheckAttribute)int.Parse("" + str[8]),
                        (CardEventPenalty)int.Parse("" + str[9]),
                        event3Projectiles
                    );
                    ret = new Warzone(stage, event1, event2, event3);
                    break;
                default:
                    throw new InvalidCastException("Unrecognized CardEvent type character");
            }

            return ret;
        }
    }
}
