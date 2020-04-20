namespace GalaxyTrucker.Client.Model
{
    public enum Direction
    {
        Top = 0,
        Right,
        Bottom,
        Left
    };

    public enum Connector
    {
        None,
        Single,
        Double,
        Universal
    };

    public enum Ware
    {
        Empty = 0,
        Blue,
        Green,
        Yellow,
        Red
    };

    public enum Personnel
    {
        None = 0,
        HumanSingle,
        HumanDouble,
        LaserAlien,
        EngineAlien
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
        AsteroidSmall,
        AsteroidLarge,
        ShotSmall,
        ShotLarge
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
}
