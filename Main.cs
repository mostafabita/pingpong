using System;
using System.Windows.Forms;

namespace Pong
{
    public partial class Main : Form
    {
        private GameLevel _gameLevel;
        private Game _game;
        
        public Main()
        {
            InitializeComponent();
            InitializeGame(_gameLevel);
        }

        public void InitializeGame(GameLevel gameLevel = GameLevel.Beginner)
        {
            _gameLevel = gameLevel;
            _game = new Game(_gameLevel);
            _game.Create(this);
        }

        private void newGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InitializeGame(_gameLevel);
        }

        private void changeLevelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (((ToolStripMenuItem)sender).Text)
            {
                case "Beginner":
                    InitializeGame(GameLevel.Beginner);
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