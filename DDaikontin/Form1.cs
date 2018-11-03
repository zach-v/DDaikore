using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using DDaikore;

namespace DDaikontin
{
    public partial class Form1 : Form
    {
        Core core = new Core();
        private int enterKey = 0;
        private int upArrowKey = 0;
        private int downArrowKey = 0;

        public Form1()
        {
            InitializeComponent();
            new Thread(() =>
            {
                core.MenuLoop = MenuLoop;
                core.MenuDraw = MenuDraw;
                core.GameLoop = GameLoop;
                core.GameDraw = MenuDraw; //Drawing is done in the Paint method for now

                regKeys();

                core.Begin();
            }).Start();
        }

        public void MenuLoop()
        {
            if (core.GetInputState(enterKey) == Core.InputState.JustPressed)
            {
                ResetGameState();
                core.menuIndex = -1;
            }
            else if (core.GetInputState(upArrowKey) == Core.InputState.JustPressed) core.menuOption = (core.menuOption + 1) % 2;
            else if (core.GetInputState(downArrowKey) == Core.InputState.JustPressed) core.menuOption = core.menuOption == 0 ? 1 : core.menuOption - 1;
        }

        public void MenuDraw()
        {
            pictureBox1.Invalidate();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            core.Exit();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            //Clear the background
            g.FillRectangle(Brushes.White, 0, 0, pictureBox1.Width, pictureBox1.Height);

            if (core.menuOption == 0)
            {
                drawMenu(g);
            }
            if (core.menuOption == -1)
            {
                drawGame(g);
            }
        }

        private void drawMenu(Graphics g)
        {
            g.DrawString("Daikontinum", this.Font, Brushes.Black, new PointF(20, 10));
            g.DrawString("Play", this.Font, core.menuOption == 0 ? Brushes.Blue : Brushes.Black, new PointF(20, 40));
            g.DrawString("Exit", this.Font, core.menuOption == 1 ? Brushes.Blue : Brushes.Black, new PointF(20, 60));
        }

        private void drawGame(Graphics g)
        {

        }

        private void regKeys()
        {
            enterKey = core.RegisterInput(Keys.Enter);
            upArrowKey = core.RegisterInput(Keys.Up);
            downArrowKey = core.RegisterInput(Keys.Down);
        }

        public class GameState
        {
            public List<ShipBase> playerShips = new List<ShipBase>();
        }

        protected GameState gs;

        public void ResetGameState()
        {
            gs = new GameState();
            var playerShipGfx = new UnitGraphics();
            gs.playerShips.Add(new ShipBase(playerShipGfx, 10, 100));
        }

        public void GameLoop()
        {
            //Physics!
            for (var x = 0; x < gs.playerShips.Count; x++)
            {
                for (var y = x + 1; y < gs.playerShips.Count; y++)
                {
                    if (gs.playerShips[x].CollidesWith(gs.playerShips[y]))
                    {
                        //TODO: Give damage
                        //TODO: Make ships bounce apart (get angle between ships and send them in opposite directions)
                        gs.playerShips[x].velocity = -5;
                        gs.playerShips[y].velocity = 5;
                    }
                }
            }

            //TODO: Other collisions, player inputs, stuff, things, etc.
        }

    }
}
