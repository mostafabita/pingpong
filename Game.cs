using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Microsoft.Win32.SafeHandles;
using Pong.Controls;
using Timer = System.Windows.Forms.Timer;

namespace Pong
{
    public class Game
    {
        private GameLevel Level { get; set; }
        private Quantity FoodQuantity { get; set; }
        private int Rows { get; set; }
        private int Cols { get; set; }
        private int NutWidth { get; set; }
        private int Speed { get; set; }
        private int Hearts { get; set; }
        private int PaddleFragments { get; set; }

        private readonly Dictionary<GameLevel, Action> _levelInitFuncDic;
        private const int NutsToPanelRatio = 4;
        private const int SkewMovementTilt = 1;
        private const int ScoreStep = 4;
        private const int Gap = 3;
        private int _foodNumber;
        private int _paddleFragments;
        private int _speed;
        private int _score;
        private int _scoreStep;
        private int _skewMovementTilt;
        private int _hearts;
        private bool _gameStart;
        private bool _ballStart;
        private bool _ballStick;
        private bool _roundLose;
        private bool _moveLeft;
        private bool _moveRight;
        private Form _mainForm;
        private Panel _gamePanel;
        private Timer _ballTimer;
        private Timer _movementTimer;
        private Control.ControlCollection _controls;
        private List<Control> _paddle;
        private List<Control> _nuts;
        private Nut _ball;
        private Direction _ballDirection;

        public Game(GameLevel level = GameLevel.Beginner)
        {
            _levelInitFuncDic = new Dictionary<GameLevel, Action>
            {
                {GameLevel.Beginner, () => Initialize(GameLevel.Beginner, 5, 17, 20, 2, Quantity.VeryMuch, 5, 5)},
                {GameLevel.Intermediate, () => Initialize(GameLevel.Intermediate, 7, 25, 15, 4, Quantity.Normal, 4, 5)},
                {GameLevel.Advanced, () => Initialize(GameLevel.Advanced, 7, 30, 15, 6, Quantity.Few, 3, 5)}
            };

            _levelInitFuncDic[level]();
        }

        private void Initialize(GameLevel level, int row, int cols, int nutWidth, int speed, Quantity foods, int hearts, int paddleFrags)
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

        private FoodType GetRandomFood()
        {
            var random = new Random();
            return random.Next((int)FoodQuantity) == 0 ? (FoodType)random.Next(0, _foodNumber) : FoodType.Null;
        }
        
        public void Create(Form form)
        {
            #region Initializing
            _mainForm = form;
            Log("Game Started");
            _mainForm.KeyDown += Main_KeyDown;
            _mainForm.KeyUp += Main_KeyUp;
            _gamePanel = new Panel
            {
                Name = "gamePanel",
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(NutWidth, NutWidth * 2),
                Size = new Size((Cols + 2) * NutWidth + Gap, (Rows * NutsToPanelRatio + 2) * NutWidth + Gap),
                BackColor = Color.White
            };
            _paddle = new List<Control>();
            _nuts = new List<Control>();
            _gameStart = true;
            _ballStick = _ballStart = false;
            _ballDirection = Direction.N;
            _skewMovementTilt = SkewMovementTilt;
            _scoreStep = ScoreStep + (int)Level;
            _hearts = Hearts;
            _speed = Speed;
            _score = 0;
            _paddleFragments = PaddleFragments;
            _controls = _gamePanel.Controls;
            _ballTimer?.Stop();
            _ballTimer = new Timer { Interval = 200 / _speed, Enabled = true };
            _movementTimer?.Stop();
            _movementTimer = new Timer { Interval = 40, Enabled = false };
            _ballTimer.Tick += BallTimer_Tick;
            _movementTimer.Tick += MovementTimer_Tick;
            _foodNumber = Enum.GetNames(typeof(FoodType)).Length - 1;
            _mainForm.Size = new Size(_gamePanel.Width + Gap * 2 + NutWidth * 2, _gamePanel.Height + NutWidth * 4 + Gap * 4);
            _mainForm.Controls.RemoveByKey("gamePanel");
            _mainForm.Controls.Add(_gamePanel); 
            #endregion

            #region Vertical Wall

            for (int i = 0, j = (Cols + 1) * NutWidth; i < Rows * NutsToPanelRatio + 2; i++)
            {
                _controls.Add(new Nut(0, i * NutWidth, NutWidth, NutType.Wall));
                _controls.Add(new Nut(j, i * NutWidth, NutWidth, NutType.Wall));
            }

            #endregion

            #region Horizontall Wall

            for (int i = 1, j = (Rows * NutsToPanelRatio + 1) * NutWidth; i <= Cols; i++)
            {
                _controls.Add(new Nut(i * NutWidth, 0, NutWidth, NutType.Wall));
                _controls.Add(new Nut(i * NutWidth, j, NutWidth, NutType.Earth));
            }

            #endregion

            #region Nut Table

            for (var i = 1; i <= Rows; i++)
                for (var j = 1; j <= Cols; j++)
                {
                    var nut = new Nut(j * NutWidth, i * NutWidth, NutWidth, NutType.Nut, GetRandomFood());
                    nut.FoodHit += Nut_FoodHit;
                    _nuts.Add(nut);
                }
            _controls.AddRange(_nuts.ToArray());

            #endregion

            #region Ball

            _ball = new Nut((Cols / 2 + 1) * NutWidth, (Rows * NutsToPanelRatio - 1) * NutWidth, NutWidth, NutType.Ball, FoodType.Null, _paddleFragments / 2);
            _controls.Add(_ball);

            #endregion

            #region Paddle

            for (int i = 0, j = _ball.Left - _paddleFragments / 2 * NutWidth; i < _paddleFragments; i++, j += NutWidth)
                _paddle.Add(new Nut(j, Rows * NutsToPanelRatio * NutWidth, NutWidth, NutType.Paddle, FoodType.Null, i));
            _controls.AddRange(_paddle.ToArray());
            #endregion
        }

        public void Create(Form form, GameLevel level)
        {
            _levelInitFuncDic[level]();
            Create(form);
        }
        
        private void Nut_FoodHit(object sender)
        {
            var foodHitTimer = new Timer { Interval = 300, Tag = sender, Enabled = true };
            foodHitTimer.Tick += FoodHitTimer_Tick;
        }

        private void FoodHitTimer_Tick(object sender, EventArgs e)
        {
            if (!_gameStart) return;
            var timer = (Timer)sender;
            var nut = (Nut)timer.Tag;
            timer.Stop();
            nut.Visible = true;
            nut.Index = -2;
            if (_roundLose) nut.Visible = false;
            else
            {
                var foundNut = FindNut(nut.Left, nut.Top + NutWidth);
                switch (foundNut.GetBehavior())
                {
                    case NutBehavior.Paddle:
                        #region Paddle
                        nut.Visible = false;
                        timer.Stop();
                        Award(nut.Food);
                        return;
                    #endregion
                    case NutBehavior.Earth:
                        #region Earth
                        nut.Visible = false;
                        return;
                    #endregion
                    default:
                        #region Default
                        nut.Top += NutWidth;
                        break;
                        #endregion
                }
                timer.Start();
            }
        }

        private void BallTimer_Tick(object sender, EventArgs e)
        {
            if (_gameStart && _ballStart)
            {
                Nut nextNut, nextHrNut, nextVrNut;
                NutBehavior nextNutBehavior, nextHrNutBehavior, nextVrNutBehavior;

                switch (_ballDirection)
                {
                    case Direction.N:

                        #region North

                        nextNut = FindNut(_ball.Left, _ball.Top - NutWidth);
                        nextNutBehavior = nextNut.GetBehavior();
                        if (nextNutBehavior == NutBehavior.Continue)
                            _ball.Top -= NutWidth;
                        else
                        {
                            CalculateScore(nextNut);
                            _ballDirection = Direction.S;
                        }
                        break;

                    #endregion

                    case Direction.S:

                        #region South

                        nextNut = FindNut(_ball.Left, _ball.Top + NutWidth);
                        nextNutBehavior = nextNut.GetBehavior();
                        switch (nextNutBehavior)
                        {
                            case NutBehavior.Continue:
                                _ball.Top += NutWidth;
                                break;
                            case NutBehavior.Earth:
                                LoseHeart();
                                return;
                            case NutBehavior.Paddle:
                                if (_ballStick)
                                {
                                    StickBallToPaddle();
                                    return;
                                }
                                if (nextNut.Index > _ball.Index)
                                    _ballDirection = Direction.NE;
                                else if (nextNut.Index < _ball.Index)
                                    _ballDirection = Direction.NW;
                                else _ballDirection = Direction.N;
                                _ball.Index = nextNut.Index;
                                break;
                            default:
                                CalculateScore(nextNut);
                                _ballDirection = Direction.N;
                                break;

                        }
                        break;

                    #endregion

                    case Direction.NE:

                        #region North East

                        nextNut = FindNut(_ball.Left + NutWidth * _skewMovementTilt, _ball.Top - NutWidth);
                        nextNutBehavior = nextNut.GetBehavior();
                        nextHrNut = FindNut(_ball.Left + NutWidth, _ball.Top);
                        nextHrNutBehavior = nextHrNut.GetBehavior();
                        nextVrNut = FindNut(_ball.Left + (_skewMovementTilt == 1 ? 0 : NutWidth), _ball.Top - NutWidth);
                        nextVrNutBehavior = nextVrNut.GetBehavior();

                        if (nextNutBehavior == NutBehavior.Continue && nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue)
                            _ball.Location = new Point(_ball.Left + NutWidth * _skewMovementTilt, _ball.Top - NutWidth);
                        else
                        {
                            if (nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue && nextNutBehavior != NutBehavior.Continue)
                            {
                                if (_skewMovementTilt == 1)
                                    _ballDirection = Direction.SW;
                                else
                                {
                                    _ball.Location = new Point(_ball.Left + NutWidth, _ball.Top - NutWidth);
                                    _ballDirection = Direction.NW;
                                }
                                CalculateScore(nextNut);
                            }
                            else if (nextVrNutBehavior != NutBehavior.Continue && nextHrNutBehavior != NutBehavior.Continue)
                            {
                                if (_skewMovementTilt == 1)
                                {
                                    CalculateScore(nextVrNut);
                                    _ballDirection = Direction.SW;
                                }
                                else
                                    _ballDirection = Direction.NW;

                                CalculateScore(nextHrNut);
                            }
                            else if (nextVrNutBehavior != NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue)
                            {
                                CalculateScore(nextVrNut);
                                _ballDirection = Direction.SE;
                            }
                            else if (nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior != NutBehavior.Continue)
                            {
                                CalculateScore(nextHrNut);
                                _ballDirection = Direction.NW;
                            }
                        }
                        break;

                    #endregion

                    case Direction.NW:

                        #region Noth West

                        nextNut = FindNut(_ball.Left - NutWidth * _skewMovementTilt, _ball.Top - NutWidth);
                        nextNutBehavior = nextNut.GetBehavior();
                        nextHrNut = FindNut(_ball.Left - NutWidth, _ball.Top);
                        nextHrNutBehavior = nextHrNut.GetBehavior();
                        nextVrNut = FindNut(_ball.Left - (_skewMovementTilt == 1 ? 0 : NutWidth), _ball.Top - NutWidth);
                        nextVrNutBehavior = nextVrNut.GetBehavior();

                        if (nextNutBehavior == NutBehavior.Continue && nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue)
                            _ball.Location =
                                new Point(_ball.Left - NutWidth * _skewMovementTilt,
                                    _ball.Top - NutWidth);
                        else
                        {
                            if (nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue && nextNutBehavior != NutBehavior.Continue)
                            {
                                if (_skewMovementTilt == 1)

                                    _ballDirection = Direction.SE;
                                else
                                {
                                    _ball.Location =
                                        new Point(_ball.Left - NutWidth,
                                            _ball.Top - NutWidth);
                                    _ballDirection = Direction.NE;
                                }
                                CalculateScore(nextNut);
                            }
                            else if (nextVrNutBehavior != NutBehavior.Continue && nextHrNutBehavior != NutBehavior.Continue)
                            {
                                if (_skewMovementTilt == 1)
                                {
                                    CalculateScore(nextVrNut);
                                    _ballDirection = Direction.SE;
                                }
                                else
                                    _ballDirection = Direction.NE;

                                CalculateScore(nextHrNut);
                            }
                            else if (nextVrNutBehavior != NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue)
                            {
                                CalculateScore(nextVrNut);
                                _ballDirection = Direction.SW;
                            }
                            else if (nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior != NutBehavior.Continue)
                            {
                                CalculateScore(nextHrNut);
                                _ballDirection = Direction.NE;
                            }
                        }
                        break;

                    #endregion

                    case Direction.SE:

                        #region South East

                        nextNut = FindNut(_ball.Left + NutWidth, _ball.Top + NutWidth);
                        nextNutBehavior = nextNut.GetBehavior();
                        nextHrNut = FindNut(_ball.Left + NutWidth, _ball.Top);
                        nextHrNutBehavior = nextHrNut.GetBehavior();
                        nextVrNut = FindNut(_ball.Left, _ball.Top + NutWidth);
                        nextVrNutBehavior = nextVrNut.GetBehavior();

                        if (nextNutBehavior == NutBehavior.Earth || nextVrNutBehavior == NutBehavior.Earth || nextHrNutBehavior == NutBehavior.Earth)
                        {
                            LoseHeart();
                            return;
                        }
                        if (nextNutBehavior == NutBehavior.Continue && nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue)
                            _ball.Location =
                                new Point(_ball.Left + NutWidth,
                                    _ball.Top + NutWidth);
                        else
                        {
                            _skewMovementTilt = 1;
                            if (nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue && nextNutBehavior != NutBehavior.Continue)
                            {
                                _skewMovementTilt = 2;
                                CalculateScore(nextNut);
                                _ballDirection = Direction.NW;
                            }
                            else if (nextVrNutBehavior != NutBehavior.Continue && nextHrNutBehavior != NutBehavior.Continue)
                            {
                                if (nextVrNut.Type == NutType.Paddle && _ballStick)
                                {
                                    StickBallToPaddle();
                                    return;
                                }
                                CalculateScore(nextHrNut);
                                CalculateScore(nextVrNut);
                                _ballDirection = Direction.NW;
                            }
                            else if (nextVrNutBehavior != NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue)
                            {
                                if (nextVrNut.Type == NutType.Paddle && _ballStick)
                                {
                                    StickBallToPaddle();
                                    return;
                                }
                                CalculateScore(nextVrNut);
                                _ballDirection = Direction.NE;
                            }
                            else if (nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior != NutBehavior.Continue)
                            {
                                CalculateScore(nextHrNut);
                                _ballDirection = Direction.SW;
                            }
                        }
                        break;

                    #endregion

                    case Direction.SW:

                        #region South West

                        nextNut = FindNut(_ball.Left - NutWidth, _ball.Top + NutWidth);
                        nextNutBehavior = nextNut.GetBehavior();
                        nextHrNut = FindNut(_ball.Left - NutWidth, _ball.Top);
                        nextHrNutBehavior = nextHrNut.GetBehavior();
                        nextVrNut = FindNut(_ball.Left, _ball.Top + NutWidth);
                        nextVrNutBehavior = nextVrNut.GetBehavior();

                        if (nextNutBehavior == NutBehavior.Earth || nextVrNutBehavior == NutBehavior.Earth || nextHrNutBehavior == NutBehavior.Earth)
                        {
                            LoseHeart();
                            return;
                        }
                        if (nextNutBehavior == NutBehavior.Continue && nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue)
                            _ball.Location =
                                new Point(_ball.Left - NutWidth,
                                    _ball.Top + NutWidth);
                        else
                        {
                            _skewMovementTilt = 1;
                            if (nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue && nextNutBehavior != NutBehavior.Continue)
                            {
                                _skewMovementTilt = 2;
                                CalculateScore(nextNut);
                                _ballDirection = Direction.NE;
                            }
                            else if (nextVrNutBehavior != NutBehavior.Continue && nextHrNutBehavior != NutBehavior.Continue)
                            {
                                if (nextVrNut.Type == NutType.Paddle && _ballStick)
                                {
                                    StickBallToPaddle();
                                    return;
                                }
                                CalculateScore(nextHrNut);
                                CalculateScore(nextVrNut);
                                _ballDirection = Direction.NE;
                            }
                            else if (nextVrNutBehavior != NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue)
                            {
                                if (nextVrNut.Type == NutType.Paddle && _ballStick)
                                {
                                    StickBallToPaddle();
                                    return;
                                }
                                CalculateScore(nextVrNut);
                                _ballDirection = Direction.NW;
                            }
                            else if (nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior != NutBehavior.Continue)
                            {
                                CalculateScore(nextHrNut);
                                _ballDirection = Direction.SE;
                            }
                        }
                        break;

                        #endregion
                }
            }
            _ballTimer.Start();
        }

        private void MovementTimer_Tick(object sender, EventArgs e)
        {
            MovePaddle();
        }

        private void Award(FoodType foodType)
        {
            switch (foodType)
            {
                case FoodType.Grow:

                    #region Big

                    Log("Paddle Growed");
                    if (_paddleFragments < Cols - 2)
                    {
                        var newNut = new Nut();
                        if (_paddle[_paddleFragments - 1].Left + NutWidth * 2 < (Cols + 2) * NutWidth)
                        {
                            newNut = new Nut(_paddle[_paddleFragments - 1].Left + NutWidth, _paddle[0].Top, NutWidth, NutType.Paddle, FoodType.Null, _paddleFragments);
                            _paddle.Add(newNut);
                        }
                        else if (_paddle[0].Left > NutWidth)
                        {
                            newNut = new Nut(_paddle[0].Left - NutWidth, _paddle[0].Top, NutWidth, NutType.Paddle, FoodType.Null, ((Nut)_paddle[0]).Index - 1);
                            _paddle.Insert(0, newNut);
                        }
                        _controls.Add(newNut);
                        _paddleFragments++;
                    }
                    break;

                #endregion

                case FoodType.Shrink:

                    #region Small

                    Log("Paddle Shrinked");
                    if (--_paddleFragments <= 0)
                    {
                        _hearts = 0;
                        LoseHeart();
                    }
                    else
                        _paddle[_paddleFragments].Dispose();
                    break;

                #endregion

                case FoodType.Heart:

                    #region Live

                    Log("Live Increased");
                    _hearts++;
                    break;

                #endregion

                case FoodType.Death:

                    #region Death

                    Log("Live Decreased");
                    if (--_hearts < 0) LoseHeart();
                    break;

                #endregion

                case FoodType.Stick:

                    #region Stick

                    Log("Sticky Ball");
                    _ballStick = true;
                    break;

                #endregion

                case FoodType.Faster:

                    #region Speed Up

                    Log("Speed Increased");
                    _ballTimer.Interval = 200 / ++_speed;
                    break;

                #endregion

                case FoodType.Slower:

                    #region Speed Down
                    if (_speed > 0)
                    {
                        Log("Speed Decreased");
                        _ballTimer.Interval = 200 / _speed--;
                    }
                    break;

                    #endregion
            }
        }

        private void StickBallToPaddle()
        {
            _ballStart = false;
            _ballDirection = Direction.N;
            _ball.Index = _paddleFragments / 2;
        }

        private void MovePaddle()
        {
            if (!_gameStart) return;

            if (_moveLeft)
            {
                #region Left
                if (_paddle[0].Left > NutWidth)
                {
                    foreach (var padd in _paddle) padd.Left -= NutWidth;
                    if (!_ballStart) _ball.Left -= NutWidth;
                }
                #endregion
            }
            if (_moveRight)
            {
                #region Right
                if (_paddle[_paddleFragments - 1].Left + NutWidth * 2 < (Cols + 2) * NutWidth)
                {
                    foreach (var padd in _paddle) padd.Left += NutWidth;
                    if (!_ballStart) _ball.Left += NutWidth;
                }
                #endregion
            }
        }

        private void LoseHeart()
        {
            _gameStart = _ballStart = false;
            _roundLose = true;
            _ballDirection = Direction.N;
            if (_hearts-- > 0)
            {
                #region Lose Heart
                Thread.Sleep(1500);
                RealignPaddle();
                _gameStart = true;
                #endregion
            }
            else
            {
                #region Game Over
                MessageBox.Show("Game Over", "Pong", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (MessageBox.Show("Do you want to restart game ?", "Pong", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    Create(_mainForm);
                #endregion
            }
        }

        private void CalculateScore(Nut nut)
        {
            if (nut.Type != NutType.Nut) return;
            nut.Visible = false;
            _score += _scoreStep;
            if (!_nuts.Any(o => ((Nut)o).Type == NutType.Nut && o.Visible)) MessageBox.Show("YOU WIN");
        }

        private Nut FindNut(int x, int y)
        {
            var nut = _controls.Cast<Nut>().FirstOrDefault(o => o.Location == new Point(x, y));
            return nut ?? new Nut();
        }
        
        private void RealignPaddle()
        {
            _ball.Location = new Point((Cols / 2 + 1) * NutWidth, (Rows * NutsToPanelRatio - 1) * NutWidth);
            _ball.Index = _paddleFragments / 2;
            var paddleStartPosition = _ball.Left - _paddleFragments / 2 * NutWidth;
            foreach (var padd in _paddle)
            {
                padd.Left = paddleStartPosition;
                paddleStartPosition += NutWidth;
            }
        }

        private void Main_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left) _moveLeft = false;
            if (e.KeyCode == Keys.Right) _moveRight = false;
            if (_moveLeft || _moveRight) return;
            _mainForm.Tag = Keys.None;
            _movementTimer.Stop();
        }

        private void Main_KeyDown(object sender, KeyEventArgs e)
        {
            if (_mainForm.Tag != null && e.KeyData == (Keys)_mainForm.Tag) return;
            _mainForm.Tag = e.KeyData;
            switch (e.KeyData)
            {
                case Keys.Escape:

                    #region Escape

                    Application.Exit();
                    break;

                #endregion

                case Keys.Left:

                    #region Left

                    _moveLeft = true;
                    MovePaddle();
                    _movementTimer.Start();
                    break;

                #endregion

                case Keys.Right:

                    #region Right
                    _moveRight = true;
                    MovePaddle();
                    _movementTimer.Start();
                    break;

                #endregion

                case Keys.Space:

                    #region Space
                    _gameStart = _ballStart = true;
                    _ballStick = _roundLose = false;
                    break;

                #endregion

                case Keys.P:
                    #region Play & Pause
                    _gameStart = !_gameStart;
                    break;
                    #endregion
            }
        }

        private void Log(string content)
        {
            _mainForm.Text = content;
        }
    }
}