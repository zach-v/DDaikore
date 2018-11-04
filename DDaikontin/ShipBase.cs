using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace DDaikontin
{
    public class ShipBase
    {
        public double velocity = 0;
        public double angle = 0; //Radians
        public double facing = 0;
        public double posX;
        public double posY;
        public List<PointF> bulletPoints;
        public int lastBulletIndex = 0;

        public long lastFrameFired = 0;

        public int bulletMode = 0;

        public UnitGraphics uGraphics;
        public DCollider collider;
        
        public ShipBase(UnitGraphics uGraphics, double posX, double posY, List<PointF> bulletPoints)
        {
            this.uGraphics = uGraphics;
            this.collider = new DCollider(uGraphics);
            this.bulletPoints = bulletPoints;
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

        /// <summary>
        /// Check if this ShipBase collides with another ShipBase //TODO: Make it into an interface
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool CollidesWith(ShipBase other)
        {
            return this.collider.CollidesWith(this.posX, this.posY, other.collider, other.posX, other.posY);
        }
    }
}
