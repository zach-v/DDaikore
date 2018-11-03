using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace DDaikontin
{
    class ShipBase
    {
        public double velocity = 0;
        public double acceleration = 0;
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
    }
}
