using System;

namespace GalaxyTrucker.Model
{
    public enum Direction
    {
        Top,
        Right,
        Bottom,
        Left,
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
        Empty = 0,
        Blue = 1,
        Green = 2,
        Yellow = 3,
        Red = 4
    };

    public enum Personnel
    {
        None = 0,
        HumanSingle = 1,
        HumanDouble = 2,
        LaserAlien = 3,
        EngineAlien = 4
    };

    public enum ShipLayout
    {
        Small,
        Medium,
        BigWide,
        BigLong
    };

    public enum Projectile
    {
        AsteroidSmall = 0,
        AsteroidLarge = 1,
        ShotSmall = 2,
        ShotLarge = 3
    };

    public enum PlayerColor
    {
        Yellow,
        Red,
        Blue,
        Green
    }

    public enum WreckedSource
    {
        CockpitHit,
        OutOfHumans
    }

    public enum CardCheckAttribute
    {
        Firepower = 0,
        Enginepower = 1,
        CrewCount = 2
    }
    public enum CardEventPenalty
    {
        Delay = 0,
        Crew = 1,
        Wares = 2,
        Barrage = 3
    }

    public enum GameStage
    {
        First = 0,
        Second = 1,
        Third = 2
    }

    [Flags]
    public enum PartAddProblems
    {
        None,
        Occupied,
        HasNoConnection,
        ConnectorsDontMatch,
        BlockedAsLaser,
        BlockedAsEngine,
        BlocksLaser,
        BlocksEngine
    }
}
