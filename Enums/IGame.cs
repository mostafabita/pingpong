using System;

namespace PingPong.Common
{
    public abstract class Game
    {
        public GameLevel Level;
        public FoodNumber Foods;
        public int Rows;
        public int Cols;
        public int NutWidth;
        public int Speed;

        protected Game(int row, int cols, int nutWidth, int speed, FoodNumber foods)
        {
            Level = GameLevel.Custom;
            Rows = row;
            Cols = cols;
            NutWidth = nutWidth;
            Speed = speed;
            Foods = foods;
        }
    }

    public class BegineerGame : Game
    {
        public BegineerGame() : base(5, 20, 15, 4, FoodNumber.Much)
        {

        }
    }

    public class IntermediateGame : Game
    {
        public IntermediateGame() : base(7, 25, 15, 6, FoodNumber.Normal)
        {

        }
    }

    public class AdvancedGame : Game
    {
        public AdvancedGame() : base(7, 30, 15, 8, FoodNumber.Much)
        {

        }
    }
}