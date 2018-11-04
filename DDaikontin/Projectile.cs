using System;
using System.Collections.Generic;

namespace DDaikontin
{
    public class Projectile : Body
    {
        public int bulletType = 0;
        public int lifetime = 0;

        public UnitGraphics uGraphics;

        public Projectile(int bulletType, int lifetime, UnitGraphics uGraphics, DCollider collider, 
            double shooterVelocity, double shooterFacing, double bulletVelocity, double shooterAngle,
            double spawnX, double spawnY)
        {
            this.bulletType = bulletType;
            this.angle = shooterAngle;
            this.velocity = shooterVelocity;
            this.facing = shooterFacing;
            Geometry.ApplyForce(ref this.velocity, ref this.angle, bulletVelocity, shooterFacing);
            this.uGraphics = uGraphics;
            this.collider = collider;
            this.lifetime = lifetime;
            this.posX = spawnX;
            this.posY = spawnY;
        }

        public new void Process()
        {
            base.Process();

            if (bulletType == 1) //Spinny shuriken bullet
            {
                facing += 0.07;
            }
        }
    }
}
