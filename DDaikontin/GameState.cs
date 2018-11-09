using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using DDaikore;
using System.IO;

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

        /// <summary>
        /// Make a clone of the minimum data necessary to run the renderer concurrently with the game processing
        /// </summary>
        /// <returns></returns>
        public GameState CloneForRenderer()
        {
            return new GameState() {
                regionID = regionID,
                sectorID = sectorID,
                regionSectorArea = regionSectorArea,
                playerShips = playerShips.Where(p => p.isAlive).Select(p => p.CloneForRenderer()).ToList(),
                enemyShips = enemyShips.Where(p => p.isAlive).Select(p => p.CloneForRenderer()).ToList(),
                playerProjectiles = playerProjectiles.Where(p => p.lifetime > 0).Select(p => p.CloneForRenderer()).ToList(),
                enemyProjectiles = enemyProjectiles.Where(p => p.lifetime > 0).Select(p => p.CloneForRenderer()).ToList(),
                currentPlayer = currentPlayer.CloneForRenderer()
            };
        }

        /// <summary>
        /// Receive a comm message, rectify differences, and build a response message immediately
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public byte[] ReceiveMessage(byte[] message)
        {
            //TODO: if message length is 0, this is the first message, so just respond with the current state of affairs
            //TODO: generate response and return it

            return SerializeForComm();
        }

        /// <summary>
        /// Serialize the game state for the sake of comm transmission (excludes info about other players)
        /// </summary>
        /// <returns></returns>
        public byte[] SerializeForComm()
        {
            var response = new byte[1024 * 1024]; //1 MB max
            var s = new CommMessageStreamWriter(response);
            s.Write(currentPlayer.health); //TODO: Make a serializer method in the ShipBase class
            s.Write(currentPlayer.angle);
            s.Write(currentPlayer.facing);
            s.Write(currentPlayer.velocity);
            s.Write(currentPlayer.posX);
            s.Write(currentPlayer.posY);
            //TODO: Also send key states 'n' stuff
            //TODO: I'd really like to avoid sending anything that can be computed easily--that does not include currentPlayer's projectiles!
            //To avoid sending: anything about enemies other than how much currentPlayer damaged them and when (need to track damage a player took when and after they 'died' in case the other player did something to prevent the death and we find out about it 120 frames later)
            //s.Write(playerProjectiles.Count); //TODO: track new player projectiles; send only the new ones
            //TODO: Send a "going to spawn/activate enemies for region X sector Y at frame Z" message, and then do what it says
            //TODO: Soft-tether the players. If player A goes out of range of the leader, a force applies to player A on player A's machine until they're in range again, and it syncs the same as that player's position normally does.


            return response;
        }
    }
}
