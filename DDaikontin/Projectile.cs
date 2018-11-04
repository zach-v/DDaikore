using System;
using System.Collections.Generic;

namespace DDaikontin {
    public class Projectile {

        public bool bulletType = true;

        public double velocity = 0;
        public double angle = 0; //Radians
        public double posX;
        public double posY;
        public int lifetime = 0;

        public UnitGraphics uGraphics;
        public DCollider collider;

        public Projectile (bool bulletType, double initVel, UnitGraphics uGraphics, double angle, int lifetime, double posX, double posY) {
            this.bulletType = bulletType;
            velocity = initVel;
            this.uGraphics = uGraphics;
            this.lifetime = lifetime;
            this.angle = angle;
            this.posX = posX;
            this.posY = posY;
        }

        public void applyForce(double force, double direction) {
            //Break velocity + angle down into X and Y components, then add the .rotation-based force, then convert back with atan2 and the distance formula
            double xSpeed = velocity * Math.Cos(angle);
            double ySpeed = velocity * Math.Sin(angle);

            xSpeed += force * Math.Cos(direction);
            ySpeed += force * Math.Sin(direction);

            velocity = Math.Sqrt(xSpeed * xSpeed + ySpeed * ySpeed);
            angle = Math.Atan2(ySpeed, xSpeed);
        }
    }
}
