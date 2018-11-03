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
    public partial class Form1 : Form {
        Core core = new Core();
        private int enterKey = 0;
        private int upArrowKey = 0;
        private int downArrowKey = 0;
        private int leftArrowKey = 0;
        private int rightArrowKey = 0;

        public Form1() {
            InitializeComponent();
            new Thread(() => {
                core.MenuLoop = MenuLoop;
                core.MenuDraw = MenuDraw;
                core.GameLoop = GameLoop;
                core.GameDraw = MenuDraw; //Drawing is done in the Paint method for now

                regKeys();

                core.Begin();
            }).Start();
        }

        public void MenuLoop() {
            if (core.GetInputState(enterKey) == Core.InputState.JustPressed) {
                ResetGameState();
                core.menuIndex = -1;
            } else if (core.GetInputState(upArrowKey) == Core.InputState.JustPressed) core.menuOption = (core.menuOption + 1) % 2;
            else if (core.GetInputState(downArrowKey) == Core.InputState.JustPressed) core.menuOption = core.menuOption == 0 ? 1 : core.menuOption - 1;
        }

        public void MenuDraw() {
            pictureBox1.Invalidate();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            core.Exit();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e) {
            var g = e.Graphics;
            //Clear the background
            g.FillRectangle(Brushes.Black, 0, 0, pictureBox1.Width, pictureBox1.Height);

            if (core.menuIndex >= 0) {
                drawMenu(g);
            }
            if (core.menuIndex == -1) {
                drawGame(g);
            }
        }

        private void drawMenu(Graphics g) {
            g.DrawString("Daikontinum", this.Font, Brushes.White, new PointF(20, 10));
            g.DrawString("Play", this.Font, core.menuOption == 0 ? Brushes.Blue : Brushes.White, new PointF(20, 40));
            g.DrawString("Exit", this.Font, core.menuOption == 1 ? Brushes.Blue : Brushes.White, new PointF(20, 60));
        }

        //TODO: make more comments on code
        private void drawGame(Graphics g) {
            var debugGfx = new UnitGraphics(Pens.AliceBlue);
            g.TranslateTransform((float) -gs.currentPlayer.posX + pictureBox1.Width / 2, (float) -gs.currentPlayer.posY + pictureBox1.Height / 2);
            var oldTransform = g.Transform;
            foreach (var ship in gs.playerShips.Union(gs.enemyShips)) {
                g.Transform = oldTransform;
                g.TranslateTransform((float)ship.posX, (float)ship.posY);
                g.RotateTransform((float)(ship.facing / Math.PI * 180));

                g.DrawLines(ship.uGraphics.color, ship.uGraphics.points.ToArray());

                foreach (var circle in ship.collider.dCircles) {
                    g.DrawEllipse(debugGfx.color, (float)(-circle.Radius + circle.X), (float)(-circle.Radius + circle.Y), (float)circle.Radius * 2, (float)circle.Radius * 2);
                }
                g.ResetTransform();
            }
        }

        private void regKeys() {
            enterKey = core.RegisterInput(Keys.Enter);
            upArrowKey = core.RegisterInput(Keys.Up);
            downArrowKey = core.RegisterInput(Keys.Down);
            leftArrowKey = core.RegisterInput(Keys.Left);
            rightArrowKey = core.RegisterInput(Keys.Right);
        }

        public class GameState {
            public List<ShipBase> playerShips;
            public List<ShipBase> enemyShips;
            public ShipBase currentPlayer;

            public GameState() {
                playerShips = new List<ShipBase>();
                enemyShips = new List<ShipBase>();
            }

            public void init() {
                var playerShipGfx = new UnitGraphics(Pens.White, new List<PointF>() {
                    new PointF(0,0),
                    new PointF(10,0),
                    new PointF(0,0)
                });
                var enemyShipGfx = new UnitGraphics(Pens.Red, new List<PointF>() {
                    new PointF(0,0),
                    new PointF(10,0),
                    new PointF(0,0)
                });
                currentPlayer = new ShipBase(playerShipGfx, 50, 50);
                playerShips.Add(currentPlayer);
                enemyShips.Add(new ShipBase(enemyShipGfx, 400, 400));
            }

        }

        protected GameState gs;

        public void ResetGameState() {
            gs = new GameState();
            gs.init();
        }

        public void GameLoop() {
            foreach (var ship in gs.playerShips.Union(gs.enemyShips)) {
                ship.posX += ship.velocity * Math.Cos(ship.angle);
                ship.posY += ship.velocity * Math.Sin(ship.angle);

                ship.velocity *= 0.99;
            }

            if (core.GetInputState(leftArrowKey) == Core.InputState.Held) {
                gs.currentPlayer.facing -= 0.03;
            }
            if (core.GetInputState(rightArrowKey) == Core.InputState.Held) {
                gs.currentPlayer.facing += 0.03;
            }
            if (core.GetInputState(upArrowKey) == Core.InputState.Held) {
                gs.currentPlayer.applyForce(0.05, gs.currentPlayer.facing);
            }
            if (core.GetInputState(downArrowKey) == Core.InputState.Held) {
                gs.currentPlayer.velocity *= 0.9;
            }

            //Physics!
            for (var x = 0; x < gs.playerShips.Count; x++) {
                for (var y = x + 1; y < gs.playerShips.Count; y++) {
                    if (gs.playerShips[x].CollidesWith(gs.playerShips[y])) {
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
