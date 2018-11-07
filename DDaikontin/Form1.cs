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
using System.Diagnostics;

namespace DDaikontin
{
    public partial class Form1 : Form
    {
        private const ushort GameVersion = 1; //Increment when you make a network-compatibility-breaking change

        private Core core = new Core();
        private GameRenderer<System.Drawing.Drawing2D.Matrix> renderer;
        private PictureBoxArtist artist;
        private InputMappings input;

        private int hitSound = -1;
        private int startSound;
        private int explosionSound;
        private int menuLoopSound;
        private int playerShootSound;
        private int enemyShootSound;
        private int gameplayLoopSound;
        private string baseAssetsPath = "assets/";
        private string baseSoundPath = "sounds/";
        private string baseFontPath = "graphics/vibrocentric-font/";
        private PlayingSoundEffect menuMusic = null;
        private PlayingSoundEffect gameMusic = null;
        private System.Drawing.Text.PrivateFontCollection fontCollection = new System.Drawing.Text.PrivateFontCollection();

        private int[] menuItems = { 4, 1, 1 }; //indexed by core.menuIndex

        public Form1()
        {
            InitializeComponent();
            //Locate assets by looking up one level until it finds them or goes too far
            while (!System.IO.Directory.Exists(baseAssetsPath) && baseAssetsPath.Length < 30) baseAssetsPath = "../" + baseAssetsPath;
            baseSoundPath = baseAssetsPath + baseSoundPath;
            baseFontPath = baseAssetsPath + baseFontPath;

            //Load font
            try
            {
                fontCollection.AddFontFile(baseFontPath + "vibrocentric rg.ttf");
                this.Font = new Font(new FontFamily("vibrocentric", fontCollection), 20, FontStyle.Regular);
            }
            catch (Exception e)
            {
                MessageBox.Show("Font failed to load: " + e.Message);
                //Can continue without the font
            }
            new Thread(() =>
            {
                input = new InputMappings(core);
                artist = new PictureBoxArtist();
                renderer = new GameRenderer<System.Drawing.Drawing2D.Matrix>(core, input, artist, this.Font, pictureBox1.Width, pictureBox1.Height);

                core.GameVersion = GameVersion;
                core.MenuLoop = MenuLoop;
                core.GameLoop = GameLoop;
                core.MenuDraw = pictureBox1.Invalidate;
                core.GameDraw = pictureBox1.Invalidate;
                try
                {
                    hitSound = core.RegisterSound(baseSoundPath + "sound-hit-1.wav");
                    menuLoopSound = core.RegisterSound(baseSoundPath + "song-3.wav");
                    startSound = core.RegisterSound(baseSoundPath + "sound-start-1.wav");
                    explosionSound = core.RegisterSound(baseSoundPath + "sound-death-1.wav");
                    playerShootSound = core.RegisterSound(baseSoundPath + "sound-shot-2.wav");
                    enemyShootSound = core.RegisterSound(baseSoundPath + "sound-shot-3.wav");

                    gameplayLoopSound = core.RegisterSound(baseSoundPath + "song-2.wav");
                }
                catch (Exception e) //TODO: Handle more properly (one sound at a time)
                {
                    MessageBox.Show("One or more sounds failed to load: " + e.Message);
                    //Can continue without audio if we at least have ONE sound--everything will play that sound
                    if (hitSound == -1) throw;
                }

                core.Begin();
                Application.Exit();
            }).Start();
        }

        public void MenuLoop()
        {
            if (menuMusic == null) menuMusic = core.PlaySound(menuLoopSound, true);
            if (input.GetState(input.enterKey) == InputState.JustPressed)
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
                        renderer.creditsScroll = pictureBox1.Height;
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

            if (input.GetState(input.downArrowKey) == InputState.JustPressed) core.menuOption = (core.menuOption + 1) % menuItems[core.menuIndex];
            if (input.GetState(input.upArrowKey) == InputState.JustPressed) core.menuOption = core.menuOption == 0 ? menuItems[core.menuIndex] - 1 : core.menuOption - 1;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            core.Exit();
        }

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
            renderer.gs = gs;
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
                
                //Enemy spawning
                //TODO: You can check this less often
                gs.regionID = (int) Math.Floor(Geometry.DistanceFromOrigin(gs.currentPlayer.posX, gs.currentPlayer.posY) / gs.ringThickness);

                //Generate regions that don't already exist
                //TODO: Instead of bools, they should be lists (initially null references) to hold inactive enemies that were in the region when deactivated.
                while (gs.regionSpawnRecord.Count < gs.regionID + 2) //Note: Generates for 2 regions at the start
                {
                    gs.regionSpawnRecord.Add(new bool[gs.regionSpawnRecord.Count + 2]); //Sector count is equal to regionID + 2
                }

                for (int i = Math.Max(1, gs.regionID - 1); i <= gs.regionID + 1; i++)
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

            var maxDist = 1000;
            //if frame number & 63 is 0
            var subCounter = core.frameCounter & 63;
            if (subCounter == 0)
            {
                //for each enemy
                for (int i = gs.enemyShips.Count - 1; i >= 0; i--)
                {
                    //If enemy is far away
                    if (Math.Abs(gs.enemyShips[i].posX - gs.currentPlayer.posX) >= maxDist && Math.Abs(gs.enemyShips[i].posY - gs.currentPlayer.posY) >= maxDist)
                    {
                        gs.inactivatedEnemies.Add(gs.enemyShips[i]);
                        gs.enemyShips.RemoveAt(i);
                        //deactivate enemy
                    }
                }
            }
            else if (subCounter == 32)
            {
                //else if frame number & 63 is 32
                //for each enemy in inactive list
                for (int i = gs.inactivatedEnemies.Count - 1; i >= 0; i--)
                {
                    //if enemy is close
                    if (Math.Abs(gs.inactivatedEnemies[i].posX - gs.currentPlayer.posX) < maxDist && Math.Abs(gs.inactivatedEnemies[i].posY - gs.currentPlayer.posY) < maxDist)
                    {
                        //activate enemy
                        gs.enemyShips.Add(gs.inactivatedEnemies[i]);
                        gs.inactivatedEnemies.RemoveAt(i);
                    }
                }
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
            var tRand = new PseudoRandom((uint)core.RandomInt(1024));

            var myAngleFromCenter = Geometry.FixAngle(Math.Atan2(gs.currentPlayer.posY, gs.currentPlayer.posX));
            //Sector angles should be smaller for farther-out regions
            //This formula results in generating 100% of region 0, 67% of region 1, 50% of region 2, 40% of region 3, 6.25% of region 30...
            var sectorCount = regionID + 2;
            var sectorAngle = Math.PI * 2 / sectorCount;
            var regionBaseAngle = regionID * 0.1; //Needed so that all the spawn points to the east aren't lined up
            //Round myAngleFromCenter by sectorAngle (always a whole number of sectorAngles per circle)
            var sectorID = (int)((myAngleFromCenter + regionBaseAngle) / sectorAngle) % sectorCount;
            if (regionID == gs.regionID) gs.sectorID = sectorID;

            for (int sector = sectorID - 1; sector <= sectorID + 1; sector++)
            {
                var idx = (sector + sectorCount) % sectorCount;
                if (!gs.regionSpawnRecord[regionID][idx])
                {
                    gs.regionSpawnRecord[regionID][idx] = true;

                    var ringInner = gs.ringThickness * regionID; //Distance from the innermost part of this ring to the origin
                    var angle = idx * sectorAngle - regionBaseAngle;
                    var spawnCount = regionID + 5; //Number of chances to spawn an enemy in this sector
                    var subsectorAngle = sectorAngle / spawnCount; //Angles at which to potentially spawn enemies

                    //Randomly do or don't generate random enemies at each subsectorAngle within (minArc, maxArc)
                    for (int spawnIdx = 0; spawnIdx < spawnCount; spawnIdx++)
                    {
                        tRand.Next();
                        //Locate a random distance from the origin at this angle, but within the region
                        var subPos = tRand.RandomDouble() * gs.ringThickness;
                        var x = Math.Cos(angle) * (ringInner + subPos);
                        var y = Math.Sin(angle) * (ringInner + subPos);
                        gs.generateEnemy(tRand, regionID, x, y, onEnemyFireBasic, onEnemyDamagedBasic, onEnemyDeathBasic);
                        angle += subsectorAngle;
                    }
                }
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
            if ((input.GetState(input.leftArrowKey) == InputState.Held) || (input.GetState(input.aKey) == InputState.Held))
            {
                gs.currentPlayer.facing = Geometry.FixAngle(gs.currentPlayer.facing - 0.037);
            }
            if ((input.GetState(input.rightArrowKey) == InputState.Held) || (input.GetState(input.dKey) == InputState.Held))
            {
                gs.currentPlayer.facing = Geometry.FixAngle(gs.currentPlayer.facing + 0.037);
            }
            if ((input.GetState(input.upArrowKey) == InputState.Held) || (input.GetState(input.wKey) == InputState.Held))
            {
#if DEBUG
                gs.currentPlayer.ApplyForce(0.09, gs.currentPlayer.facing);
#else
                gs.currentPlayer.ApplyForce(0.045, gs.currentPlayer.facing);
#endif
            }
            if ((input.GetState(input.downArrowKey) == InputState.Held) || (input.GetState(input.sKey) == InputState.Held))
            {
                gs.currentPlayer.velocity *= 0.93;
            }
            if (input.GetState(input.spaceKey) == InputState.Held)
            {
                gs.currentPlayer.FireWeapon(600, core.frameCounter);
            }
        }

        public void CheckInGameMenuKeys()
        {
            if (input.GetState(input.escapeKey) == InputState.JustPressed)
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

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            //TODO: On the next frame, resize the PictureBoxArtist
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            lock (core)
            {
                artist.Prepare(e.Graphics);
                if (core.menuIndex < 0) renderer.DrawGame();
                else renderer.DrawMenu();
            }
        }
    }
}
