using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace DDaikontin
{
    public enum Behavior
    {
        Player,
        ShootConstantly,

    }

    public class ShipBase : Body
    {
        /// <summary>
        /// Locations relative from which bullets can be fired, relative to the ship
        /// </summary>
        public List<PointF> bulletPoints;
        public int lastBulletIndex = 0;
        public int framesBetweenBullets = 5; //TODO: Look up bulletRate by bullet type? Pass it in? What if they switch weapons?
        public long lastFrameFired = 0;

        public int bulletMode = 0;
        public int health = 3;

        public Behavior behavior = Behavior.Player;

        public UnitGraphics uGraphics;
        /// <summary>
        /// Action to take when unit is killed
        /// </summary>
        public Action OnDeath;
        /// <summary>
        /// Action to take when unit is damaged but not killed
        /// </summary>
        public Action OnDamaged;
        /// <summary>
        /// Action to take when the unit attempts to shoot
        /// </summary>
        public Action<List<Projectile>> OnWeaponFire;
        
        public bool isAlive {
            get { return health != 0; }
            set { health = value ? 1 : 0; }
        }

        public ShipBase(UnitGraphics uGraphics, Behavior behavior, int health, int framesBetweenBullets, double posX, double posY, List<PointF> bulletPoints)
        {
            this.uGraphics = uGraphics;
            this.collider = new DCollider(uGraphics);
            this.bulletPoints = bulletPoints;
            this.posX = posX;
            this.posY = posY;
            this.health = health;
            this.behavior = behavior;
            this.framesBetweenBullets = framesBetweenBullets;
        }

        public void Kill()
        {
            health = 0;
            if (!ReferenceEquals(OnDeath, null)) OnDeath();
        }

        public void Damage(int amount)
        {
            health -= amount;
            if (health <= 0) Kill();
            else if (!ReferenceEquals(OnDamaged, null)) OnDamaged();
        }

        /// <summary>
        /// Create one or more attack projectiles
        /// </summary>
        /// <param name="lifeTime">Number of game frames before the projectile auto-expires</param>
        /// <param name="currentFrame">Current game frame</param>
        /// <param name="bulletType">Type of bullet (default uses bulletMode as the type)</param>
        /// <param name="force">Force the bullet to fire even if it hasn't been long enough</param>
        /// <returns></returns>
        public void FireWeapon(int lifeTime, long currentFrame, int bulletType = -1, bool force = false)
        {
            var projectiles = new List<Projectile>();
            //If it hasn't been long enough, don't fire.
            if (!force && currentFrame - lastFrameFired < framesBetweenBullets) return;
            if (bulletType == -1) bulletType = bulletMode;
            lastFrameFired = currentFrame;
            UnitGraphics bulletGfx = null;
            DCollider bulletCollider = null;
            double bulletVelocity = 0;

            if (bulletType == 0)
            {
                bulletGfx = new UnitGraphics(uGraphics.color, new List<PointF>()
                    {
                        new PointF(2,0),
                        new PointF(0, 2),
                        new PointF(-2,0),
                        new PointF(0, -2),
                        new PointF(2, 0)
                    });
                bulletCollider = new DCollider(0, 0, 3);
                bulletVelocity = 4;
            }
            lastBulletIndex = (lastBulletIndex + 1) % bulletPoints.Count;
            var bulletSpawnPoint = Geometry.Rotate(bulletPoints[lastBulletIndex], (float)facing);
            projectiles.Add(new Projectile(bulletType, lifeTime, bulletGfx, bulletCollider, velocity, facing, 
                bulletVelocity, angle, posX + bulletSpawnPoint.X, posY + bulletSpawnPoint.Y));

            if (!ReferenceEquals(OnWeaponFire, null)) OnWeaponFire(projectiles);
        }

        public void Process(long currentFrame)
        {
            base.Process();

            velocity *= 0.99;

            if (behavior == Behavior.ShootConstantly)
            {
                FireWeapon(240, currentFrame);
            }
        }
    }
}
