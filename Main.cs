using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Pong.Controls;
using Timer = System.Windows.Forms.Timer;

namespace Pong
{
    public partial class Main : Form
    {
        private const short Gap = 3;
        private const short Hearts = 5;
        private const short NutsToPanelRatio = 4;
        private const short PaddleFragments = 5;
        private const short ScoreStep = 5;
        private const short MovementStep = 1;
        private short _currentPaddleFrag;
        private short _score;
        private short _movementStep;
        private short _hearts;
        private int _ballIndex;
        private int _paddleIndex;
        private bool _gameStart;
        private bool _moveLeft;
        private bool _moveRight;
        private bool _ballStart;
        private bool _ballStick;
        private Direction _ballDirection = Direction.N;
        private Keys _previousKey;
        private Game _game;
        private Panel _gamePanel;
        private Control.ControlCollection _controls;
        private Timer _ballTimer;
        private Timer _movementTimer;

        public Main()
        {
            InitializeComponent();
            InitializeGame();
        }

        public void InitializeGame(GameLevel gameLevel = GameLevel.Beginner)
        {
            _game = new Game(gameLevel);
            _gamePanel = new Panel
            {
                Name = "gamePanel",
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(_game.NutWidth, _game.NutWidth * 2),
                Size = new Size((_game.Cols + 2) * _game.NutWidth + Gap, (_game.Rows * NutsToPanelRatio + 2) * _game.NutWidth + Gap),
                BackColor = Color.White
            };
            _controls = _gamePanel.Controls;
            _ballTimer = new Timer { Interval = 40 };
            _movementTimer = new Timer { Interval = 40 };
            _ballTimer.Tick += _ballTimer_Tick;
            _movementTimer.Tick += _movementTimer_Tick;
            Controls.RemoveByKey("gamePanel");
            Controls.Add(_gamePanel);
            ResizeForm();

            _gameStart = true;
            _movementStep = MovementStep;
            _hearts = Hearts;
            _currentPaddleFrag = PaddleFragments;
            _ballTimer.Interval = 200 / _game.Speed;
            Log("Game Start");

            _controls.Clear();

            #region Vertical Wall

            for (int i = 0, j = (_game.Cols + 1) * _game.NutWidth; i < _game.Rows * NutsToPanelRatio + 2; i++)
            {
                _controls.Add(new Nut(0, i * _game.NutWidth, _game.NutWidth));
                _controls.Add(new Nut(j, i * _game.NutWidth, _game.NutWidth));
            }

            #endregion

            #region Horizontall Wall

            for (int i = 1, j = (_game.Rows * NutsToPanelRatio + 1) * _game.NutWidth; i <= _game.Cols; i++)
            {
                _controls.Add(new Nut(i * _game.NutWidth, 0, _game.NutWidth, NutType.Wall));
                _controls.Add(new Nut(i * _game.NutWidth, j, _game.NutWidth, NutType.Earth));
            }

            #endregion

            #region Nut Table

            for (var i = 1; i <= _game.Rows; i++)
                for (var j = 1; j <= _game.Cols; j++)
                {
                    var nut = new Nut(j * _game.NutWidth, i * _game.NutWidth, _game.NutWidth, NutType.Nut, FoodType.Big);
                    nut.FoodHit += Nut_FoodHit;
                    _controls.Add(nut);
                }

            #endregion

            #region Ball

            _ballIndex = _controls.Count;
            _controls.Add(new Nut((_game.Cols / 2 + 1) * _game.NutWidth, (_game.Rows * NutsToPanelRatio - 1) * _game.NutWidth,
                _game.NutWidth, NutType.Ball, FoodType.Null, _currentPaddleFrag / 2));

            #endregion

            #region Paddle

            _paddleIndex = _ballIndex + 1;
            for (int i = 0, j = _controls[_ballIndex].Left - _currentPaddleFrag / 2 * _game.NutWidth; i < _currentPaddleFrag; i++, j += _game.NutWidth)
                _controls.Add(new Nut(j, _game.Rows * NutsToPanelRatio * _game.NutWidth, _game.NutWidth, NutType.Paddle, FoodType.Null, i));

            #endregion
        }

        private void Nut_FoodHit(object sender)
        {
            var foodHitTimer = new Timer { Interval = 300, Tag = sender, Enabled = true };
            foodHitTimer.Tick += FoodHitTimer_Tick;
        }

        private void FoodHitTimer_Tick(object sender, EventArgs e)
        {
            var timer = (Timer)sender;
            var nut = (Nut)timer.Tag;
            timer.Stop();
            if (_gameStart)
            {
                nut.Visible = true;
                nut.Index = -2;
                var dirResult = SearchPanel(new Point(nut.Left, nut.Top + _game.NutWidth));
                switch (dirResult)
                {
                    case -2:
                        nut.Visible = false;
                        timer.Stop();
                        return;
                    case -1:
                        nut.Top += _game.NutWidth;
                        break;
                    default:
                        if (((Nut)_controls[dirResult]).Type == NutType.Paddle)
                        {
                            nut.Visible = false;
                            timer.Stop();
                            Award(nut.Food);
                            return;
                        }
                        nut.Top += _game.NutWidth;
                        break;
                }
                timer.Start();
            }
            else
            {
                #region Destroy dropped food on heart lose
                if (!_ballStart)
                {
                    nut.Visible = false;
                    timer.Stop();
                } 
                #endregion
            }
        }

        private void _ballTimer_Tick(object sender, EventArgs e)
        {
            _ballTimer.Stop();
            if (_gameStart)
            {
                int dirResult, vResult, hResult;

                switch (_ballDirection)
                {
                    case Direction.N:

                        #region North

                        dirResult =
                            SearchPanel(new Point(_controls[_ballIndex].Left,
                                _controls[_ballIndex].Top - _game.NutWidth));
                        if (dirResult == -1)
                            _controls[_ballIndex].Top -= _game.NutWidth;
                        else
                        {
                            VisibilityScroring(dirResult);
                            _ballDirection = Direction.S;
                        }
                        break;

                    #endregion

                    case Direction.S:

                        #region South

                        dirResult =
                            SearchPanel(new Point(_controls[_ballIndex].Left,
                                _controls[_ballIndex].Top + _game.NutWidth));
                        if (dirResult == -2)
                        {
                            LoseHeart();
                            return;
                        }
                        if (dirResult == -1)
                            _controls[_ballIndex].Top += _game.NutWidth;
                        else
                        {
                            var tempNut = (Nut)_controls[dirResult];
                            switch (tempNut.Type)
                            {
                                case NutType.Paddle:
                                    if (_ballStick)
                                    {
                                        StickBallToPaddle();
                                        return;
                                    }
                                    if (tempNut.Index > ((Nut)_controls[_ballIndex]).Index)
                                        _ballDirection = Direction.NE;
                                    else if (tempNut.Index < ((Nut)_controls[_ballIndex]).Index)
                                        _ballDirection = Direction.NW;
                                    else _ballDirection = Direction.N;
                                    ((Nut)_controls[_ballIndex]).Index = tempNut.Index;
                                    break;
                                default:
                                    VisibilityScroring(dirResult);
                                    _ballDirection = Direction.N;
                                    break;
                            }
                        }
                        break;

                    #endregion

                    case Direction.NE:

                        #region North East

                        dirResult =
                            SearchPanel(new Point(_controls[_ballIndex].Left + _game.NutWidth * _movementStep,
                                _controls[_ballIndex].Top - _game.NutWidth));
                        hResult =
                            SearchPanel(new Point(_controls[_ballIndex].Left + _game.NutWidth,
                                _controls[_ballIndex].Top));
                        vResult =
                            SearchPanel(
                                new Point(_controls[_ballIndex].Left + (_movementStep == 1 ? 0 : _game.NutWidth),
                                    _controls[_ballIndex].Top - _game.NutWidth));

                        if (dirResult == -1 && vResult == -1 && hResult == -1)
                            _controls[_ballIndex].Location =
                                new Point(_controls[_ballIndex].Left + _game.NutWidth * _movementStep,
                                    _controls[_ballIndex].Top - _game.NutWidth);
                        else
                        {
                            if (vResult == -1 && hResult == -1 && dirResult != -1)
                            {
                                if (_movementStep == 1)
                                    _ballDirection = Direction.SW;
                                else
                                {
                                    _controls[_ballIndex].Location =
                                        new Point(_controls[_ballIndex].Left + _game.NutWidth,
                                            _controls[_ballIndex].Top - _game.NutWidth);
                                    _ballDirection = Direction.NW;
                                }
                                VisibilityScroring(dirResult);
                            }
                            else if (vResult != -1 && hResult != -1)
                            {
                                if (_movementStep == 1)
                                {
                                    VisibilityScroring(vResult);
                                    _ballDirection = Direction.SW;
                                }
                                else
                                    _ballDirection = Direction.NW;

                                VisibilityScroring(hResult);
                            }
                            else if (vResult != -1 && hResult == -1)
                            {
                                VisibilityScroring(vResult);
                                _ballDirection = Direction.SE;
                            }
                            else if (vResult == -1 && hResult != -1)
                            {
                                VisibilityScroring(hResult);
                                _ballDirection = Direction.NW;
                            }
                        }
                        break;

                    #endregion

                    case Direction.NW:

                        #region Noth West

                        dirResult =
                            SearchPanel(new Point(_controls[_ballIndex].Left - _game.NutWidth * _movementStep,
                                _controls[_ballIndex].Top - _game.NutWidth));
                        hResult =
                            SearchPanel(new Point(_controls[_ballIndex].Left - _game.NutWidth,
                                _controls[_ballIndex].Top));
                        vResult =
                            SearchPanel(
                                new Point(_controls[_ballIndex].Left - (_movementStep == 1 ? 0 : _game.NutWidth),
                                    _controls[_ballIndex].Top - _game.NutWidth));

                        if (dirResult == -1 && vResult == -1 && hResult == -1)
                            _controls[_ballIndex].Location =
                                new Point(_controls[_ballIndex].Left - _game.NutWidth * _movementStep,
                                    _controls[_ballIndex].Top - _game.NutWidth);
                        else
                        {
                            if (vResult == -1 && hResult == -1 && dirResult != -1)
                            {
                                if (_movementStep == 1)

                                    _ballDirection = Direction.SE;
                                else
                                {
                                    _controls[_ballIndex].Location =
                                        new Point(_controls[_ballIndex].Left - _game.NutWidth,
                                            _controls[_ballIndex].Top - _game.NutWidth);
                                    _ballDirection = Direction.NE;
                                }
                                VisibilityScroring(dirResult);
                            }
                            else if (vResult != -1 && hResult != -1)
                            {
                                if (_movementStep == 1)
                                {
                                    VisibilityScroring(vResult);
                                    _ballDirection = Direction.SE;
                                }
                                else
                                    _ballDirection = Direction.NE;

                                VisibilityScroring(hResult);
                            }
                            else if (vResult != -1 && hResult == -1)
                            {
                                VisibilityScroring(vResult);
                                _ballDirection = Direction.SW;
                            }
                            else if (vResult == -1 && hResult != -1)
                            {
                                VisibilityScroring(hResult);
                                _ballDirection = Direction.NE;
                            }
                        }
                        break;

                    #endregion

                    case Direction.SE:

                        #region South East

                        dirResult =
                            SearchPanel(new Point(_controls[_ballIndex].Left + _game.NutWidth,
                                _controls[_ballIndex].Top + _game.NutWidth));
                        hResult =
                            SearchPanel(new Point(_controls[_ballIndex].Left + _game.NutWidth,
                                _controls[_ballIndex].Top));
                        vResult =
                            SearchPanel(new Point(_controls[_ballIndex].Left,
                                _controls[_ballIndex].Top + _game.NutWidth));

                        if (dirResult == -2 || vResult == -2 || hResult == -2)
                        {
                            LoseHeart();
                            return;
                        }
                        if (dirResult == -1 && vResult == -1 && hResult == -1)
                            _controls[_ballIndex].Location =
                                new Point(_controls[_ballIndex].Left + _game.NutWidth,
                                    _controls[_ballIndex].Top + _game.NutWidth);
                        else
                        {
                            _movementStep = 1;
                            if (vResult == -1 && hResult == -1 && dirResult != -1)
                            {
                                _movementStep = 2;
                                VisibilityScroring(dirResult);
                                _ballDirection = Direction.NW;
                            }
                            else if (vResult != -1 && hResult != -1)
                            {
                                if (_ballStick)
                                    if (((Nut)_controls[vResult]).Type == NutType.Paddle)
                                    {
                                        StickBallToPaddle();
                                        return;
                                    }
                                VisibilityScroring(hResult);
                                VisibilityScroring(vResult);
                                _ballDirection = Direction.NW;
                            }
                            else if (vResult != -1 && hResult == -1)
                            {
                                if (_ballStick)
                                    if (((Nut)_controls[vResult]).Type == NutType.Paddle)
                                    {
                                        StickBallToPaddle();
                                        return;
                                    }
                                VisibilityScroring(vResult);
                                _ballDirection = Direction.NE;
                            }
                            else if (vResult == -1 && hResult != -1)
                            {
                                VisibilityScroring(hResult);
                                _ballDirection = Direction.SW;
                            }
                        }
                        break;

                    #endregion

                    case Direction.SW:

                        #region South West

                        dirResult =
                            SearchPanel(new Point(_controls[_ballIndex].Left - _game.NutWidth,
                                _controls[_ballIndex].Top + _game.NutWidth));
                        hResult =
                            SearchPanel(new Point(_controls[_ballIndex].Left - _game.NutWidth,
                                _controls[_ballIndex].Top));
                        vResult =
                            SearchPanel(new Point(_controls[_ballIndex].Left,
                                _controls[_ballIndex].Top + _game.NutWidth));

                        if (dirResult == -2 || vResult == -2 || hResult == -2)
                        {
                            LoseHeart();
                            return;
                        }
                        if (dirResult == -1 && vResult == -1 && hResult == -1)
                            _controls[_ballIndex].Location =
                                new Point(_controls[_ballIndex].Left - _game.NutWidth,
                                    _controls[_ballIndex].Top + _game.NutWidth);
                        else
                        {
                            _movementStep = 1;
                            if (vResult == -1 && hResult == -1 && dirResult != -1)
                            {
                                _movementStep = 2;
                                VisibilityScroring(dirResult);
                                _ballDirection = Direction.NE;
                            }
                            else if (vResult != -1 && hResult != -1)
                            {
                                if (_ballStick)
                                    if (((Nut)_controls[vResult]).Type == NutType.Paddle)
                                    {
                                        StickBallToPaddle();
                                        return;
                                    }
                                VisibilityScroring(hResult);
                                VisibilityScroring(vResult);
                                _ballDirection = Direction.NE;
                            }
                            else if (vResult != -1 && hResult == -1)
                            {
                                if (_ballStick)
                                    if (((Nut)_controls[vResult]).Type == NutType.Paddle)
                                    {
                                        StickBallToPaddle();
                                        return;
                                    }
                                VisibilityScroring(vResult);
                                _ballDirection = Direction.NW;
                            }
                            else if (vResult == -1 && hResult != -1)
                            {
                                VisibilityScroring(hResult);
                                _ballDirection = Direction.SE;
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
                case FoodType.Big:

                    #region Big

                    Log("Paddle Growed ...");
                    if (_currentPaddleFrag < _game.Cols - 2)
                    {
                        _controls.Add(
                            new Nut(_controls[_paddleIndex + _currentPaddleFrag - 1].Left + _game.NutWidth,
                                _game.Rows * NutsToPanelRatio * _game.NutWidth, _game.NutWidth,
                                NutType.Paddle, FoodType.Null, _currentPaddleFrag));
                        if (_controls[_paddleIndex + _currentPaddleFrag++ - 1].Left + _game.NutWidth * 2 >
                            _gamePanel.Width - _game.NutWidth)
                            for (var i = _paddleIndex; i < _paddleIndex + _currentPaddleFrag; i++)
                                _controls[i].Left -= _game.NutWidth;
                    }
                    break;

                #endregion

                case FoodType.Small:

                    #region Small

                    Log("Paddle Shrinked ...");
                    if (_currentPaddleFrag == 1)
                    {
                        _hearts = 0;
                        LoseHeart();
                    }
                    else
                        _controls[_ballIndex + _currentPaddleFrag--].Dispose();
                    break;

                #endregion

                case FoodType.Live:

                    #region Live

                    Log("Live Increased ...");
                    _hearts++;
                    break;

                #endregion

                case FoodType.Death:

                    #region Death

                    Log("Live Decreased ...");
                    if (--_hearts < 0) LoseHeart();
                    break;

                #endregion

                case FoodType.Stick:

                    #region Stick

                    Log("Sticky Ball ...");
                    _ballStick = true;
                    break;

                #endregion

                case FoodType.SpeedUp:

                    #region Speed Up

                    Log("Speed Increased ...");
                    _game.Speed += 1;
                    _ballTimer.Interval = 200 / _game.Speed;
                    break;

                #endregion

                case FoodType.SpeedDown:

                    #region Speed Down

                    Log("Speed Decreased ...");
                    _game.Speed -= 1;
                    _ballTimer.Interval = 200 / _game.Speed;
                    break;

                    #endregion
            }
        }

        private void StickBallToPaddle()
        {
            _ballTimer.Stop();
            _ballDirection = Direction.N;
            _ballStart = false;
            ((Nut)_controls[_ballIndex]).Index = _currentPaddleFrag / 2;
        }

        private void MovePaddle()
        {
            if (!_gameStart) return;

            if (_moveLeft)
            {
                #region Left
                if (_controls[_paddleIndex].Left - _game.NutWidth >= _game.NutWidth)
                {
                    for (var i = _paddleIndex; i < _paddleIndex + _currentPaddleFrag; i++)
                        _controls[i].Left -= _game.NutWidth;
                    if (!_ballStart)
                        _controls[_ballIndex].Left -= _game.NutWidth;
                }
                #endregion
            }
            if (_moveRight)
            {
                #region Right
                if (_controls[_paddleIndex + _currentPaddleFrag - 1].Left + _game.NutWidth * 2 <=
                    _gamePanel.Width - _game.NutWidth)
                {
                    for (var i = _paddleIndex; i < _paddleIndex + _currentPaddleFrag; i++)
                        _controls[i].Left += _game.NutWidth;
                    if (!_ballStart)
                        _controls[_ballIndex].Left += _game.NutWidth;
                }
                #endregion
            }
        }

        private void LoseHeart()
        {
            _ballTimer.Stop();
            _ballStart = _gameStart = false;
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
                    InitializeGame();
                #endregion
            }
        }

        private void VisibilityScroring(int index)
        {
            if (((Nut)_controls[index]).Type == NutType.Nut)
            {
                _controls[index].Visible = false;
                if ((_score += ScoreStep) >= _game.Rows * _game.Cols * ScoreStep) MessageBox.Show("YOU WIN");
            }
        }

        private int SearchPanel(Point point)
        {
            for (var i = 0; i < _controls.Count; i++)
                if (_controls[i].Location == point && _controls[i].Visible && ((Nut)_controls[i]).Type != NutType.Earth && ((Nut)_controls[i]).Index != -2)
                    return i;
                else if (_controls[i].Location == point && _controls[i].Visible && ((Nut)_controls[i]).Type == NutType.Earth)
                    return -2;
            return -1;
        }

        private void Log(string content)
        {
            Text = content;
        }

        private void RealignPaddle()
        {
            var ball = (Nut)_controls[_ballIndex];
            ball.Location = new Point((_game.Cols / 2 + 1) * _game.NutWidth, (_game.Rows * NutsToPanelRatio - 1) * _game.NutWidth);
            ball.Index = _currentPaddleFrag / 2;
            for (int i = 0, j = ball.Left - _currentPaddleFrag / 2 * _game.NutWidth; i < _currentPaddleFrag; i++, j += _game.NutWidth)
                _controls[_paddleIndex + i].Location = new Point(j, _game.Rows * NutsToPanelRatio * _game.NutWidth);
        }

        private void ResizeForm()
        {
            Size = new Size(_gamePanel.Width + Gap * 2 + _game.NutWidth * 2, _gamePanel.Height + _game.NutWidth * 4 + Gap * 4);
        }

        private void MainFrm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == _previousKey) return;
            _previousKey = e.KeyData;
            switch (_previousKey)
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

                    _ballTimer.Enabled = _gameStart;
                    _ballStart = true;
                    _ballStick = false;
                    break;

                #endregion

                case Keys.P:

                    #region Play & Pause

                    _ballTimer.Enabled = !_ballTimer.Enabled;
                    _gameStart = !_gameStart;
                    break;

                    #endregion
            }
        }

        private void PlayForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left) _moveLeft = false;
            if (e.KeyCode == Keys.Right) _moveRight = false;
            if (_moveLeft || _moveRight) return;
            _previousKey = Keys.None;
            _movementTimer.Stop();
        }

        private void newGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _gameStart = true;
            InitializeGame();
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