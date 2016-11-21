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
        public int Gap { get; private set; } = 3;
        public int Hearts { get; private set; }
        public int ScoreStep { get; private set; } = 4;
        public int MovementStep { get; private set; } = 1;
        public int NutsToPanelRatio { get; private set; } = 4;
        public int PaddleFragments { get; private set; }
        private int FoodNumber { get; } = Enum.GetNames(typeof(FoodType)).Length - 1;

        public Game(GameLevel level)
        {
            var levelInitFuncDic = new Dictionary<GameLevel, Action>
            {
                {GameLevel.Beginner, () => Initialize(GameLevel.Beginner, 5, 17, 20, 2, Quantity.VeryMuch, 5, 5)},
                {GameLevel.Intermediate, () => Initialize(GameLevel.Intermediate, 7, 25, 15, 4, Quantity.Normal, 4, 5)},
                {GameLevel.Advanced, () => Initialize(GameLevel.Advanced, 7, 30, 15, 6, Quantity.Few, 3, 5)}
            };

            levelInitFuncDic[level]();
        }
        public Game(int row, int cols, int nutWidth, int speed, Quantity foods, int hearts, int paddleFrags)
        {
            Initialize(GameLevel.Custom, row, cols, nutWidth, speed, foods, hearts, paddleFrags);
        }
        protected void Initialize(GameLevel level, int row, int cols, int nutWidth, int speed, Quantity foods, int hearts, int paddleFrags)
        {
            Level = level;
            Rows = row;
            Cols = cols;
            NutWidth = nutWidth;
            Speed = speed;
            FoodQuantity = foods;
            Hearts = hearts;
            PaddleFragments = paddleFrags;
        }
        public FoodType GetRandomFood()
        {
            var random = new Random();
            return random.Next((int)FoodQuantity) == 0 ? (FoodType)random.Next(0, FoodNumber) : FoodType.Null;
        }
    }
}