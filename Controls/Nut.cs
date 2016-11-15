using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Pong.Controls
{
    internal sealed class Nut : PictureBox
    {
        public Nut(int left, int top, int width, NutType type = NutType.Wall, FoodType food = FoodType.Null, int index = 0)
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
                {NutType.Wall, Image.FromFile(Program.AssetsPath + "\\pic\\square2.png")},
                {NutType.Earth, Image.FromFile(Program.AssetsPath + "\\pic\\square2.png")},
                {NutType.Ball, Image.FromFile(Program.AssetsPath + "\\pic\\square1.png")},
                {NutType.Nut, Image.FromFile(Program.AssetsPath + "\\pic\\square3.png")},
                {NutType.Paddle, Image.FromFile(Program.AssetsPath + "\\pic\\square3.png")}
            };
            return wallImageFuncDic[type];
        }

        private static Image GetFoodImage(FoodType food)
        {
            var foodImageFuncDic = new Dictionary<FoodType, Image>
            {
                {FoodType.Stick, Image.FromFile(Program.AssetsPath + "\\pic\\fruit1.png")},
                {FoodType.Death, Image.FromFile(Program.AssetsPath + "\\pic\\fruit2.png")},
                {FoodType.Live, Image.FromFile(Program.AssetsPath + "\\pic\\fruit3.png")},
                {FoodType.Big, Image.FromFile(Program.AssetsPath + "\\pic\\fruit4.png")},
                {FoodType.Small, Image.FromFile(Program.AssetsPath + "\\pic\\fruit5.png")},
                {FoodType.SpeedDown, Image.FromFile(Program.AssetsPath + "\\pic\\fruit6.png")},
                {FoodType.SpeedUp, Image.FromFile(Program.AssetsPath + "\\pic\\fruit7.png")}
            };
            return foodImageFuncDic[food];
        }

        public void SetFood()
        {
            Image = GetFoodImage(Food);
        }

        public delegate void FoodHitDelegate(object sender);

        public event FoodHitDelegate FoodHit;

    }
}