using GalaxyTrucker.Model;
using GalaxyTrucker.Model.PartTypes;
using System;
using System.Drawing;
using GalaxyTrucker.Exceptions;

namespace GalaxyTrucker.Views.Utils
{
    public static class PartBuilder
    {
        private static readonly string _startingPath = "Resources/PartBuilder/";

        public static Image GetPartImage(Part part)
        {
            Image img = Image.FromFile($"{_startingPath}blank.png");
            Graphics g = Graphics.FromImage(img);

            for(int i = 0; i < 4; ++i)
            {
                string connectorPath = part.Connectors[i] switch
                {
                    Connector.Single => $"{_startingPath}connector_single.png",
                    Connector.Double => $"{_startingPath}connector_double.png",
                    Connector.Universal => $"{_startingPath}connector_universal.png",
                    _ => null
                };
                if(connectorPath != null)
                {
                    RotateFlipType rotation = i switch
                    {
                        0 => RotateFlipType.RotateNoneFlipNone,
                        1 => RotateFlipType.Rotate90FlipNone,
                        2 => RotateFlipType.Rotate180FlipNone,
                        _ => RotateFlipType.Rotate270FlipNone
                    };
                    Image connector = Image.FromFile(connectorPath);
                    connector.RotateFlip(rotation);
                    g.DrawImage(connector, new Point(0, 0));
                }
            }
            string partPath = _startingPath;
            switch (part)
            {
                case Battery b:
                    partPath += b.Capacity == 2 ? "part_battery2.png" : "part_battery3.png";
                    break;
                case Cockpit c:
                    partPath += c.Player switch
                    {
                        PlayerColor.Blue => "part_cockpitblue.png",
                        PlayerColor.Red => "part_cockpitred.png",
                        PlayerColor.Green => "part_cockpitgreen.png",
                        _ => "part_cockpityellow.png",
                    };
                    break;
                case Cabin _:
                    partPath += "part_cabin.png";
                    break;
                case EngineDouble _:
                    partPath += "part_enginedbl.png";
                    break;
                case Engine _:
                    partPath += "part_engine.png";
                    break;
                case LaserDouble _:
                    partPath += "part_laserdbl.png";
                    break;
                case Laser _:
                    partPath += "part_laser.png";
                    break;
                case Pipe _:
                    partPath += "part_pipe.png";
                    break;
                case EngineCabin _:
                    partPath += "part_enginecabin.png";
                    break;
                case LaserCabin _:
                    partPath += "part_lasercabin.png";
                    break;
                case Shield _:
                    partPath += "part_shield.png";
                    break;
                case SpecialStorage spStorage:
                    partPath += spStorage.Capacity switch
                    {
                        1 => "part_spstorage1.png",
                        _ => "part_spstorage2.png"
                    };
                    break;
                case Storage storage:
                    partPath += storage.Capacity switch
                    {
                        2 => "part_storage2.png",
                        _ => "part_storage3.png"
                    };
                    break;
                default:
                    partPath = null;
                    break;
            }
            if (partPath == null)
                throw new PartBuilderException();
            Image partPiece;
            try
            {
                partPiece = Image.FromFile(partPath);
            }catch(Exception)
            {
                partPiece = Image.FromFile($"{_startingPath}part_error.png");
            }
            g.DrawImage(partPiece, new Point(0, 0));

            return img;
        }
    }
}
