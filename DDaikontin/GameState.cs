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
        public List<ShipBase> inactivatedEnemies = new List<ShipBase>();

        /// <summary>
        /// Thickness of each region (difference between inner and outer radius)
        /// </summary>
        public readonly double ringThickness = 1000;
        /// <summary>
        /// Last region the player was in when it was calculated
        /// </summary>
        public int regionID;
        /// <summary>
        /// Last sector the player was in when it was calculated
        /// </summary>
        public int sectorID;

#if DEBUG
        public double regionSectorArea;
#endif

        UnitGraphics playerShipGfx = new UnitGraphics(Pens.White, LineArt.PlayerShip);
        UnitGraphics enemyShipGfx = new UnitGraphics(Pens.Red, LineArt.PlayerShip);
        UnitGraphics enemy2ShipGfx = new UnitGraphics(Pens.Red, LineArt.EnemyShip2.Select(p => new PointF(p.X * 2f, p.Y * 2f)).ToList());

        public List<Tuple<double, ShipBase>> enemiesPossible;
        public ShipBase currentPlayer;

        public List<bool[]> regionSpawnRecord = new List<bool[]>();

        public GameState()
        {
            playerShips = new List<ShipBase>();
            enemyShips = new List<ShipBase>();
            playerProjectiles = new List<Projectile>();
            enemyProjectiles = new List<Projectile>();
        }

        public void init()
        {
            currentPlayer = new ShipBase(playerShipGfx, Behavior.Player,
#if DEBUG
                50,
#else
                5,
#endif
                6, 0, 0, LineArt.PlayerShootPoints);
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
