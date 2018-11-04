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
        public List<Projectile> playerProjectiles;
        public List<Projectile> enemyProjectiles;
        public ShipBase currentPlayer;

        public GameState()
        {
            playerShips = new List<ShipBase>();
            enemyShips = new List<ShipBase>();
            playerProjectiles = new List<Projectile>();
            enemyProjectiles = new List<Projectile>();
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
            currentPlayer = new ShipBase(playerShipGfx, 3, 50, 50, new List<PointF>()
                {
                    new PointF(16,0),
                    new PointF(-18,16),
                    new PointF(-18, -16)
                });
            playerShips.Add(currentPlayer);
            enemyShips.Add(new ShipBase(enemyShipGfx, 5, 400, 400, new List<PointF>()
                {
                    new PointF(18,0)
                }));
        }

        public void Shoot(ShipBase ship, int lifeTime, long currentFrame, int bulletType = -1)
        {
            var newProjectiles = ship.FireWeapon(lifeTime, currentFrame, bulletType);
            if (playerShips.Contains(ship)) playerProjectiles.AddRange(newProjectiles);
            else enemyProjectiles.AddRange(newProjectiles);
        }
    }
}
