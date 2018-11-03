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
        public double acceleration = 0;
        public double angle = 0; //Radians
        public double posX;
        public double posY;

        public UnitGraphics uGraphics;
        public DCollider collider;

        public ShipBase(UnitGraphics uGraphics)
        {
            this.uGraphics = uGraphics;
            posX = 0;
            posY = 0;
        }

        public ShipBase(UnitGraphics uGraphics, double posX, double posY)
        {
            this.uGraphics = uGraphics;
            this.posX = posX;
            this.posY = posY;
        }

        public double getVelocity {
            get;
        }

        public double getAcceleration()
        {
            return acceleration;
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
