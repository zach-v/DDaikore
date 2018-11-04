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
            currentPlayer = new ShipBase(playerShipGfx, Behavior.Player, 3, 6, 0, 0, LineArt.PlayerShootPoints);
            playerShips.Add(currentPlayer);

            /*
            enemyShips.Add(new ShipBase(enemyShipGfx, Behavior.ShootConstantly, 5, 40, 400, 400, LineArt.PlayerShootPoints1));
            enemyShips.Add(new ShipBase(enemy2ShipGfx, Behavior.SpinShoot, 5, 5, -400, -400, LineArt.EnemyShip2_ShootPoints.Select(p => new PointF(p.X * 2f, p.Y * 2f)).ToList()));
            enemyShips.Add(new ShipBase(new UnitGraphics(Pens.Fuchsia, LineArt.BossShip),
                Behavior.Boss, 250, 6, 50, -600, LineArt.BossBulletPoints)
            { facing = Math.PI / 2, collider = new DCollider(LineArt.BossColliders) });
            /**/
        }

        public void generateEnemy(PseudoRandom tRand, int regionID, double x, double y, Action<List<Projectile>> OnFire, Action OnDamage, Action OnDeath)
        {
            double val = tRand.RandomDouble();
            if (val > 0.2)
                return;
            else if (val > 0.05)
                enemyShips.Add(new ShipBase(enemyShipGfx, Behavior.ShootConstantly, regionID / 3 + 2, 40, x, y, LineArt.PlayerShootPoints1) { OnWeaponFire = OnFire });
            else if (val > 0.005)
                enemyShips.Add(new ShipBase(enemy2ShipGfx, Behavior.SpinShoot, regionID / 3 + 4, 5, x, y, LineArt.EnemyShip2_ShootPoints.Select(p => new PointF(p.X * 2f, p.Y * 2f)).ToList()) { OnWeaponFire = OnFire });
            else 
                enemyShips.Add(new ShipBase(new UnitGraphics(Pens.Fuchsia, LineArt.BossShip), Behavior.Boss, regionID / 3 + 150, 6, x, y, LineArt.BossBulletPoints)
                { facing = Math.PI / 2, collider = new DCollider(LineArt.BossColliders), OnWeaponFire = OnFire });
        }
    }
}
