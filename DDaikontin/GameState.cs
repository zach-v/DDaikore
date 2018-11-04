using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace DDaikontin
{
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
            var bulletGfx = new UnitGraphics(new Pen(Color.FromArgb(255, 255, 255, 255)), new List<PointF>()
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
}
