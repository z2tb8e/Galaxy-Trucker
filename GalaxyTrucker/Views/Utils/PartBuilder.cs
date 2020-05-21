using GalaxyTrucker.Model;
using GalaxyTrucker.Model.PartTypes;
using System.Drawing;
using GalaxyTrucker.Properties;
using System;

namespace GalaxyTrucker.Views.Utils
{
    public static class PartBuilder
    {
        public static Image GetPartImage(Part part)
        {
            Image img = Resources.blank;
            Graphics g = Graphics.FromImage(img);

            for(int i = 0; i < 4; ++i)
            {
                Image connector = part.Connectors[i] switch
                {
                    Connector.Single => Resources.connector_single,
                    Connector.Double => Resources.connector_double,
                    Connector.Universal => Resources.connector_universal,
                    _ => null
                };
                if (connector != null)
                {
                    RotateFlipType rotation = i switch
                    {
                        0 => RotateFlipType.RotateNoneFlipNone,
                        1 => RotateFlipType.Rotate90FlipNone,
                        2 => RotateFlipType.Rotate180FlipNone,
                        _ => RotateFlipType.Rotate270FlipNone
                    };
                    
                    connector.RotateFlip(rotation);
                    g.DrawImage(connector, new Point(0, 0));
                }
            }
            Image partPiece = null;
            switch (part)
            {
                case Battery b:
                    partPiece = b.Capacity == 2 ? Resources.part_battery2 : Resources.part_battery3;
                    break;
                case Cockpit c:
                    partPiece = c.Player switch
                    {
                        PlayerColor.Blue => Resources.part_cockpitblue,
                        PlayerColor.Red => Resources.part_cockpitred,
                        PlayerColor.Green => Resources.part_cockpitgreen,
                        _ => Resources.part_cockpityellow
                    };
                    break;
                case Cabin _:
                    partPiece = Resources.part_cabin;
                    break;
                case EngineDouble _:
                    partPiece = Resources.part_enginedbl;
                    break;
                case Engine _:
                    partPiece = Resources.part_engine;
                    break;
                case LaserDouble _:
                    partPiece = Resources.part_laserdbl;
                    break;
                case Laser _:
                    partPiece = Resources.part_laser;
                    break;
                case Pipe _:
                    partPiece = Resources.part_pipe;
                    break;
                case EngineCabin _:
                    partPiece = Resources.part_enginecabin;
                    break;
                case LaserCabin _:
                    partPiece = Resources.part_lasercabin;
                    break;
                case Shield _:
                    partPiece = Resources.part_shield;
                    break;
                case SpecialStorage spStorage:
                    partPiece = spStorage.Capacity switch
                    {
                        1 => Resources.part_spstorage1,
                        _ => Resources.part_spstorage2
                    };
                    break;
                case Storage storage:
                    partPiece = storage.Capacity switch
                    {
                        2 => Resources.part_storage2,
                        _ => Resources.part_storage3
                    };
                    break;
                default:
                    break;
            }
            if (partPiece == null)
            {
                throw new ArgumentException();
            }
            g.DrawImage(partPiece, new Point(0, 0));

            return img;
        }
    }
}
