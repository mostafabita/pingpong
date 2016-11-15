namespace PingPong.Enums
{
    internal enum Direction
    {
        N,
        S,
        NE,
        SE,
        NW,
        SW
    }

    internal enum NutType
    {
        Ball,
        Earth,
        Nut,
        Rocket,
        Wall
    }

    internal enum FoodType
    {
        Stick,
        Death,
        Live,
        Big,
        Small,
        SpeedDown,
        SpeedUp,
        Image,
        Null
    }

    public enum FoodNumber
    {
        Few = 12,
        Normal = 10,
        Much = 6
    }

    public enum GameLevel
    {
        Beginner,
        Intermediate,
        Advanced,
        Custom
    }
}