namespace Pong
{
    public enum Direction
    {
        N,
        S,
        NE,
        SE,
        NW,
        SW
    }

    public enum NutType
    {
        Ball,
        Earth,
        Nut,
        Paddle,
        Wall
    }

    public enum FoodType
    {
        Stick,
        Death,
        Live,
        Big,
        Small,
        SpeedDown,
        SpeedUp,
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