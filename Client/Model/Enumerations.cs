namespace Client.Model
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

    public enum Projectiles
    {
        AsteroidSmall,
        AsteroidLarge,
        ShotSmall,
        ShotLarge
    }
}
