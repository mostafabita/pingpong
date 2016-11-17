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
        Wall,
        Null
    }

    public enum FoodType
    {
        Stick,
        Death,
        Heart,
        Grow,
        Shrink,
        Slower,
        Faster,
        Null
    }

    public enum Quantity
    {
        Rare = 20,
        Few = 12,
        Normal = 10,
        Much = 6,
        VeryMuch = 1
    }

    public enum NutBehavior
    {
        Continue,
        Earth,
        Paddle,
        Others
    }

    public enum GameLevel
    {
        Beginner,
        Intermediate,
        Advanced,
        Custom
    }
}