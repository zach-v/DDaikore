﻿using System;
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

        private readonly double ringThickness = 1000;

        private uint backgroundSeed = (uint)(new Random().Next());
        private int hitSound;
        private int startSound;
        private int explosionSound;
        private int menuLoopSound;
        private int playerShootSound;
        private int enemyShootSound;
        private int gameplayLoopSound;
        private string baseSoundPath = "../../../assets/sounds/";
        private PlayingSoundEffect menuMusic = null;
        private PlayingSoundEffect gameMusic = null;

        private int[] menuItems = { 4, 1, 1 }; //indexed by core.menuIndex
        private float creditsScroll;
        private string[] creditsLines = { "--Credits--", "",
            "Aureuscode", "Mason \"DeProgrammer\" McCoy", "",
            "Snacktivision", "Zach \"SwagDoge\" Vanscoit", "",
            "@ HackSI 2018" };

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
                playerShootSound = core.RegisterSound(baseSoundPath + "sound-shot-2.wav");
                enemyShootSound = core.RegisterSound(baseSoundPath + "sound-shot-3.wav");

                gameplayLoopSound = core.RegisterSound(baseSoundPath + "song-2.wav");

                registerInputs();

                core.Begin();
                Application.Exit();
            }).Start();
        }

        #region Inputs
        private int enterKey;
        private int upArrowKey;
        private int downArrowKey;
        private int leftArrowKey;
        private int rightArrowKey;
        private int wKey;
        private int sKey;
        private int aKey;
        private int dKey;
        private int spaceKey;
        private int escapeKey;
        private int shiftKey;
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
            shiftKey = core.RegisterInput(Keys.LShiftKey);
        }
        #endregion

        public void MenuLoop()
        {
            if (menuMusic == null) menuMusic = core.PlaySound(menuLoopSound, true);
            if (core.GetInputState(enterKey) == InputState.JustPressed)
            {
                if (core.menuIndex == 0) //Main menu
                {
                    if (core.menuOption == 0)
                    {
                        ResetGameState();
                        core.menuIndex = -1;

                        core.PlaySound(startSound);
                        //Stop the menu music and set it to null so it can play again if the menu loop ever gets called again
                        menuMusic.stopSound();
                        menuMusic = null;
                    }
                    else if (core.menuOption == 1)
                    {
                        core.menuIndex = 1; //Options menu
                        core.menuOption = 0;
                    }
                    else if (core.menuOption == 2)
                    {
                        core.menuIndex = 2; //Credits menu
                        core.menuOption = 0;
                        creditsScroll = pictureBox1.Height;
                    }
                    else if (core.menuOption == 3)
                    {
                        core.Exit();
                    }
                }
                else if (core.menuIndex == 1) //Options menu
                {
                    core.menuIndex = 0;
                    core.menuOption = 1;
                }
                else if (core.menuIndex == 2) //Credits menu
                {
                    core.menuIndex = 0;
                    core.menuOption = 2;
                }
            }

            if (core.GetInputState(downArrowKey) == InputState.JustPressed) core.menuOption = (core.menuOption + 1) % menuItems[core.menuIndex];
            if (core.GetInputState(upArrowKey) == InputState.JustPressed) core.menuOption = core.menuOption == 0 ? menuItems[core.menuIndex] - 1 : core.menuOption - 1;
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

        private void DrawStringHorizontallyCentered(Graphics g, string text, Font font, Brush brush, float x, float y, float width, float height)
        {
            var size = g.MeasureString(text, font, (int)width);
            g.DrawString(text, font, brush, new RectangleF(x + width / 2 - size.Width / 2, y, size.Width + 2, height + 2));
        }

        private void DrawStringCentered(Graphics g, string text, Font font, Brush brush, float x, float y, float width, float height)
        {
            var size = g.MeasureString(text, font, (int)width);
            g.DrawString(text, font, brush, new RectangleF(x + width / 2 - size.Width / 2, 
                y + height / 2 - size.Height / 2, 
                size.Width + 2, size.Height + 2));
        }

        private void drawMenu(Graphics g)
        {
            if (core.menuIndex == 0)
            {
                DrawStringCentered(g, "DDaikontin", new Font(Font.FontFamily, Font.Size + 4f, FontStyle.Bold), Brushes.Aqua,
                    0, 20, pictureBox1.Width, 40);
                DrawStringCentered(g, "Play", this.Font, core.menuOption == 0 ? Brushes.White : Brushes.Gray, 0, 60, pictureBox1.Width, 30);
                DrawStringCentered(g, "Options", this.Font, core.menuOption == 1 ? Brushes.White : Brushes.Gray, 0, 100, pictureBox1.Width, 30);
                DrawStringCentered(g, "Credits", this.Font, core.menuOption == 2 ? Brushes.White : Brushes.Gray, 0, 140, pictureBox1.Width, 30);
                DrawStringCentered(g, "Exit", this.Font, core.menuOption == 3 ? Brushes.White : Brushes.Gray, 0, 180, pictureBox1.Width, 30);
            }
            else if (core.menuIndex == 1) //Options
            {
                DrawStringCentered(g, "Back", this.Font, core.menuOption == 0 ? Brushes.White : Brushes.Gray, 0, 180, pictureBox1.Width, 30);
            }
            else if (core.menuIndex == 2) //Credits
            {
                creditsScroll--;
                //Loop around
                var lineHeight = (int)g.MeasureString("A", Font, pictureBox1.Width).Height + 8;
                if (creditsScroll < -lineHeight * creditsLines.Length) creditsScroll = pictureBox1.Height;
                var tScroll = creditsScroll;
                for (int x = 0; x < creditsLines.Length; x++)
                {
                    if (tScroll >= pictureBox1.Height - 90) break;
                    DrawStringHorizontallyCentered(g, creditsLines[x], Font, Brushes.White, 0, tScroll, pictureBox1.Width, pictureBox1.Height - 90 - tScroll);
                    tScroll += lineHeight;
                }
                DrawStringCentered(g, "Back", Font, core.menuOption == 0 ? Brushes.White : Brushes.Gray, 0, pictureBox1.Height - 80, pictureBox1.Width, 40);
            }
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

#if DEBUG
                foreach (var circle in projectile.collider.dCircles)
                {
                    g.DrawEllipse(Pens.Aqua, (float)(-circle.Radius + circle.X), (float)(-circle.Radius + circle.Y),
                        (float)circle.Radius * 2, (float)circle.Radius * 2);
                }
#endif
            }

            //Draw ships (and in debug mode, draw their colliders)
            foreach (var ship in gs.playerShips.Union(gs.enemyShips).Where(p => p.isAlive))
            {
                g.Transform = oldTransform;
                g.TranslateTransform((float)ship.posX, (float)ship.posY);
                g.RotateTransform((float)(ship.facing / Math.PI * 180));

                if (ship.lastDamagedFrame <= core.frameCounter - 8)
                {
                    g.DrawLines(ship.uGraphics.color, ship.uGraphics.points.ToArray());
                }
                else //Invert ship color when recently damaged
                {
                    var tempColor = ship.uGraphics.color.Color.ToArgb();
                    var tempPen = new Pen(Color.FromArgb(tempColor ^ 0x00FFFFFF));
                    g.DrawLines(tempPen, ship.uGraphics.points.ToArray());
                }

#if DEBUG
                foreach (var circle in ship.collider.dCircles)
                {
                    g.DrawEllipse(Pens.Aqua, (float)(-circle.Radius + circle.X), (float)(-circle.Radius + circle.Y), 
                        (float)circle.Radius * 2, (float)circle.Radius * 2);
                }
#endif
                g.ResetTransform();
            }

            if (core.GetInputState(shiftKey) == InputState.Held)
            {
                g.DrawLine(new Pen(Color.FromArgb(255,0,39,45)), (float) (pictureBox1.Width / 2 - gs.currentPlayer.posX), (float) (pictureBox1.Height / 2 - gs.currentPlayer.posY),
                    pictureBox1.Width / 2, pictureBox1.Height / 2);
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
                    var vol = 0.4f + (float)new Random().NextDouble() * 0.4f;
                    core.PlaySound(playerShootSound, false, vol);
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
                    core.PlaySound(enemyShootSound);
                };
            }
        }

        public void GameLoop()
        {
            lock (core)
            {
                if (gameMusic == null) gameMusic = core.PlaySound(gameplayLoopSound, true, 0.6f);

                if (gs.currentPlayer.isAlive) CheckGameKeys();
                CheckInGameMenuKeys();

                //rotateEnemies(); //TODO: Move to enemy ship's Process function. Need a behavior enum.

                checkProjectileLifetime();
                
                var regionID = (int) Math.Floor(Geometry.DistanceFromOrigin(gs.currentPlayer.posX, gs.currentPlayer.posY) / ringThickness);
                while (gs.regionSpawnRecord.Count < regionID + 2)       // NOTE: Generates 2 regions at the start
                {
                    gs.regionSpawnRecord.Add(new List<Tuple<double, double>>());
                }

                for (int i = Math.Max(1, regionID - 1); i <= regionID + 1; i++)
                {
                    GenerateFoesForRegion(i);
                }

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
                            ship.Damage(projectile.damage, core.frameCounter);
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
                            ship.Damage(projectile.damage, core.frameCounter);
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
                            gs.playerShips[x].Damage(1, core.frameCounter);
                            gs.playerShips[y].Damage(1, core.frameCounter);
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
                            ship.Damage(1, core.frameCounter);
                            foe.Damage(20, core.frameCounter);
                        }
                        //TODO: Deactivate enemies that are far away; you can move them to a separate list and check less frequently to see if they should be readded to active list
                    }
                }
                //TODO: Other collisions, player inputs, stuff, things, etc.
            }
        } // End of Gameloop

        public void onEnemyDamagedBasic()
        {
            core.PlaySound(hitSound);
        }

        public void onEnemyDeathBasic()
        {
            core.PlaySound(explosionSound);
        }

        public void onEnemyFireBasic(List<Projectile> bullets)
        {
            gs.enemyProjectiles.AddRange(bullets);
            core.PlaySound(enemyShootSound);
        }

        protected void GenerateFoesForRegion(int regionID)
        {
            var myAngleFromCenter = Math.Atan2(gs.currentPlayer.posY, gs.currentPlayer.posX);
            //Sector angles should be smaller for farther-out regions
            //This formula results in generating 100% of region 0, 67% of region 1, 50% of region 2, 40% of region 3, 6.25% of region 30...
            var sectorAngle = Math.PI * 2 / (regionID + 2);
            //Round myAngleFromCenter by sectorAngle (always a whole number of sectorAngles per circle)
            myAngleFromCenter = Geometry.FixAngle(Math.Round(myAngleFromCenter / sectorAngle) * sectorAngle);
            var minArc = Geometry.FixAngle(myAngleFromCenter - sectorAngle); //Also a rounded-off angle
            var maxArc = Geometry.FixAngle(myAngleFromCenter + sectorAngle - 0.00000001); //Ditto
            var subsectorAngle = sectorAngle / (regionID + 5); //Number of chances to spawn an enemy in this sector

            //Check if any arcs intersect the one we want to spawn for
            foreach (var arc in gs.regionSpawnRecord[regionID])
            {
                //TODO: If the arc intersects (minArc, maxArc), remove the intersecting piece from (minArc, maxArc).
                //Don't forget to account for wrapping back to 0.
                minArc = maxArc; //TESTING ONLY. This is temporary until we have the arc intersection logic done. This means we'll only spawn enemies in one arc per ring...ever.
            }
            if (minArc == maxArc) return; //Nothing to do; no-one to spawn

            //TODO: Then combine the two arcs if any other arc was touching this one.
            // else //if no arc was intersecting/touching (minArc, maxArc)
            gs.regionSpawnRecord[regionID].Add(new Tuple<double, double>(minArc, maxArc));

            if (maxArc < minArc) maxArc += Math.PI * 2; //Make sure maxArc is bigger than minArc for easier logic
            var tRand = new PseudoRandom((uint) core.RandomInt(1024));
            var ringInner = ringThickness * regionID; //Distance from the innermost part of this ring to the origin
            //Randomly do or don't generate random enemies at each subsectorAngle within (minArc, maxArc)
            for (double angle = minArc; angle < maxArc; angle += subsectorAngle)
            {
                tRand.Next();
                //Locate a random distance from the origin at this angle, but within the region
                var subPos = tRand.RandomDouble() * ringThickness;
                var x = Math.Cos(angle) * (ringInner + subPos);
                var y = Math.Sin(angle) * (ringInner + subPos);
                gs.generateEnemy(tRand, regionID, x, y, onEnemyFireBasic, onEnemyDamagedBasic, onEnemyDeathBasic);
            }
        }

        // Will rotate the enemies based on player position
        //Avoid doing things based on player input like this, for network multiplayer's sake
        public void rotateEnemies()
        {
            foreach (var enemy in gs.enemyShips)
            {
                var targetFacing = Geometry.Face(enemy.posX, enemy.posY, gs.currentPlayer.posX, gs.currentPlayer.posY);

                targetFacing = targetFacing - enemy.facing;
                targetFacing = Geometry.FixAngle(targetFacing);

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
                gameMusic.stopSound();
                gameMusic = null;
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
