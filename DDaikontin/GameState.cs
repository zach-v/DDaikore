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
            var playerShipGfx = new UnitGraphics(Pens.White, LineArt.PlayerShip);
            var enemyShipGfx = new UnitGraphics(Pens.Red, LineArt.PlayerShip);
            currentPlayer = new ShipBase(playerShipGfx, Behavior.Player, 3, 6, 50, 50, LineArt.PlayerShootPoints);
            playerShips.Add(currentPlayer);
            //playerShips.Add(new ShipBase(playerShipGfx, Behavior.Player, 3, 6, 100, 50, LineArt.PlayerShootPoints));
            enemyShips.Add(new ShipBase(enemyShipGfx, Behavior.ShootConstantly, 5, 40, 400, 400, LineArt.PlayerShootPoints1));

            var bossCollider = new DCollider(
66, 67, 110,
47, 151, 110,
18, 246, 95,
-20, 334, 70,
-41, 444, 65,
169, 167, 30,
217, 176, 16,
245, 181, 11,
267, 188, 6,
66, 352, 22,
98, 376, 17,
123, 390, 12,
141, 404, 10,
157, 415, 8,
167, 422, 6,
7, 487, 42,
36, 512, 26,
61, 535, 13,
66, -67, 110,
47, -151, 110,
18, -246, 95,
-20, -334, 70,
-41, -444, 65,
169, -167, 30,
217, -176, 16,
245, -181, 11,
267, -188, 6,
66, -352, 22,
98, -376, 17,
123, -390, 12,
141, -404, 10,
157, -415, 8,
167, -422, 6,
7, -487, 42,
36, -512, 26,
61, -535, 13
);

            enemyShips.Add(new ShipBase(
                new UnitGraphics(Pens.Fuchsia, new List<PointF>() {
                    new PointF(150, 0),
                    new PointF(180, 64),
                    new PointF(150, 128),
                    new PointF(280, 192),
                    new PointF(140, 196),
                    new PointF(72, 332),
                    new PointF(170, 430),
                    new PointF(39, 363),
                    new PointF(0, 393),
                    new PointF(70, 550),
                    new PointF(-120, 480),
                    new PointF(-30, 0),
                    new PointF(-120, -480),
                    new PointF(70, -550),
                    new PointF(0, -393),
                    new PointF(39, -363),
                    new PointF(170, -430),
                    new PointF(72, -332),
                    new PointF(140, -196),
                    new PointF(280, -192),
                    new PointF(150, -128),
                    new PointF(180, -64),
                    new PointF(150, 0)
                }),
                Behavior.Boss, 250, 6, 50, -600,
                new List<PointF> {
                    new PointF(180, -64),
                    new PointF(180, 64),
                    new PointF(280, -192),
                    new PointF(280, 192),
                    new PointF(170, -430),
                    new PointF(170, 430),
                })
            { facing = Math.PI / 2,
                collider = bossCollider
            });
        }
    }
}
