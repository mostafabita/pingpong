using System;
using System.Collections.Generic;

namespace Pong
{
    public class Game
    {
        public GameLevel Level { get; private set; }
        public FoodNumber Foods { get; private set; }
        public int Rows { get; private set; }
        public int Cols { get; private set; }
        public int NutWidth { get; private set; }
        public int Speed { get; set; }

        public Game(GameLevel level)
        {
            var levelInitFuncDic = new Dictionary<GameLevel, Action>
            {
                {GameLevel.Beginner, () => Initialize(GameLevel.Beginner, 5, 17, 20, 2, FoodNumber.Much)},
                {GameLevel.Intermediate, () => Initialize(GameLevel.Intermediate, 7, 25, 15, 6, FoodNumber.Normal)},
                {GameLevel.Advanced, () => Initialize(GameLevel.Advanced, 7, 30, 15, 8, FoodNumber.Few)}
            };

            levelInitFuncDic[level]();
        }

        public Game(int row, int cols, int nutWidth, int speed, FoodNumber foods)
        {
            Initialize(GameLevel.Custom, row, cols, nutWidth, speed, foods);
        }

        protected void Initialize(GameLevel level, int row, int cols, int nutWidth, int speed, FoodNumber foods)
        {
            Level = level;
            Rows = row;
            Cols = cols;
            NutWidth = nutWidth;
            Speed = speed;
            Foods = foods;
        }

        public FoodType RandomFood()
        {
            var rnd = new Random();
            return rnd.Next((int)Foods) == 1 ? (FoodType)rnd.Next(0, 6) : FoodType.Null;
        }
    }
}