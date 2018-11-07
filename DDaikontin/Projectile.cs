using System;
using System.Collections.Generic;

namespace DDaikontin
{
    public enum BulletType
    {
        Auto,
        StraightStrong,
        Straight,
        Spin,
        FourWay,
    }

    public class Projectile : Body
    {
        public int lifetime = 0;
        public int damage = 1;

        public BulletType bulletType = BulletType.Straight;

        public UnitGraphics uGraphics;

        protected Projectile() { }

        public Projectile(BulletType bulletType, int lifetime, UnitGraphics uGraphics, DCollider collider, 
            double shooterVelocity, double shooterFacing, double bulletVelocity, double shooterAngle,
            double spawnX, double spawnY)
        {
            this.bulletType = bulletType;
            this.angle = shooterAngle;
            this.velocity = shooterVelocity;
            this.facing = shooterFacing;
            
            this.uGraphics = uGraphics;
            this.collider = collider;
            this.lifetime = lifetime;
            this.posX = spawnX;
            this.posY = spawnY;
            
            Geometry.ApplyForce(ref this.velocity, ref this.angle, bulletVelocity, shooterFacing);
            if (bulletType == BulletType.Straight) damage = 1;
            else if (bulletType == BulletType.StraightStrong) damage = 3;
            else if (bulletType == BulletType.FourWay) damage = 1;
        }

        public void Kill()
        {
            //Expire the bullet (but it'll stay in memory until we're ready to get rid of it)
            lifetime = 0;
        }

        public void Process(long currentFrame)
        {
            base.Process();

            if (bulletType == BulletType.Spin) //Spinny shuriken bullet
            {
                facing += 0.07;
            }
            if (bulletType == BulletType.FourWay)
            {
                var tv = this.velocity;
                facing += 0.016;
                //facing *= 1.02; //Fireflies
                Geometry.ApplyForce(ref this.velocity, ref this.angle, 0.7, facing);
                this.velocity = tv;
            }
        }

        public Projectile CloneForRenderer()
        {
            return new Projectile() {
                facing = facing,
                angle = angle,
                uGraphics = uGraphics,
                posX = posX,
                posY = posY,
#if DEBUG
                collider = collider,
#endif
            };
        }
    }
}
