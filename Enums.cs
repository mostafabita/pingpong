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

    public enum Quantity
    {
        Rare = 20,
        Few = 12,
        Normal = 10,
        Much = 6,
        VeryMuch = 3
    }

    public enum GameLevel
    {
        Beginner,
        Intermediate,
        Advanced,
        Custom
    }
}