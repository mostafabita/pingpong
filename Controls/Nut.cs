using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Pong.Controls
{
    internal sealed class Nut : PictureBox
    {
        public Nut(int left = 0, int top = 0, int width = 0, NutType type = NutType.Null, FoodType food = FoodType.Null, int index = 0)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = width;
            Type = type;
            Index = index;
            Food = food;
            Image = GetNutImage(type);
            BackColor = Color.White;
            SizeMode = PictureBoxSizeMode.Zoom;
            BorderStyle = BorderStyle.None;
            InitialTimer();
        }

        public Timer Timer;

        public int Index { get; set; }

        public NutType Type { get; set; }

        public FoodType Food { get; set; }

        private void InitialTimer()
        {
            Timer = new Timer { Interval = 100, Enabled = true };
            Timer.Tick += timer_Tick;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (Visible || Food == FoodType.Null) return;
            Timer.Dispose();
            SetFood();
            FoodHit?.Invoke(this);
        }

        private static Image GetNutImage(NutType type)
        {
            var wallImageFuncDic = new Dictionary<NutType, Image>
            {
                {NutType.Wall, Image.FromFile(Program.AssetsPath + "\\pic\\nut2.png")},
                {NutType.Earth, Image.FromFile(Program.AssetsPath + "\\pic\\nut2.png")},
                {NutType.Ball, Image.FromFile(Program.AssetsPath + "\\pic\\nut1.png")},
                {NutType.Nut, Image.FromFile(Program.AssetsPath + "\\pic\\nut3.png")},
                {NutType.Paddle, Image.FromFile(Program.AssetsPath + "\\pic\\nut3.png")},
                {NutType.Null, null}
            };
            return wallImageFuncDic[type];
        }

        private static Image GetFoodImage(FoodType food)
        {
            var foodImageFuncDic = new Dictionary<FoodType, Image>
            {
                {FoodType.Stick, Image.FromFile(Program.AssetsPath + "\\pic\\award6.png")},
                {FoodType.Death, Image.FromFile(Program.AssetsPath + "\\pic\\award2.png")},
                {FoodType.Heart, Image.FromFile(Program.AssetsPath + "\\pic\\award8.png")},
                {FoodType.Grow, Image.FromFile(Program.AssetsPath + "\\pic\\award5.png")},
                {FoodType.Shrink, Image.FromFile(Program.AssetsPath + "\\pic\\award3.png")},
                {FoodType.Slower, Image.FromFile(Program.AssetsPath + "\\pic\\award7.png")},
                {FoodType.Faster, Image.FromFile(Program.AssetsPath + "\\pic\\award1.png")}
            };
            return foodImageFuncDic[food];
        }

        public void SetFood()
        {
            Image = GetFoodImage(Food);
        }

        public delegate void FoodHitDelegate(object sender);

        public event FoodHitDelegate FoodHit;

        public NutBehavior GetBehavior()
        {
            if (Type == NutType.Null || !Visible) return NutBehavior.Continue;
            if (Type == NutType.Earth) return NutBehavior.Earth;
            if (Type == NutType.Paddle) return NutBehavior.Paddle;
            if (Type != NutType.Earth && Index != -2) return NutBehavior.Others;
            return NutBehavior.Continue;
        }
    }
}