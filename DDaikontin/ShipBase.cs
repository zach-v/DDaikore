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
        private double velocity = 0;
        private double acceleration = 0;
        private PointF position;

        private UnitGraphics uGraphics;

        public ShipBase(UnitGraphics uGraphics)
        {
            this.uGraphics = uGraphics;
            position = new PointF(0, 0);    // Default to 0,0
        }

        public ShipBase(UnitGraphics uGraphics, PointF position)
        {
            this.uGraphics = uGraphics;
            this.position = position;
        }

        public double getVelocity()
        {
            return velocity;
        }

        public double getAcceleration()
        {
            return acceleration;
        }
    }
}
