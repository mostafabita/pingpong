using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using PingPong.Common;
using PingPong.Controls;
using PingPong.Enums;
using static System.Windows.Forms.Application;
using Timer = System.Windows.Forms.Timer;

namespace PingPong.Forms
{
    public partial class Main : Form
    {
        private bool _gameStart = true;
        private bool _moveLeft;
        private bool _moveRight;
        private bool _ballStart;
        private bool _ballStick;
        private int _ballIndex;
        private int _rocketIndex;
        private int _rows;
        private int _cols;
        private int _currentRocketFrag;
        private int _nutWidth;
        private int _score;
        private int _speed;
        private int _movementStep = 1;
        private int _lives = 3;
        private int _gap = 3;
        private const short NutsToPanelRatio = 4;
        private const int RocketFragments = 5;
        private const int ScoreStep = 5;
        private readonly Random _rnd = new Random();
        private readonly Timer _ballTimer;
        private readonly Timer _movementTimer;
        private Direction _ballDirection = Direction.N;
        private Keys? _previousKey;
        private Game _game;

        public Main()
        {
            _game = new Game(GameLevel.Beginner);
            _ballTimer = new Timer { Interval = 40 };
            _ballTimer.Tick += _ballTimer_Tick;
            _movementTimer = new Timer { Interval = 40 };
            _movementTimer.Tick += _movementTimer_Tick;

            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            InitializeGame();
        }

        public void InitializeGame()
        {
            _currentRocketFrag = RocketFragments;
            _nutWidth = _game.NutWidth;
            _rows = _game.Rows;
            _cols = _game.Cols;
            _speed = _game.Speed;
            _ballTimer.Interval = 200 / _speed;
            speedLbl.Text = _speed.ToString();
            livesLbl.Text = _lives.ToString();
            scoreLbl.Text = "0";
            Text = "Start Game";
            var controls = gamePanel.Controls;

            ResizeFormObjects();
            controls.Clear();

            #region Vertical Wall

            for (int i = 0, j = (_cols + 1) * _nutWidth; i < _rows * NutsToPanelRatio + 2; i++)
            {
                controls.Add(new Nut(0, i * _nutWidth, _nutWidth, NutType.Wall));
                controls.Add(new Nut(j, i * _nutWidth, _nutWidth, NutType.Wall));
            }

            #endregion

            #region Horizontall Wall

            for (int i = 1, j = (_rows * NutsToPanelRatio + 1) * _nutWidth; i <= _cols; i++)
            {
                controls.Add(new Nut(i * _nutWidth, 0, _nutWidth, NutType.Wall));
                controls.Add(new Nut(i * _nutWidth, j, _nutWidth, NutType.Earth));
            }

            #endregion

            #region Nut Table

            for (var i = 1; i <= _rows; i++)
                for (var j = 1; j <= _cols; j++)
                {
                    var nut = new Nut(j * _nutWidth, i * _nutWidth, _nutWidth, NutType.Nut, DetermineFoodType());
                    nut.FoodHit += Nut_FoodHit;
                    controls.Add(nut);
                }

            #endregion

            #region Ball

            _ballIndex = controls.Count;
            controls.Add(new Nut((_cols / 2 + 1) * _nutWidth, (_rows * NutsToPanelRatio - 1) * _nutWidth,
                _nutWidth, NutType.Ball, FoodType.Null, _currentRocketFrag / 2));

            #endregion

            #region Rocket

            _rocketIndex = controls.Count;
            for (int i = 0, j = controls[_ballIndex].Left - _currentRocketFrag / 2 * _nutWidth;
                i < _currentRocketFrag;
                i++, j += _nutWidth)
                controls.Add(new Nut(j, _rows * NutsToPanelRatio * _nutWidth, _nutWidth, NutType.Rocket, FoodType.Null, i));

            #endregion
        }

        private FoodType DetermineFoodType()
        {
            return _rnd.Next((int)_game.Foods) == 1 ? (FoodType)_rnd.Next(0, 6) : FoodType.Null;
        }

        private void ResizeFormObjects()
        {
            gamePanel.Size = new Size((_cols + 2) * _nutWidth + _gap, (_rows * NutsToPanelRatio + 2) * _nutWidth + _gap);
            gamePanel.BackColor = Color.White;
            Size = new Size(gamePanel.Width + 40, gamePanel.Height + 150);
        }

        private void AlignRocket()
        {
            gamePanel.Controls[_ballIndex].Location = new Point((_cols / 2 + 1) * _nutWidth,
                (_rows * NutsToPanelRatio - 1) * _nutWidth);
            for (int i = 0, j = gamePanel.Controls[_ballIndex].Left - _currentRocketFrag / 2 * _nutWidth;
                i < _currentRocketFrag;
                i++, j += _nutWidth)
                gamePanel.Controls[_rocketIndex + i].Location = new Point(j, gamePanel.Height - 2 * _nutWidth);
            ((Nut)gamePanel.Controls[_ballIndex]).Index = _currentRocketFrag / 2;
        }

        private void Nut_FoodHit(object sender)
        {
            var foodHitTimer = new Timer { Interval = 300, Tag = sender, Enabled = true };
            foodHitTimer.Tick += FoodHitTimer_Tick;
        }

        private void FoodHitTimer_Tick(object sender, EventArgs e)
        {
            var timer = (Timer)sender;
            timer.Stop();
            if (_gameStart)
            {
                var nut = (Nut)timer.Tag;
                nut.Visible = true;
                nut.Index = -2;
                var dirResult = SearchPanel(new Point(nut.Left, nut.Top + _nutWidth));
                switch (dirResult)
                {
                    case -2:
                        nut.Visible = false;
                        timer.Stop();
                        return;
                    case -1:
                        nut.Top += _nutWidth;
                        break;
                    default:
                        if (((Nut)gamePanel.Controls[dirResult]).Type == NutType.Rocket)
                        {
                            nut.Visible = false;
                            timer.Stop();
                            Award(nut.Food);
                            return;
                        }
                        nut.Top += _nutWidth;
                        break;
                }
            }
            timer.Start();
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
                            SearchPanel(new Point(gamePanel.Controls[_ballIndex].Left,
                                gamePanel.Controls[_ballIndex].Top - _nutWidth));
                        if (dirResult == -1)
                            gamePanel.Controls[_ballIndex].Top -= _nutWidth;
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
                            SearchPanel(new Point(gamePanel.Controls[_ballIndex].Left,
                                gamePanel.Controls[_ballIndex].Top + _nutWidth));
                        if (dirResult == -2)
                        {
                            GameOver();
                            return;
                        }
                        if (dirResult == -1)
                            gamePanel.Controls[_ballIndex].Top += _nutWidth;
                        else
                        {
                            var tempNut = (Nut)gamePanel.Controls[dirResult];
                            switch (tempNut.Type)
                            {
                                case NutType.Rocket:
                                    if (_ballStick)
                                    {
                                        StickBallToRocket();
                                        return;
                                    }
                                    if (tempNut.Index > ((Nut)gamePanel.Controls[_ballIndex]).Index)
                                        _ballDirection = Direction.NE;
                                    else if (tempNut.Index < ((Nut)gamePanel.Controls[_ballIndex]).Index)
                                        _ballDirection = Direction.NW;
                                    else _ballDirection = Direction.N;
                                    ((Nut)gamePanel.Controls[_ballIndex]).Index = tempNut.Index;
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
                            SearchPanel(new Point(gamePanel.Controls[_ballIndex].Left + _nutWidth * _movementStep,
                                gamePanel.Controls[_ballIndex].Top - _nutWidth));
                        hResult =
                            SearchPanel(new Point(gamePanel.Controls[_ballIndex].Left + _nutWidth,
                                gamePanel.Controls[_ballIndex].Top));
                        vResult =
                            SearchPanel(
                                new Point(gamePanel.Controls[_ballIndex].Left + (_movementStep == 1 ? 0 : _nutWidth),
                                    gamePanel.Controls[_ballIndex].Top - _nutWidth));

                        if (dirResult == -1 && vResult == -1 && hResult == -1)
                            gamePanel.Controls[_ballIndex].Location =
                                new Point(gamePanel.Controls[_ballIndex].Left + _nutWidth * _movementStep,
                                    gamePanel.Controls[_ballIndex].Top - _nutWidth);
                        else
                        {
                            if (vResult == -1 && hResult == -1 && dirResult != -1)
                            {
                                if (_movementStep == 1)
                                    _ballDirection = Direction.SW;
                                else
                                {
                                    gamePanel.Controls[_ballIndex].Location =
                                        new Point(gamePanel.Controls[_ballIndex].Left + _nutWidth,
                                            gamePanel.Controls[_ballIndex].Top - _nutWidth);
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
                            SearchPanel(new Point(gamePanel.Controls[_ballIndex].Left - _nutWidth * _movementStep,
                                gamePanel.Controls[_ballIndex].Top - _nutWidth));
                        hResult =
                            SearchPanel(new Point(gamePanel.Controls[_ballIndex].Left - _nutWidth,
                                gamePanel.Controls[_ballIndex].Top));
                        vResult =
                            SearchPanel(
                                new Point(gamePanel.Controls[_ballIndex].Left - (_movementStep == 1 ? 0 : _nutWidth),
                                    gamePanel.Controls[_ballIndex].Top - _nutWidth));

                        if (dirResult == -1 && vResult == -1 && hResult == -1)
                            gamePanel.Controls[_ballIndex].Location =
                                new Point(gamePanel.Controls[_ballIndex].Left - _nutWidth * _movementStep,
                                    gamePanel.Controls[_ballIndex].Top - _nutWidth);
                        else
                        {
                            if (vResult == -1 && hResult == -1 && dirResult != -1)
                            {
                                if (_movementStep == 1)

                                    _ballDirection = Direction.SE;
                                else
                                {
                                    gamePanel.Controls[_ballIndex].Location =
                                        new Point(gamePanel.Controls[_ballIndex].Left - _nutWidth,
                                            gamePanel.Controls[_ballIndex].Top - _nutWidth);
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
                            SearchPanel(new Point(gamePanel.Controls[_ballIndex].Left + _nutWidth,
                                gamePanel.Controls[_ballIndex].Top + _nutWidth));
                        hResult =
                            SearchPanel(new Point(gamePanel.Controls[_ballIndex].Left + _nutWidth,
                                gamePanel.Controls[_ballIndex].Top));
                        vResult =
                            SearchPanel(new Point(gamePanel.Controls[_ballIndex].Left,
                                gamePanel.Controls[_ballIndex].Top + _nutWidth));

                        if (dirResult == -2 || vResult == -2 || hResult == -2)
                        {
                            GameOver();
                            return;
                        }
                        if (dirResult == -1 && vResult == -1 && hResult == -1)
                            gamePanel.Controls[_ballIndex].Location =
                                new Point(gamePanel.Controls[_ballIndex].Left + _nutWidth,
                                    gamePanel.Controls[_ballIndex].Top + _nutWidth);
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
                                    if (((Nut)gamePanel.Controls[vResult]).Type == NutType.Rocket)
                                    {
                                        StickBallToRocket();
                                        return;
                                    }
                                VisibilityScroring(hResult);
                                VisibilityScroring(vResult);
                                _ballDirection = Direction.NW;
                            }
                            else if (vResult != -1 && hResult == -1)
                            {
                                if (_ballStick)
                                    if (((Nut)gamePanel.Controls[vResult]).Type == NutType.Rocket)
                                    {
                                        StickBallToRocket();
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
                            SearchPanel(new Point(gamePanel.Controls[_ballIndex].Left - _nutWidth,
                                gamePanel.Controls[_ballIndex].Top + _nutWidth));
                        hResult =
                            SearchPanel(new Point(gamePanel.Controls[_ballIndex].Left - _nutWidth,
                                gamePanel.Controls[_ballIndex].Top));
                        vResult =
                            SearchPanel(new Point(gamePanel.Controls[_ballIndex].Left,
                                gamePanel.Controls[_ballIndex].Top + _nutWidth));

                        if (dirResult == -2 || vResult == -2 || hResult == -2)
                        {
                            GameOver();
                            return;
                        }
                        if (dirResult == -1 && vResult == -1 && hResult == -1)
                            gamePanel.Controls[_ballIndex].Location =
                                new Point(gamePanel.Controls[_ballIndex].Left - _nutWidth,
                                    gamePanel.Controls[_ballIndex].Top + _nutWidth);
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
                                    if (((Nut)gamePanel.Controls[vResult]).Type == NutType.Rocket)
                                    {
                                        StickBallToRocket();
                                        return;
                                    }
                                VisibilityScroring(hResult);
                                VisibilityScroring(vResult);
                                _ballDirection = Direction.NE;
                            }
                            else if (vResult != -1 && hResult == -1)
                            {
                                if (_ballStick)
                                    if (((Nut)gamePanel.Controls[vResult]).Type == NutType.Rocket)
                                    {
                                        StickBallToRocket();
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
            MoveRocket();
        }

        private void Award(FoodType foodType)
        {
            switch (foodType)
            {
                case FoodType.Big:

                    #region Big

                    Text = "Rocket Growed ...";
                    if (_currentRocketFrag < _cols - 2)
                    {
                        gamePanel.Controls.Add(
                            new Nut(gamePanel.Controls[_rocketIndex + _currentRocketFrag - 1].Left + _nutWidth,
                                _rows * NutsToPanelRatio * _nutWidth, _nutWidth,
                                NutType.Rocket, FoodType.Null, _currentRocketFrag));
                        if (gamePanel.Controls[_rocketIndex + _currentRocketFrag++ - 1].Left + _nutWidth * 2 >
                            gamePanel.Width - _nutWidth)
                            for (var i = _rocketIndex; i < _rocketIndex + _currentRocketFrag; i++)
                                gamePanel.Controls[i].Left -= _nutWidth;
                    }
                    break;

                #endregion

                case FoodType.Small:

                    #region Small

                    Text = "Rocket Shrinked ...";
                    if (_currentRocketFrag == 1)
                    {
                        _lives = 0;
                        GameOver();
                    }
                    else
                        gamePanel.Controls[_ballIndex + _currentRocketFrag--].Dispose();
                    break;

                #endregion

                case FoodType.Live:

                    #region Live

                    Text = "Live Increased ...";
                    livesLbl.Text = (++_lives).ToString();
                    break;

                #endregion

                case FoodType.Death:

                    #region Death

                    Text = "Live Decreased ...";
                    if (--_lives < 0)
                        GameOver();
                    else
                        livesLbl.Text = _lives.ToString();
                    break;

                #endregion

                case FoodType.Stick:

                    #region Stick

                    Text = "Sticky Ball ...";
                    _ballStick = true;
                    break;

                #endregion

                case FoodType.SpeedUp:

                    #region Speed Up

                    Text = "Speed Increased ...";
                    _speed += 1;
                    _ballTimer.Interval = 200 / _speed;
                    break;

                #endregion

                case FoodType.SpeedDown:

                    #region Speed Down

                    Text = "Speed Decreased ...";
                    _speed -= 1;
                    _ballTimer.Interval = 200 / _speed;
                    break;

                    #endregion
            }
        }

        private void StickBallToRocket()
        {
            _ballTimer.Stop();
            _ballDirection = Direction.N;
            _ballStart = false;
            ((Nut)gamePanel.Controls[_ballIndex]).Index = _currentRocketFrag / 2;
        }

        private void MoveRocket()
        {
            if (!_gameStart) return;

            if (_moveLeft)
            {
                #region Left
                if (gamePanel.Controls[_rocketIndex].Left - _nutWidth >= _nutWidth)
                {
                    for (var i = _rocketIndex; i < _rocketIndex + _currentRocketFrag; i++)
                        gamePanel.Controls[i].Left -= _nutWidth;
                    if (!_ballStart)
                        gamePanel.Controls[_ballIndex].Left -= _nutWidth;
                }
                #endregion
            }
            if (_moveRight)
            {
                #region Right
                if (gamePanel.Controls[_rocketIndex + _currentRocketFrag - 1].Left + _nutWidth * 2 <=
                    gamePanel.Width - _nutWidth)
                {
                    for (var i = _rocketIndex; i < _rocketIndex + _currentRocketFrag; i++)
                        gamePanel.Controls[i].Left += _nutWidth;
                    if (!_ballStart)
                        gamePanel.Controls[_ballIndex].Left += _nutWidth;
                }
                #endregion
            }
        }

        private void GameOver()
        {
            _ballTimer.Stop();
            _ballStart = false;
            _ballDirection = Direction.N;
            if (_lives > 0)
            {
                #region Loos Live

                livesLbl.Text = (--_lives).ToString();
                _gameStart = !_gameStart;
                Thread.Sleep(1500);
                _gameStart = !_gameStart;
                AlignRocket();

                #endregion
            }
            else
            {
                #region Game Over

                _gameStart = false;
                MessageBox.Show("Game Over", "Tennis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (
                    MessageBox.Show("Do you want to restart game ?", "Tennis", MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    livesLbl.Text = (_lives = 3).ToString();
                    _gameStart = true;
                    InitializeGame();
                }

                #endregion
            }
        }

        private void VisibilityScroring(int index)
        {
            if (((Nut)gamePanel.Controls[index]).Type == NutType.Nut)
            {
                gamePanel.Controls[index].Visible = false;
                if ((_score += ScoreStep) >= _rows * _cols * ScoreStep)
                {
                    MessageBox.Show("YOU WIN");
                }
                scoreLbl.Text = _score.ToString();
            }
        }

        private int SearchPanel(Point point)
        {
            for (var i = 0; i < gamePanel.Controls.Count; i++)
                if (gamePanel.Controls[i].Location == point && gamePanel.Controls[i].Visible &&
                    ((Nut)gamePanel.Controls[i]).Type != NutType.Earth && ((Nut)gamePanel.Controls[i]).Index != -2)
                    return i;
                else if (gamePanel.Controls[i].Location == point && gamePanel.Controls[i].Visible &&
                         ((Nut)gamePanel.Controls[i]).Type == NutType.Earth)
                    return -2;
            return -1;
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
                    MoveRocket();
                    _movementTimer.Start();
                    break;

                #endregion

                case Keys.Right:

                    #region Right
                    _moveRight = true;
                    MoveRocket();
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
            _previousKey = null;
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
                    ChangeGameLevel(GameLevel.Beginner);
                    break;
                case "Intermediate":
                    ChangeGameLevel(GameLevel.Intermediate);
                    break;
                case "Advanced":
                    ChangeGameLevel(GameLevel.Advanced);
                    break;
            }
        }

        private void ChangeGameLevel(GameLevel level)
        {
            _game = new Game(level);
            InitializeGame();
        }
    }
}