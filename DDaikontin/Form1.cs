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
        private bool exited = false;

        private bool drawMeow2 = false;

        public Form1()
        {
            InitializeComponent();
            new Thread(() =>
            {
                core.MenuLoop = MenuLoop;
                core.MenuDraw = MenuDraw;
                core.GameLoop = GameLoop;
                core.GameDraw = MenuDraw; //Drawing is done in the Paint method for now
                enterKey = core.RegisterInput(Keys.Enter);
                core.Begin();
            }).Start();
        }

        public void MenuLoop()
        {
            var state = core.GetInputState(enterKey);
            if (state == Core.InputState.JustPressed) drawMeow2 = true;
            else drawMeow2 = false;
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

            //TODO: This is where you draw stuff
            g.DrawString("Meow", this.Font, Brushes.Black, new PointF(20, 20));
            if (drawMeow2) g.DrawString("Meow", this.Font, Brushes.Black, new PointF(50, 50));
        }

        public class GameState
        {
            public List<ShipBase> playerShips = new List<ShipBase>();
        }

        protected GameState gs = new GameState();

        public void ResetGameState()
        {
            gs = new GameState();
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
