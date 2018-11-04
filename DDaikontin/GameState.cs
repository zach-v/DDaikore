using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using DDaikore;

namespace DDaikontin
{
    public class GameState
    {
        public List<ShipBase> playerShips;
        public List<ShipBase> enemyShips;
        public List<Projectile> playerProjectiles;
        public List<Projectile> enemyProjectiles;

        UnitGraphics playerShipGfx = new UnitGraphics(Pens.White, LineArt.PlayerShip);
        UnitGraphics enemyShipGfx = new UnitGraphics(Pens.Red, LineArt.PlayerShip);
        UnitGraphics enemy2ShipGfx = new UnitGraphics(Pens.Red, LineArt.EnemyShip2.Select(p => new PointF(p.X * 2f, p.Y * 2f)).ToList());

        public List<Tuple<double, ShipBase>> enemiesPossible;
        public ShipBase currentPlayer;

        public List<List<Tuple<double, double>>> regionSpawnRecord = new List<List<Tuple<double, double>>>();

        public GameState()
        {
            playerShips = new List<ShipBase>();
            enemyShips = new List<ShipBase>();
            playerProjectiles = new List<Projectile>();
            enemyProjectiles = new List<Projectile>();
        }

        public void init()
        {
            currentPlayer = new ShipBase(playerShipGfx, Behavior.Player, 3, 6, 50, 50, LineArt.PlayerShootPoints);
            playerShips.Add(currentPlayer);
            
            /*
            enemyShips.Add(new ShipBase(enemyShipGfx, Behavior.ShootConstantly, 5, 40, 400, 400, LineArt.PlayerShootPoints1));
            enemyShips.Add(new ShipBase(new UnitGraphics(Pens.Fuchsia, LineArt.BossShip),
                Behavior.Boss, 250, 6, 50, -600, LineArt.BossBulletPoints)
            { facing = Math.PI / 2, collider = new DCollider(LineArt.BossColliders) });
            enemyShips.Add(new ShipBase(enemy2ShipGfx, Behavior.SpinShoot, 5, 5, -400, -400, LineArt.EnemyShip2_ShootPoints.Select(p => new PointF(p.X * 2f, p.Y * 2f)).ToList()));
            /**/
        }

        public void generateEnemy(PseudoRandom tRand, int regionID, double x, double y)
        {
            double val = tRand.RandomDouble();
            if (val > 0.3)
                enemyShips.Add(new ShipBase(enemyShipGfx, Behavior.ShootConstantly, 5, 40, 400, 400, LineArt.PlayerShootPoints1));
            else if (val <= 0.3)
                enemyShips.Add(new ShipBase(enemy2ShipGfx, Behavior.SpinShoot, 5, 5, -400, -400, LineArt.EnemyShip2_ShootPoints.Select(p => new PointF(p.X * 2f, p.Y * 2f)).ToList()));
        }
    }
}
