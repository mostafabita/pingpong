using System;
using System.Windows.Forms;

namespace Pong
{
    public partial class Main : Form
    {
        private readonly Game _game = new Game();

        public Main()
        {
            InitializeComponent();
            _game.Create(this);
        }

        private void newGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _game.Create(this);
        }

        private void changeLevelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (((ToolStripMenuItem)sender).Text)
            {
                case "Beginner":
                    _game.Create(this, GameLevel.Beginner);
                    break;
                case "Intermediate":
                    _game.Create(this, GameLevel.Intermediate);
                    break;
                case "Advanced":
                    _game.Create(this, GameLevel.Advanced);
                    break;
            }
        }
    }
}