using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Pong.Controls;
using Timer = System.Windows.Forms.Timer;

namespace Pong
{
    public partial class Main : Form
    {
        private int _currentPaddleFrag;
        private int _speed;
        private int _score;
        private int _scoreStep;
        private int _movementStep;
        private int _hearts;
        private bool _gameStart;
        private bool _ballStart;
        private bool _ballStick;
        private bool _roundLose;
        private bool _moveLeft;
        private bool _moveRight;
        private Game _game;
        private Panel _gamePanel;
        private Timer _ballTimer;
        private Timer _movementTimer;
        private Control.ControlCollection _controls;
        private List<Control> _paddle;
        private List<Control> _nuts;
        private Nut _ball;
        private Direction _ballDir = Direction.N;

        public Main()
        {
            InitializeComponent();
            InitializeGame();
        }

        public void InitializeGame(GameLevel gameLevel = GameLevel.Beginner)
        {
            Log("Game Start");
            KeyDown += Main_KeyDown;
            KeyUp += Main_KeyUp;
            _game = new Game(gameLevel);
            _gamePanel = new Panel
            {
                Name = "gamePanel",
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(_game.NutWidth, _game.NutWidth * 2),
                Size = new Size((_game.Cols + 2) * _game.NutWidth + _game.Gap, (_game.Rows * _game.NutsToPanelRatio + 2) * _game.NutWidth + _game.Gap),
                BackColor = Color.White
            };
            _paddle = new List<Control>();
            _nuts = new List<Control>();
            _gameStart = true;
            _ballStick = _ballStart = false;
            _movementStep = _game.MovementStep;
            _scoreStep = (short)(_game.ScoreStep + (short)gameLevel);
            _hearts = _game.Hearts;
            _speed = _game.Speed;
            _currentPaddleFrag = _game.PaddleFragments;
            _controls = _gamePanel.Controls;
            _ballTimer?.Stop();
            _ballTimer = new Timer { Interval = 200 / _speed, Enabled = true };
            _movementTimer?.Stop();
            _movementTimer = new Timer { Interval = 40, Enabled = false };
            _ballTimer.Tick += _ballTimer_Tick;
            _movementTimer.Tick += _movementTimer_Tick;
            Size = new Size(_gamePanel.Width + _game.Gap * 2 + _game.NutWidth * 2, _gamePanel.Height + _game.NutWidth * 4 + _game.Gap * 4);
            Controls.RemoveByKey("gamePanel");
            Controls.Add(_gamePanel);

            #region Vertical Wall

            for (int i = 0, j = (_game.Cols + 1) * _game.NutWidth; i < _game.Rows * _game.NutsToPanelRatio + 2; i++)
            {
                _controls.Add(new Nut(0, i * _game.NutWidth, _game.NutWidth, NutType.Wall));
                _controls.Add(new Nut(j, i * _game.NutWidth, _game.NutWidth, NutType.Wall));
            }

            #endregion

            #region Horizontall Wall

            for (int i = 1, j = (_game.Rows * _game.NutsToPanelRatio + 1) * _game.NutWidth; i <= _game.Cols; i++)
            {
                _controls.Add(new Nut(i * _game.NutWidth, 0, _game.NutWidth, NutType.Wall));
                _controls.Add(new Nut(i * _game.NutWidth, j, _game.NutWidth, NutType.Earth));
            }

            #endregion

            #region Nut Table

            for (var i = 1; i <= _game.Rows; i++)
                for (var j = 1; j <= _game.Cols; j++)
                {
                    var nut = new Nut(j * _game.NutWidth, i * _game.NutWidth, _game.NutWidth, NutType.Nut, FoodType.Slower);
                    nut.FoodHit += Nut_FoodHit;
                    _nuts.Add(nut);
                }
            _controls.AddRange(_nuts.ToArray());

            #endregion

            #region Ball

            _ball = new Nut((_game.Cols / 2 + 1) * _game.NutWidth, (_game.Rows * _game.NutsToPanelRatio - 1) * _game.NutWidth, _game.NutWidth, NutType.Ball, FoodType.Null, _currentPaddleFrag / 2);
            _controls.Add(_ball);

            #endregion

            #region Paddle

            for (int i = 0, j = _ball.Left - _currentPaddleFrag / 2 * _game.NutWidth; i < _currentPaddleFrag; i++, j += _game.NutWidth)
                _paddle.Add(new Nut(j, _game.Rows * _game.NutsToPanelRatio * _game.NutWidth, _game.NutWidth, NutType.Paddle, FoodType.Null, i));
            _controls.AddRange(_paddle.ToArray());
            #endregion
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
                var foundNut = FindNut(nut.Left, nut.Top + _game.NutWidth);
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
                        nut.Top += _game.NutWidth;
                        break;
                        #endregion
                }
                timer.Start();
            }
        }

        private void _ballTimer_Tick(object sender, EventArgs e)
        {
            if (_gameStart && _ballStart)
            {
                Nut nextNut, nextHrNut, nextVrNut;
                NutBehavior nextNutBehavior, nextHrNutBehavior, nextVrNutBehavior;

                switch (_ballDir)
                {
                    case Direction.N:

                        #region North

                        nextNut = FindNut(_ball.Left, _ball.Top - _game.NutWidth);
                        nextNutBehavior = nextNut.GetBehavior();
                        if (nextNutBehavior == NutBehavior.Continue)
                            _ball.Top -= _game.NutWidth;
                        else
                        {
                            CalculateScore(nextNut);
                            _ballDir = Direction.S;
                        }
                        break;

                    #endregion

                    case Direction.S:

                        #region South

                        nextNut = FindNut(_ball.Left, _ball.Top + _game.NutWidth);
                        nextNutBehavior = nextNut.GetBehavior();
                        switch (nextNutBehavior)
                        {
                            case NutBehavior.Continue:
                                _ball.Top += _game.NutWidth;
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
                                    _ballDir = Direction.NE;
                                else if (nextNut.Index < _ball.Index)
                                    _ballDir = Direction.NW;
                                else _ballDir = Direction.N;
                                _ball.Index = nextNut.Index;
                                break;
                            default:
                                CalculateScore(nextNut);
                                _ballDir = Direction.N;
                                break;

                        }
                        break;

                    #endregion

                    case Direction.NE:

                        #region North East

                        nextNut = FindNut(_ball.Left + _game.NutWidth * _movementStep, _ball.Top - _game.NutWidth);
                        nextNutBehavior = nextNut.GetBehavior();
                        nextHrNut = FindNut(_ball.Left + _game.NutWidth, _ball.Top);
                        nextHrNutBehavior = nextHrNut.GetBehavior();
                        nextVrNut = FindNut(_ball.Left + (_movementStep == 1 ? 0 : _game.NutWidth), _ball.Top - _game.NutWidth);
                        nextVrNutBehavior = nextVrNut.GetBehavior();

                        if (nextNutBehavior == NutBehavior.Continue && nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue)
                            _ball.Location = new Point(_ball.Left + _game.NutWidth * _movementStep, _ball.Top - _game.NutWidth);
                        else
                        {
                            if (nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue && nextNutBehavior != NutBehavior.Continue)
                            {
                                if (_movementStep == 1)
                                    _ballDir = Direction.SW;
                                else
                                {
                                    _ball.Location = new Point(_ball.Left + _game.NutWidth, _ball.Top - _game.NutWidth);
                                    _ballDir = Direction.NW;
                                }
                                CalculateScore(nextNut);
                            }
                            else if (nextVrNutBehavior != NutBehavior.Continue && nextHrNutBehavior != NutBehavior.Continue)
                            {
                                if (_movementStep == 1)
                                {
                                    CalculateScore(nextVrNut);
                                    _ballDir = Direction.SW;
                                }
                                else
                                    _ballDir = Direction.NW;

                                CalculateScore(nextHrNut);
                            }
                            else if (nextVrNutBehavior != NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue)
                            {
                                CalculateScore(nextVrNut);
                                _ballDir = Direction.SE;
                            }
                            else if (nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior != NutBehavior.Continue)
                            {
                                CalculateScore(nextHrNut);
                                _ballDir = Direction.NW;
                            }
                        }
                        break;

                    #endregion

                    case Direction.NW:

                        #region Noth West

                        nextNut = FindNut(_ball.Left - _game.NutWidth * _movementStep, _ball.Top - _game.NutWidth);
                        nextNutBehavior = nextNut.GetBehavior();
                        nextHrNut = FindNut(_ball.Left - _game.NutWidth, _ball.Top);
                        nextHrNutBehavior = nextHrNut.GetBehavior();
                        nextVrNut = FindNut(_ball.Left - (_movementStep == 1 ? 0 : _game.NutWidth), _ball.Top - _game.NutWidth);
                        nextVrNutBehavior = nextVrNut.GetBehavior();

                        if (nextNutBehavior == NutBehavior.Continue && nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue)
                            _ball.Location =
                                new Point(_ball.Left - _game.NutWidth * _movementStep,
                                    _ball.Top - _game.NutWidth);
                        else
                        {
                            if (nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue && nextNutBehavior != NutBehavior.Continue)
                            {
                                if (_movementStep == 1)

                                    _ballDir = Direction.SE;
                                else
                                {
                                    _ball.Location =
                                        new Point(_ball.Left - _game.NutWidth,
                                            _ball.Top - _game.NutWidth);
                                    _ballDir = Direction.NE;
                                }
                                CalculateScore(nextNut);
                            }
                            else if (nextVrNutBehavior != NutBehavior.Continue && nextHrNutBehavior != NutBehavior.Continue)
                            {
                                if (_movementStep == 1)
                                {
                                    CalculateScore(nextVrNut);
                                    _ballDir = Direction.SE;
                                }
                                else
                                    _ballDir = Direction.NE;

                                CalculateScore(nextHrNut);
                            }
                            else if (nextVrNutBehavior != NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue)
                            {
                                CalculateScore(nextVrNut);
                                _ballDir = Direction.SW;
                            }
                            else if (nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior != NutBehavior.Continue)
                            {
                                CalculateScore(nextHrNut);
                                _ballDir = Direction.NE;
                            }
                        }
                        break;

                    #endregion

                    case Direction.SE:

                        #region South East

                        nextNut = FindNut(_ball.Left + _game.NutWidth, _ball.Top + _game.NutWidth);
                        nextNutBehavior = nextNut.GetBehavior();
                        nextHrNut = FindNut(_ball.Left + _game.NutWidth, _ball.Top);
                        nextHrNutBehavior = nextHrNut.GetBehavior();
                        nextVrNut = FindNut(_ball.Left, _ball.Top + _game.NutWidth);
                        nextVrNutBehavior = nextVrNut.GetBehavior();

                        if (nextNutBehavior == NutBehavior.Earth || nextVrNutBehavior == NutBehavior.Earth || nextHrNutBehavior == NutBehavior.Earth)
                        {
                            LoseHeart();
                            return;
                        }
                        if (nextNutBehavior == NutBehavior.Continue && nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue)
                            _ball.Location =
                                new Point(_ball.Left + _game.NutWidth,
                                    _ball.Top + _game.NutWidth);
                        else
                        {
                            _movementStep = 1;
                            if (nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue && nextNutBehavior != NutBehavior.Continue)
                            {
                                _movementStep = 2;
                                CalculateScore(nextNut);
                                _ballDir = Direction.NW;
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
                                _ballDir = Direction.NW;
                            }
                            else if (nextVrNutBehavior != NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue)
                            {
                                if (nextVrNut.Type == NutType.Paddle && _ballStick)
                                {
                                    StickBallToPaddle();
                                    return;
                                }
                                CalculateScore(nextVrNut);
                                _ballDir = Direction.NE;
                            }
                            else if (nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior != NutBehavior.Continue)
                            {
                                CalculateScore(nextHrNut);
                                _ballDir = Direction.SW;
                            }
                        }
                        break;

                    #endregion

                    case Direction.SW:

                        #region South West

                        nextNut = FindNut(_ball.Left - _game.NutWidth, _ball.Top + _game.NutWidth);
                        nextNutBehavior = nextNut.GetBehavior();
                        nextHrNut = FindNut(_ball.Left - _game.NutWidth, _ball.Top);
                        nextHrNutBehavior = nextHrNut.GetBehavior();
                        nextVrNut = FindNut(_ball.Left, _ball.Top + _game.NutWidth);
                        nextVrNutBehavior = nextVrNut.GetBehavior();

                        if (nextNutBehavior == NutBehavior.Earth || nextVrNutBehavior == NutBehavior.Earth || nextHrNutBehavior == NutBehavior.Earth)
                        {
                            LoseHeart();
                            return;
                        }
                        if (nextNutBehavior == NutBehavior.Continue && nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue)
                            _ball.Location =
                                new Point(_ball.Left - _game.NutWidth,
                                    _ball.Top + _game.NutWidth);
                        else
                        {
                            _movementStep = 1;
                            if (nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue && nextNutBehavior != NutBehavior.Continue)
                            {
                                _movementStep = 2;
                                CalculateScore(nextNut);
                                _ballDir = Direction.NE;
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
                                _ballDir = Direction.NE;
                            }
                            else if (nextVrNutBehavior != NutBehavior.Continue && nextHrNutBehavior == NutBehavior.Continue)
                            {
                                if (nextVrNut.Type == NutType.Paddle && _ballStick)
                                {
                                    StickBallToPaddle();
                                    return;
                                }
                                CalculateScore(nextVrNut);
                                _ballDir = Direction.NW;
                            }
                            else if (nextVrNutBehavior == NutBehavior.Continue && nextHrNutBehavior != NutBehavior.Continue)
                            {
                                CalculateScore(nextHrNut);
                                _ballDir = Direction.SE;
                            }
                        }
                        break;

                        #endregion
                }
            }
            _ballTimer.Start();
        }

        private void _movementTimer_Tick(object sender, EventArgs e)
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
                    if (_currentPaddleFrag < _game.Cols - 2)
                    {
                        var newNut = new Nut();
                        if (_paddle[_currentPaddleFrag - 1].Left + _game.NutWidth * 2 < (_game.Cols + 2) * _game.NutWidth)
                        {
                            newNut = new Nut(_paddle[_currentPaddleFrag - 1].Left + _game.NutWidth, _paddle[0].Top, _game.NutWidth, NutType.Paddle, FoodType.Null, _currentPaddleFrag);
                            _paddle.Add(newNut);
                        }
                        else if (_paddle[0].Left > _game.NutWidth)
                        {
                            newNut = new Nut(_paddle[0].Left - _game.NutWidth, _paddle[0].Top, _game.NutWidth, NutType.Paddle, FoodType.Null, ((Nut)_paddle[0]).Index - 1);
                            _paddle.Insert(0, newNut);
                        }
                        _controls.Add(newNut);
                        _currentPaddleFrag++;
                    }
                    break;

                #endregion

                case FoodType.Shrink:

                    #region Small

                    Log("Paddle Shrinked");
                    if (--_currentPaddleFrag <= 0)
                    {
                        _hearts = 0;
                        LoseHeart();
                    }
                    else
                        _paddle[_currentPaddleFrag].Dispose();
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
            _ballDir = Direction.N;
            _ball.Index = _currentPaddleFrag / 2;
        }

        private void MovePaddle()
        {
            if (!_gameStart) return;

            if (_moveLeft)
            {
                #region Left
                if (_paddle[0].Left > _game.NutWidth)
                {
                    foreach (var padd in _paddle) padd.Left -= _game.NutWidth;
                    if (!_ballStart) _ball.Left -= _game.NutWidth;
                }
                #endregion
            }
            if (_moveRight)
            {
                #region Right
                if (_paddle[_currentPaddleFrag - 1].Left + _game.NutWidth * 2 < (_game.Cols + 2) * _game.NutWidth)
                {
                    foreach (var padd in _paddle) padd.Left += _game.NutWidth;
                    if (!_ballStart) _ball.Left += _game.NutWidth;
                }
                #endregion
            }
        }

        private void LoseHeart()
        {
            _gameStart = _ballStart = false;
            _roundLose = true;
            _ballDir = Direction.N;
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
                    InitializeGame(_game.Level);
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

        private void Log(string content)
        {
            Text = content;
        }

        private void RealignPaddle()
        {
            _ball.Location = new Point((_game.Cols / 2 + 1) * _game.NutWidth, (_game.Rows * _game.NutsToPanelRatio - 1) * _game.NutWidth);
            _ball.Index = _currentPaddleFrag / 2;
            var paddleStartPosition = _ball.Left - _currentPaddleFrag / 2 * _game.NutWidth;
            foreach (var padd in _paddle)
            {
                padd.Left = paddleStartPosition;
                paddleStartPosition += _game.NutWidth;
            }
        }

        private void Main_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left) _moveLeft = false;
            if (e.KeyCode == Keys.Right) _moveRight = false;
            if (_moveLeft || _moveRight) return;
            Tag = Keys.None;
            _movementTimer.Stop();
        }

        private void Main_KeyDown(object sender, KeyEventArgs e)
        {
            if (Tag != null && e.KeyData == (Keys)Tag) return;
            Tag = e.KeyData;
            switch (e.KeyData)
            {
                case Keys.Escape:

                    #region Escape

                    Close();
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

        private void newGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InitializeGame(_game.Level);
        }

        private void changeLevelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (((ToolStripMenuItem)sender).Text)
            {
                case "Beginner":
                    InitializeGame();
                    break;
                case "Intermediate":
                    InitializeGame(GameLevel.Intermediate);
                    break;
                case "Advanced":
                    InitializeGame(GameLevel.Advanced);
                    break;
            }
        }
    }
}