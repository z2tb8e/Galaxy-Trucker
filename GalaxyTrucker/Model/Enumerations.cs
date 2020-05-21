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
        [Description("üres")]
        None = 0,
        [Description("1 ember")]
        HumanSingle = 1,
        [Description("2 ember")]
        HumanDouble = 2,
        [Description("lila űrlény")]
        LaserAlien = 3,
        [Description("barna űrlény")]
        EngineAlien = 4
    };

    public enum ShipLayout
    {
        [Description("kicsi")]
        Small,
        [Description("közepes")]
        Medium,
        [Description("széles")]
        BigWide,
        [Description("hosszú")]
        BigLong
    };

    public enum Projectile
    {
        [Description("kis meteor")]
        MeteorSmall = 0,
        [Description("nagy meteor")]
        MeteorLarge = 1,
        [Description("kis lövés")]
        ShotSmall = 2,
        [Description("nagy lövés")]
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
        [Description("eltalálták a pilótafülkét")]
        CockpitHit,
        [Description("elfogyott az emberi legénység")]
        OutOfHumans
    }

    public enum CardCheckAttribute
    {
        [Description("tűzerő")]
        Firepower = 0,
        [Description("motorerő")]
        Enginepower = 1,
        [Description("legénységszám")]
        CrewCount = 2
    }
    public enum CardEventPenalty
    {
        [Description("késés")]
        Delay = 0,
        [Description("legénység")]
        Crew = 1,
        [Description("áru")]
        Wares = 2,
        [Description("zárótűz")]
        Barrage = 3
    }

    public enum GameStage
    {
        [Description("első")]
        First = 0,
        [Description("második")]
        Second = 1,
        [Description("harmadik")]
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
