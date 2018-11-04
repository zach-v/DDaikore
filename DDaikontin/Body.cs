using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDaikontin
{
    public class Body
    {
        /// <summary>
        /// Magnitude of motion vector
        /// </summary>
        public double velocity = 0;
        /// <summary>
        /// Angle of motion vector in radians
        /// </summary>
        public double angle = 0;
        /// <summary>
        /// Angle of visual rotation in radians
        /// </summary>
        public double facing = 0;
        public double posX;
        public double posY;

        public DCollider collider;

        /// <summary>
        /// Check if this ShipBase collides with another ShipBase //TODO: Collider might work better as an interface
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool CollidesWith(Body other)
        {
            //TODO: Needs to account for both objects' facing
            return this.collider.CollidesWith(this.posX, this.posY, other.collider, other.posX, other.posY);
        }

        public void ApplyForce(double force, double direction)
        {
            Geometry.ApplyForce(ref this.velocity, ref this.angle, force, direction);
        }

        //Basic movement
        public void Process()
        {
            posX += velocity * Math.Cos(angle);
            posY += velocity * Math.Sin(angle);
        }
    }
}
