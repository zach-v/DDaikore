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
        private const ushort GameVersion = 1; //Increment when you make a network-compatibility-breaking change

        private Core core = new Core();

        private uint backgroundSeed = (uint)(new Random().Next());
        private int hitSound = 0;
        private int startSound = 0;
        private int explosionSound = 0;
        private int menuLoopSound = 0;
        private int playerShootSound = 0;
        private string baseSoundPath = "../../../assets/sounds/";
        private PlayingSoundEffect menuMusic = null;

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
                menuLoopSound = core.RegisterSound(baseSoundPath + "song-3.wav");
                hitSound = core.RegisterSound(baseSoundPath + "sound-hit-1.wav");
                startSound = core.RegisterSound(baseSoundPath + "sound-start-1.wav");
                explosionSound = core.RegisterSound(baseSoundPath + "sound-death-1.wav");
                playerShootSound = core.RegisterSound(baseSoundPath + "sound-shot-1.wav");

                registerInputs();

                core.Begin();
            }).Start();
        }

        #region Inputs
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
        private int escapeKey = 0;
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
            escapeKey = core.RegisterInput(Keys.Escape);
        }
        #endregion

        public void MenuLoop()
        {
            if (menuMusic == null) menuMusic = core.PlaySound(menuLoopSound, true);
            if (core.GetInputState(enterKey) == InputState.JustPressed)
            {
                ResetGameState();
                core.menuIndex = -1;

                //Stop the menu music and set it to null so it can play again if the menu loop ever gets called again
                menuMusic.stopSound();
                menuMusic = null;

                core.PlaySound(startSound);
            }
            else if (core.GetInputState(upArrowKey) == InputState.JustPressed) core.menuOption = (core.menuOption + 1) % 2;
            else if (core.GetInputState(downArrowKey) == InputState.JustPressed) core.menuOption = core.menuOption == 0 ? 1 : core.menuOption - 1;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            core.Exit();
        }

        #region Graphics
        public void MenuDraw()
        {
            pictureBox1.Invalidate();
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

            //Draw a starry background using a predictable pseudorandom number sequence
            var starSeed = new PseudoRandom(backgroundSeed);
            for (double x = gs.currentPlayer.posX - (pictureBox1.Width / 2); x < gs.currentPlayer.posX + (pictureBox1.Width / 2) + 256; x += 256)
            {
                int squareX = (int)Math.Floor(x / 256);
                for (double y = gs.currentPlayer.posY - (pictureBox1.Height / 2); y < gs.currentPlayer.posY + (pictureBox1.Height / 2) + 256; y += 256)
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

            //Draw projectiles
            foreach (var projectile in gs.playerProjectiles.Union(gs.enemyProjectiles))
            {
                g.Transform = oldTransform;
                g.TranslateTransform((float)projectile.posX, (float)projectile.posY);
                g.DrawLines(projectile.uGraphics.color, projectile.uGraphics.points.ToArray());
            }

            //Draw ships (and in debug mode, draw their colliders)
            foreach (var ship in gs.playerShips.Union(gs.enemyShips).Where(p => p.isAlive))
            {
                g.Transform = oldTransform;
                g.TranslateTransform((float)ship.posX, (float)ship.posY);
                g.RotateTransform((float)(ship.facing / Math.PI * 180));
                
                g.DrawLines(ship.uGraphics.color, ship.uGraphics.points.ToArray());

#if DEBUG
                //    foreach (var circle in ship.collider.dCircles) {
                //        g.DrawEllipse(Pens.Aqua, (float)(-circle.Radius + circle.X), (float)(-circle.Radius + circle.Y), (float)circle.Radius * 2, (float)circle.Radius * 2);
                //    }
#endif
                g.ResetTransform();
            }
        }
        #endregion

        protected GameState gs;

        public void ResetGameState()
        {
            gs = new GameState();
            gs.init();
            foreach (var ship in gs.playerShips)
            {
                ship.OnDamaged = () =>
                {
                    core.PlaySound(hitSound);
                };
                ship.OnDeath = () => {
                    core.PlaySound(explosionSound);
                };
                ship.OnWeaponFire = (bullets) =>
                {
                    gs.playerProjectiles.AddRange(bullets);
                    core.PlaySound(playerShootSound);
                };
            }
            foreach (var ship in gs.enemyShips)
            {
                ship.OnDamaged = () => {
                    core.PlaySound(hitSound);
                };
                ship.OnDeath = () => {
                    core.PlaySound(explosionSound);
                };
                ship.OnWeaponFire = (bullets) => {
                    gs.enemyProjectiles.AddRange(bullets);
                };
            }
        }

        public void GameLoop()
        {
            lock (core)
            {
                if (gs.currentPlayer.isAlive) CheckGameKeys();
                CheckInGameMenuKeys();

                rotateEnemies(); //TODO: Move to enemy ship's Process function. Need a behavior enum.

                checkProjectileLifetime();

                foreach (var ship in gs.playerShips.Union(gs.enemyShips).Where(p => p.isAlive))
                {
                    ship.Process(core.frameCounter);
                }

                foreach (var projectile in gs.playerProjectiles)
                {
                    projectile.Process(core.frameCounter);
                    //Check bullet collision with enemies
                    foreach (var ship in gs.enemyShips.Where(p => p.isAlive))
                    {
                        if (ship.CollidesWith(projectile))
                        {
                            ship.Damage(projectile.damage);
                            projectile.Kill();
                        }
                    }
                }

                foreach (var projectile in gs.enemyProjectiles)
                {
                    projectile.Process(core.frameCounter);
                    //Check bullet collision with players
                    foreach (var ship in gs.playerShips.Where(p => p.isAlive))
                    {
                        if (ship.CollidesWith(projectile))
                        {
                            ship.Damage(projectile.damage);
                            projectile.Kill();
                        }
                    }
                }

                //Check if players ran into each other
                for (var x = 0; x < gs.playerShips.Count; x++)
                {
                    if (!gs.playerShips[x].isAlive) continue;
                    for (var y = x + 1; y < gs.playerShips.Count; y++)
                    {
                        if (!gs.playerShips[y].isAlive) continue;
                        if (gs.playerShips[x].CollidesWith(gs.playerShips[y]))
                        {
                            //Make ships bounce apart (get angle between ships and send them in opposite directions)
                            var targetAngle = Geometry.Face(gs.playerShips[x].posX, gs.playerShips[x].posY, gs.playerShips[y].posX, gs.playerShips[y].posY);
                            gs.playerShips[x].ApplyForce(5, targetAngle);
                            gs.playerShips[y].ApplyForce(5, -targetAngle);
                            gs.playerShips[x].Damage(1);
                            gs.playerShips[y].Damage(1);
                        }
                    }
                }

                //Check if players ran into enemies
                foreach (var ship in gs.playerShips.Where(p => p.isAlive))
                {
                    foreach (var foe in gs.enemyShips.Where(p => p.isAlive))
                    {
                        if (ship.CollidesWith(foe))
                        {
                            ship.Damage(1);
                            foe.Damage(20);
                        }
                        //TODO: Deactivate enemies that are far away; you can move them to a separate list and check less frequently to see if they should be readded to active list
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
                var targetFacing = Geometry.Face(enemy.posX, enemy.posY, gs.currentPlayer.posX, gs.currentPlayer.posY);

                targetFacing = targetFacing - enemy.facing;
                while (targetFacing < 0)
                    targetFacing += Math.PI * 2;
                while (targetFacing > Math.PI * 2)
                    targetFacing -= Math.PI * 2;

                if (targetFacing < Math.PI - 0.01)
                {
                    enemy.facing -= 0.01;
                }
                if (targetFacing > Math.PI + 0.01)
                {
                    enemy.facing += 0.01;
                }
            }
        }

        // Will check for these keys being used and will perform some action pertaining to gameplay
        public void CheckGameKeys()
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
                gs.currentPlayer.ApplyForce(0.045, gs.currentPlayer.facing);
            }
            if ((core.GetInputState(downArrowKey) == InputState.Held) || (core.GetInputState(sKey) == InputState.Held))
            {
                gs.currentPlayer.velocity *= 0.93;
            }
            if (core.GetInputState(spaceKey) == InputState.Held)
            {
                gs.currentPlayer.FireWeapon(700, core.frameCounter);
            }
        }

        public void CheckInGameMenuKeys()
        {
            if (core.GetInputState(escapeKey) == InputState.JustPressed)
            {
                //Return to menu!
                core.menuIndex = 0;
                core.menuOption = 0;
            }
        }

        // Removes the projectile if the lifetime expires
        public void checkProjectileLifetime()
        {
            //Each list has to be processed separately--the players' list and the enemies' list
            foreach (var list in new List<List<Projectile>> { gs.playerProjectiles, gs.enemyProjectiles })
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    list[i].lifetime -= 1;
                    if (list[i].lifetime <= 0) list.RemoveAt(i);
                }
            }
        }
    }
}
