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
        SpinShoot,
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

        public BulletType bulletMode = BulletType.StraightFacing;
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
        public void FireWeapon(int lifeTime, long currentFrame, BulletType bulletType = BulletType.Auto, bool force = false, double angle = 0)
        {
            var projectiles = new List<Projectile>();
            //If it hasn't been long enough, don't fire.
            if (!force && currentFrame - lastFrameFired < framesBetweenBullets) return;
            if (bulletType == BulletType.Auto)
                bulletType = bulletMode;
            lastFrameFired = currentFrame;
            UnitGraphics bulletGfx = null;
            DCollider bulletCollider = null;
            double bulletVelocity = 0;
            int bulletCount = 1;

            if (bulletType == BulletType.StraightFacing)
            {
                bulletGfx = new UnitGraphics(uGraphics.color, LineArt.TinyTim);
                bulletCollider = new DCollider(0, 0, 4);
                bulletVelocity = 4;
                angle = facing;
            } else if (bulletType == BulletType.FourWay)
            {
                bulletGfx = new UnitGraphics(uGraphics.color, LineArt.TinyTim);
                bulletCollider = new DCollider(0, 0, 4);
                bulletVelocity = 3;
                bulletCount = 4;
            } else
            {
                System.Diagnostics.Debugger.Break();
            }
            for (int i = 0; i < bulletCount; i++)
            {
                lastBulletIndex = (lastBulletIndex + 1) % bulletPoints.Count;
                var bulletSpawnPoint = Geometry.Rotate(bulletPoints[lastBulletIndex], (float)angle);
                projectiles.Add(new Projectile(bulletType, lifeTime, bulletGfx, bulletCollider, velocity, angle,
                    bulletVelocity, angle, posX + bulletSpawnPoint.X, posY + bulletSpawnPoint.Y));
                if (bulletType == BulletType.FourWay)
                {
                    angle += (Math.PI / 2);
                }
            }

            if (!ReferenceEquals(OnWeaponFire, null)) OnWeaponFire(projectiles);
        }

        public void Process(long currentFrame)
        {
            base.Process();

            if (behavior == Behavior.ShootConstantly)
            {
                FireWeapon(240, currentFrame, BulletType.StraightFacing);
                facing += 0.02;
                ApplyForce(0.11, facing);
            }
            if (behavior == Behavior.SpinShoot)
            {
                FireWeapon(240, currentFrame, BulletType.FourWay, false, facing);
                facing -= 0.09;
            }

            velocity *= 0.99;
        }
    }
}
