using System;
using System.Collections.Generic;

namespace Pong
{
    public class Game
    {
        public GameLevel Level { get; private set; }
        public Quantity FoodQuantity { get; private set; }
        public int Rows { get; private set; }
        public int Cols { get; private set; }
        public int NutWidth { get; private set; }
        public int Speed { get; set; }
        private readonly int _foodNumber = Enum.GetNames(typeof(FoodType)).Length - 1;
        private readonly Random _random = new Random();

        public Game(GameLevel level)
        {
            var levelInitFuncDic = new Dictionary<GameLevel, Action>
            {
                {GameLevel.Beginner, () => Initialize(GameLevel.Beginner, 5, 17, 20, 2, Quantity.Much)},
                {GameLevel.Intermediate, () => Initialize(GameLevel.Intermediate, 7, 25, 15, 4, Quantity.Normal)},
                {GameLevel.Advanced, () => Initialize(GameLevel.Advanced, 7, 30, 15, 6, Quantity.Few)}
            };

            levelInitFuncDic[level]();
        }

        public Game(int row, int cols, int nutWidth, int speed, Quantity foods)
        {
            Initialize(GameLevel.Custom, row, cols, nutWidth, speed, foods);
        }

        protected void Initialize(GameLevel level, int row, int cols, int nutWidth, int speed, Quantity foods)
        {
            Level = level;
            Rows = row;
            Cols = cols;
            NutWidth = nutWidth;
            Speed = speed;
            FoodQuantity = foods;
        }

        public FoodType GetRandomFood()
        {
            return _random.Next((int)FoodQuantity) == 0 ? (FoodType)_random.Next(0, _foodNumber) : FoodType.Null;
        }
    }
}