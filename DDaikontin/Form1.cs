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
        private const ushort GameVersion = 1;

        Core core = new Core();
        private int enterKey = 0;
        private int upArrowKey = 0;
        private int downArrowKey = 0;
        private int leftArrowKey = 0;
        private int rightArrowKey = 0;
        private int wKey = 0;
        private int sKey = 0;
        private int aKey = 0;
        private int dKey = 0;
        private int spaceKey = 0;

        public Form1()
        {
            InitializeComponent();
            new Thread(() =>
            {
                core.GameVersion = GameVersion;
                core.MenuLoop = MenuLoop;
                core.MenuDraw = MenuDraw;
                core.GameLoop = GameLoop;
                core.GameDraw = MenuDraw; //Drawing is done in the Paint method for now

                registerInputs();

                core.Begin();
            }).Start();
        }

        public void MenuLoop()
        {
            if (core.GetInputState(enterKey) == InputState.JustPressed)
            {
                ResetGameState();
                core.menuIndex = -1;
            }
            else if (core.GetInputState(upArrowKey) == InputState.JustPressed) core.menuOption = (core.menuOption + 1) % 2;
            else if (core.GetInputState(downArrowKey) == InputState.JustPressed) core.menuOption = core.menuOption == 0 ? 1 : core.menuOption - 1;
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
            lock (core)
            {
                var g = e.Graphics;
                //Clear the background
                g.FillRectangle(Brushes.Black, 0, 0, pictureBox1.Width, pictureBox1.Height);

                if (core.menuIndex >= 0)
                {
                    drawMenu(g);
                }
                if (core.menuIndex == -1)
                {
                    drawGame(g);
                }
            }
        }

        private void drawMenu(Graphics g)
        {
            g.DrawString("Daikontinum", this.Font, Brushes.White, new PointF(20, 10));
            g.DrawString("Play", this.Font, core.menuOption == 0 ? Brushes.Blue : Brushes.White, new PointF(20, 40));
            g.DrawString("Exit", this.Font, core.menuOption == 1 ? Brushes.Blue : Brushes.White, new PointF(20, 60));
        }

        private void drawGame(Graphics g)
        {
            g.TranslateTransform((float)-gs.currentPlayer.posX + pictureBox1.Width / 2, (float)-gs.currentPlayer.posY + pictureBox1.Height / 2);
            var oldTransform = g.Transform;

            var starSeed = new PseudoRandom();
            for (double x = gs.currentPlayer.posX - (pictureBox1.Width / 2); x < gs.currentPlayer.posX + (pictureBox1.Width / 2); x += 256)
            {
                int squareX = (int)Math.Floor(x / 256);
                for (double y = gs.currentPlayer.posY - (pictureBox1.Height / 2); y < gs.currentPlayer.posY + (pictureBox1.Height / 2); y += 256)
                {
                    int squareY = (int)Math.Floor(y / 256);
                    starSeed.lastValue = (uint)(((long)squareX * 13 + squareY * 58) & uint.MaxValue);
                    int numberOfStars = Math.Min(8 + ((int)(starSeed.Next() & 0xF00) >> 8), 25); //10 to 25 stars

                    for (int i = 0; i < numberOfStars; i++)
                    {
                        var xc = (float)squareX * 256 + (starSeed.Next() & 255);
                        var yc = (float)squareY * 256 + (starSeed.Next() & 255);
                        g.DrawLine(Pens.White, xc, yc, xc, yc + 1);
                    }
                }
            }

            foreach (var projectile in gs.projectiles)
            {
                g.Transform = oldTransform;
                g.TranslateTransform((float)projectile.posX, (float)projectile.posY);
                g.DrawLines(projectile.uGraphics.color, projectile.uGraphics.points.ToArray());
            }

            foreach (var ship in gs.playerShips.Union(gs.enemyShips))
            {
                g.Transform = oldTransform;
                g.TranslateTransform((float)ship.posX, (float)ship.posY);
                g.RotateTransform((float)(ship.facing / Math.PI * 180));
                
                g.DrawLines(ship.uGraphics.color, ship.uGraphics.points.ToArray());

                //    foreach (var circle in ship.collider.dCircles) {
                //        g.DrawEllipse(Pens.Aqua, (float)(-circle.Radius + circle.X), (float)(-circle.Radius + circle.Y), (float)circle.Radius * 2, (float)circle.Radius * 2);
                //    }
                g.ResetTransform();
            }
        }

        private void registerInputs()
        {
            enterKey = core.RegisterInput(Keys.Enter);
            upArrowKey = core.RegisterInput(Keys.Up);
            downArrowKey = core.RegisterInput(Keys.Down);
            leftArrowKey = core.RegisterInput(Keys.Left);
            rightArrowKey = core.RegisterInput(Keys.Right);
            wKey = core.RegisterInput(Keys.W);
            sKey = core.RegisterInput(Keys.S);
            aKey = core.RegisterInput(Keys.A);
            dKey = core.RegisterInput(Keys.D);
            spaceKey = core.RegisterInput(Keys.Space);
        }

        public class GameState
        {
            public List<ShipBase> playerShips;
            public List<ShipBase> enemyShips;
            public List<Projectile> projectiles;
            public List<BackgroundItem> backitems;
            public ShipBase currentPlayer;

            public GameState()
            {
                playerShips = new List<ShipBase>();
                enemyShips = new List<ShipBase>();
                projectiles = new List<Projectile>();
                backitems = new List<BackgroundItem>();
            }

            public void init()
            {
                var playerShipGfx = new UnitGraphics(Pens.White, new List<PointF>() {
                    new PointF(16,0),
                    new PointF(-16,14),
                    new PointF(-6,0),
                    new PointF(-16,-14),
                    new PointF(16,0)
                });
                var enemyShipGfx = new UnitGraphics(Pens.Red, new List<PointF>() {
                    new PointF(18,0),
                    new PointF(-18,16),
                    new PointF(-6,0),
                    new PointF(-18,-16),
                    new PointF(18,0)
                });
                currentPlayer = new ShipBase(playerShipGfx, 50, 50, new List<PointF>()
                {
                    new PointF(16,0),
                    new PointF(-18,16),
                    new PointF(-18, -16)
                });
                playerShips.Add(currentPlayer);
                enemyShips.Add(new ShipBase(enemyShipGfx, 400, 400, new List<PointF>()
                {
                    new PointF(18,0)
                }));
            }

            public void shooting(bool bulletType, ShipBase ship, int lifeTime, long currentFrame)
            {
                ship.lastFrameFired = currentFrame;
                var bulletGfx = new UnitGraphics(new Pen(Color.FromArgb(255,255,255,255)), new List<PointF>()
                {
                    new PointF(2,0),
                    new PointF(1, 0),
                    new PointF(-2,0),
                    new PointF(0, 1f)
                });
                ship.lastBulletIndex = (ship.lastBulletIndex + 1) % ship.bulletPoints.Count;
                projectiles.Add(new Projectile(bulletType, ship.velocity + 4, bulletGfx, ship.facing, lifeTime, ship.posX + ship.bulletPoints[ship.lastBulletIndex].X, ship.posY + ship.bulletPoints[ship.lastBulletIndex].Y));
            }
        }

        protected GameState gs;

        public void ResetGameState()
        {
            gs = new GameState();
            gs.init();
        }

        public void GameLoop()
        {
            lock (core)
            {
                checkKeys();

                rotateEnemies();

                checkProjectileLifetime();

                foreach (var ship in gs.playerShips.Union(gs.enemyShips))
                {
                    ship.posX += ship.velocity * Math.Cos(ship.angle);
                    ship.posY += ship.velocity * Math.Sin(ship.angle);

                    ship.velocity *= 0.99;
                }

                foreach (var projectile in gs.projectiles)
                {
                    projectile.posX += projectile.velocity * Math.Cos(projectile.angle);
                    projectile.posY += projectile.velocity * Math.Sin(projectile.angle);
                }

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
        } // End of Gameloop

        // Will rotate the enemies based on player position
        public void rotateEnemies()
        {
            foreach (var enemy in gs.enemyShips)
            {
                var targetFacing = Math.Atan2(enemy.posY - gs.currentPlayer.posY, enemy.posX - gs.currentPlayer.posX);

                targetFacing = targetFacing - enemy.facing;
                while (targetFacing < 0)
                    targetFacing += Math.PI * 2;
                while (targetFacing > Math.PI * 2)
                    targetFacing -= Math.PI * 2;

                if (targetFacing < Math.PI - 0.1)
                {
                    enemy.facing -= 0.01;
                }
                if (targetFacing > Math.PI + 0.1)
                {
                    enemy.facing += 0.01;
                }
            }
        }
        // Will check for these keys being used and will perform some action
        public void checkKeys()
        {
            if ((core.GetInputState(leftArrowKey) == InputState.Held) || (core.GetInputState(aKey) == InputState.Held))
            {
                gs.currentPlayer.facing -= 0.037;
            }
            if ((core.GetInputState(rightArrowKey) == InputState.Held) || (core.GetInputState(dKey) == InputState.Held))
            {
                gs.currentPlayer.facing += 0.037;
            }
            if ((core.GetInputState(upArrowKey) == InputState.Held) || (core.GetInputState(wKey) == InputState.Held))
            {
                gs.currentPlayer.applyForce(0.045, gs.currentPlayer.facing);
            }
            if ((core.GetInputState(downArrowKey) == InputState.Held) || (core.GetInputState(sKey) == InputState.Held))
            {
                gs.currentPlayer.velocity *= 0.93;
            }
            if (core.GetInputState(spaceKey) == InputState.Held)
            {
                if (gs.currentPlayer.bulletMode == 0)
                    if (core.frameCounter - gs.currentPlayer.lastFrameFired > 5)
                        gs.shooting(true, gs.currentPlayer, 1000, core.frameCounter);
            }
        }

        // Removes the projectile if the lifetime expires
        public void checkProjectileLifetime()
        {
            for (int i = gs.projectiles.Count - 1; i >=0; i--)
            {
                gs.projectiles[i].lifetime -= 1;
                if (gs.projectiles[i].lifetime <= 0)
                {
                    gs.projectiles.Remove(gs.projectiles[i]);
                }
            }
        }
    }
}
