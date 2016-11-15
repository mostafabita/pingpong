using System;
using System.Collections.Generic;
using System.Windows.Forms;
using PingPong.Enums;

namespace PingPong.Common
{
    public class Game
    {
        public GameLevel Level { get; private set; }
        public FoodNumber Foods { get; private set; }
        public int Rows { get; private set; }
        public int Cols { get; private set; }
        public int NutWidth { get; private set; }
        public int Speed { get; private set; }

        public Game(GameLevel level)
        {
            var levelInitFuncDic = new Dictionary<GameLevel, Action>
            {
                {GameLevel.Beginner, () => Init(GameLevel.Beginner, 5, 17, 20, 2, FoodNumber.Much)},
                {GameLevel.Intermediate, () => Init(GameLevel.Intermediate, 7, 25, 15, 6, FoodNumber.Normal)},
                {GameLevel.Advanced, () => Init(GameLevel.Advanced, 7, 30, 15, 8, FoodNumber.Few)}
            };
            levelInitFuncDic[level]();
        }

        public Game(int row, int cols, int nutWidth, int speed, FoodNumber foods)
        {
            Init(GameLevel.Custom, row, cols, nutWidth, speed, foods);
        }

        protected void Init(GameLevel level, int row, int cols, int nutWidth, int speed, FoodNumber foods)
        {
            Level = level;
            Rows = row;
            Cols = cols;
            NutWidth = nutWidth;
            Speed = speed;
            Foods = foods;
        }
    }
}