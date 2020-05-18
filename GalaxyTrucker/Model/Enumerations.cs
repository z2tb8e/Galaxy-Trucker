using System;
using System.ComponentModel;

namespace GalaxyTrucker.Model
{
    public enum Direction
    {
        [Description("Felülről")]
        Top = 0,
        [Description("Jobbról")]
        Right = 1,
        [Description("Alulról")]
        Bottom = 2,
        [Description("Balról")]
        Left = 3,
    };

    public enum Connector
    {
        None = 0,
        Single = 1,
        Double = 2,
        Universal = 3
    };

    public enum Ware
    {
        [Description("Üres")]
        Empty = 0,
        [Description("Kék")]
        Blue = 1,
        [Description("Zöld")]
        Green = 2,
        [Description("Sárga")]
        Yellow = 3,
        [Description("Piros")]
        Red = 4
    };

    public enum Personnel
    {
        [Description("Üres")]
        None = 0,
        [Description("1 ember")]
        HumanSingle = 1,
        [Description("2 ember")]
        HumanDouble = 2,
        [Description("Lila űrlény")]
        LaserAlien = 3,
        [Description("Barna űrlény")]
        EngineAlien = 4
    };

    public enum ShipLayout
    {
        [Description("Kicsi")]
        Small,
        [Description("Közepes")]
        Medium,
        [Description("Széles")]
        BigWide,
        [Description("Hosszú")]
        BigLong
    };

    public enum Projectile
    {
        [Description("Kis meteor")]
        MeteorSmall = 0,
        [Description("Nagy meteor")]
        MeteorLarge = 1,
        [Description("Kis lövés")]
        ShotSmall = 2,
        [Description("Nagy lövés")]
        ShotLarge = 3
    };

    public enum PlayerColor
    {
        [Description("Sárga")]
        Yellow,
        [Description("Piros")]
        Red,
        [Description("Kék")]
        Blue,
        [Description("Zöld")]
        Green
    }

    public enum WreckedSource
    {
        CockpitHit,
        OutOfHumans
    }

    public enum CardCheckAttribute
    {
        [Description("Tűzerő")]
        Firepower = 0,
        [Description("Motorerő")]
        Enginepower = 1,
        [Description("Legénységszám")]
        CrewCount = 2
    }
    public enum CardEventPenalty
    {
        [Description("Késés")]
        Delay = 0,
        [Description("Legénység")]
        Crew = 1,
        [Description("Áruk")]
        Wares = 2,
        [Description("Lövések")]
        Barrage = 3
    }

    public enum GameStage
    {
        [Description("Első")]
        First = 0,
        [Description("Második")]
        Second = 1,
        [Description("Harmadik")]
        Third = 2
    }

    [Flags]
    public enum PartAddProblems
    {
        None = 0,
        Occupied = 1,
        HasNoConnection = 2,
        ConnectorsDontMatch = 4,
        BlockedAsLaser = 8,
        BlockedAsEngine = 16,
        BlocksLaser = 32,
        BlocksEngine = 64
    }
}
