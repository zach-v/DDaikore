using DDaikore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDaikontin
{
    /// <summary>
    /// Graphics calculations for DDaikontin; draws using the given Artist object
    /// </summary>
    /// <typeparam name="T">Projection matrix datatype (such as System.Drawing.Drawing2D.Matrix)</typeparam>
    public class GameRenderer<T>
    {
        public Font MenuFont;
        public Core fromCore;

        //Data copied from the Core, necessary for drawing
        long frameCounter;
        int menuIndex;
        int menuOption;

        public InputMappings input;
        public GameState fromGs;
        protected GameState gs;
        public Artist<T> g;
        protected float gameWidth;
        protected float gameHeight;

        protected uint backgroundSeed = (uint)(new Random().Next());

        public float creditsScroll; //Public so you can reset it when changing menus
        protected string[] creditsLines = { "--Credits--", "",
            "Aureuscode", "Mason \"DeProgrammer\" McCoy", "",
            "Snacktivision", "Zach \"SwagDoge\" Vanscoit", "",
            "Base audio engine by Guy Perfect", "",
            "Font by Typodermic Fonts Inc.", "",
            "@ HackSI 2018" };

        public void Resize(float width, float height)
        {
            gameWidth = width;
            gameHeight = height;
        }

        /// <summary>
        /// Prepare to draw--makes copies of necessary data from the Core and GameState so drawing can safely take place during the next processing frame
        /// </summary>
        public void Prepare()
        {
            //Core
            frameCounter = fromCore.frameCounter;
            menuIndex = fromCore.menuIndex;
            menuOption = fromCore.menuOption;

            //GameState
            if (fromGs != null) gs = fromGs.CloneForRenderer();
        }

        /// <summary>
        /// Create a visual rendering object for DDaikontin
        /// </summary>
        /// <param name="gameCore"></param>
        /// <param name="inputs"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="menuFont"></param>
        public GameRenderer(Core gameCore, InputMappings inputs, Artist<T> artist, Font menuFont, int width, int height)
        {
            fromCore = gameCore;
            input = inputs;
            g = artist;
            gameWidth = width;
            gameHeight = height;
            MenuFont = menuFont;
        }

        public void DrawStringHorizontallyCentered(string text, Font font, Brush brush, float x, float y, float width, float height)
        {
            var size = g.MeasureString(text, font, (int)width);
            g.DrawStringRect(text, font, brush, new RectangleF(x + width / 2 - size.Width / 2, y, size.Width + 2, height + 2));
        }

        public void DrawStringCentered(string text, Font font, Brush brush, float x, float y, float width, float height)
        {
            var size = g.MeasureString(text, font, (int)width);
            g.DrawStringRect(text, font, brush, new RectangleF(x + width / 2 - size.Width / 2,
                y + height / 2 - size.Height / 2,
                size.Width + 2, size.Height + 2));
        }

        private void DrawMenuOption(ref int optionIndex, string text, int baseY)
        {
            DrawStringCentered(text, MenuFont, menuOption == optionIndex ? Brushes.White : Brushes.Gray, 0, baseY + optionIndex * 40, gameWidth, 30);
            optionIndex++;
        }

        public void DrawMenu()
        {
            g.BeforeFrame();
            int opt = 0;
            if (menuIndex == 0)
            {
                DrawStringCentered("DDaikontin", new Font(MenuFont.FontFamily, MenuFont.Size + 4f, FontStyle.Bold), Brushes.Aqua,
                    0, 20, gameWidth, 40);
                DrawMenuOption(ref opt, "Play", 60);
                DrawMenuOption(ref opt, "Host Game", 60);
                DrawMenuOption(ref opt, "Join Game", 60);
                DrawMenuOption(ref opt, "Options", 60);
                DrawMenuOption(ref opt, "Credits", 60);
                DrawMenuOption(ref opt, "Exit", 60);
            }
            else if (menuIndex == 1) //Options
            {
                DrawMenuOption(ref opt, "Back", 180);
            }
            else if (menuIndex == 2) //Credits
            {
                creditsScroll--;
                //Loop around
                var lineHeight = (int)g.MeasureString("A", MenuFont, (int)gameWidth).Height + 8;
                if (creditsScroll < -lineHeight * creditsLines.Length) creditsScroll = gameHeight;
                var tScroll = creditsScroll;
                for (int x = 0; x < creditsLines.Length; x++)
                {
                    if (tScroll >= gameHeight - 90) break;
                    DrawStringHorizontallyCentered(creditsLines[x], MenuFont, Brushes.White, 0, tScroll, gameWidth, gameHeight - 90 - tScroll);
                    tScroll += lineHeight;
                }
                DrawMenuOption(ref opt, "Back", (int)gameHeight - 80);
            }
            g.AfterFrame();
        }

        public void DrawGame()
        {
            g.BeforeFrame();
            g.TranslateTransform((float)-gs.currentPlayer.posX + gameWidth / 2, (float)-gs.currentPlayer.posY + gameHeight / 2);
            var oldTransform = g.GetMatrix();

            //Draw a starry background using a predictable pseudorandom number sequence
            var starSeed = new PseudoRandom(backgroundSeed);
            for (double x = gs.currentPlayer.posX - (gameWidth / 2); x < gs.currentPlayer.posX + (gameWidth / 2) + 256; x += 256)
            {
                int squareX = (int)Math.Floor(x / 256);
                for (double y = gs.currentPlayer.posY - (gameHeight / 2); y < gs.currentPlayer.posY + (gameHeight / 2) + 256; y += 256)
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

#if DEBUG
            DrawRegionSector(gs.regionID);
            g.ResetMatrix();
            g.DrawString(String.Format("Sector Area: {0:0.00}", gs.regionSectorArea), new Font(MenuFont.FontFamily, 12), Brushes.Azure, 10, gameHeight - 40);
#endif

            //Draw projectiles
            foreach (var projectile in gs.playerProjectiles.Union(gs.enemyProjectiles))
            {
                g.SetMatrix(oldTransform);
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
                g.SetMatrix(oldTransform);
                g.TranslateTransform((float)ship.posX, (float)ship.posY);
                g.RotateTransform((float)(ship.facing / Math.PI * 180));

                if (ship.lastDamagedFrame <= frameCounter - 8)
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
                g.ResetMatrix();
            }

            if (input.GetState(input.shiftKey) == InputState.Held)
            {
                g.DrawLine(new Pen(Color.FromArgb(255, 0, 39, 45)), (float)(gameWidth / 2 - gs.currentPlayer.posX), (float)(gameHeight / 2 - gs.currentPlayer.posY),
                    gameWidth / 2, gameHeight / 2);
            }
            g.AfterFrame();
        }

#if DEBUG
        protected void DrawRegionSector(int regionID)
        {
            //Figure out which sectors to draw
            var myAngleFromCenter = Geometry.FixAngle(Math.Atan2(gs.currentPlayer.posY, gs.currentPlayer.posX));
            for (var region = Math.Max(regionID - 1, 0); region <= regionID + 1; region++)
            {
                var sectorCount = region + 2;
                var sectorAngle = Math.PI * 2 / sectorCount;
                var regionBaseAngle = region * 0.1; //Needed so that all the spawn points to the east aren't lined up
                var inSector = (int)((myAngleFromCenter + regionBaseAngle) / sectorAngle) % sectorCount;
                for (int sector = inSector - 1; sector <= inSector + 1; sector++)
                {
                    var idx = (sector + sectorCount) % sectorCount;
                    var ringInner = (float)gs.ringThickness * region; //Distance from the innermost part of this ring to the origin
                    var ringOuter = ringInner + (float)gs.ringThickness;
                    var angle = idx * sectorAngle - regionBaseAngle;
                    var maxAngle = angle + sectorAngle;

                    var spawnCount = region + 5; //Number of chances to spawn an enemy in this sector
                    var subsectorAngle = sectorAngle / spawnCount; //Angles at which to potentially spawn enemies
                    for (var drawAngle = angle; drawAngle <= maxAngle; drawAngle += subsectorAngle)
                    {
                        var cos = (float)Math.Cos(drawAngle);
                        var sin = (float)Math.Sin(drawAngle);
                        //Draw from the inside to the outside of this region
                        var x1 = cos * ringInner;
                        var y1 = sin * ringInner;
                        var x2 = cos * ringOuter;
                        var y2 = sin * ringOuter;

                        var drawingPen = Pens.DarkSlateBlue; //Surrounding sectors -> blue
                        if (idx == inSector && region == regionID) drawingPen = Pens.DarkSalmon; //The region and sector the player is in -> pink
                        else if (idx == inSector) drawingPen = Pens.DarkGreen; //One region closer or farther from the origin than the player currently is -> green
                        if (drawAngle == angle) drawingPen = new Pen(drawingPen.Color, 3);
                        g.DrawLine(drawingPen, x1, y1, x2, y2);

                        //Inform the developer of the sector's area
                        if (region == regionID && sector == inSector)
                        {
                            gs.regionSectorArea = (ringOuter * ringOuter - ringInner * ringInner) * Math.PI / sectorCount / 1000000; //Final division so we have smallish numbers instead of pixels
                            //Looks like this stabilizes around 6 (region 32)... at region 51, it's still only 6.11.
                        }
                    }
                }
            }
        }
#endif


    }
}
