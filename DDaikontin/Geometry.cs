using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace DDaikontin
{
    public static class Geometry
    {
        public static PointF Rotate(this PointF point, float originX, float originY, float baseAngle)
        {
            return Rotate(point.X, point.Y, originX, originY, baseAngle);
        }

        public static PointF Rotate(float x, float y, float originX, float originY, float baseAngle)
        {
            var cos = (float)Math.Cos(baseAngle);
            var sin = (float)Math.Sin(baseAngle);
            var xDiff = x - originX;
            var yDiff = y - originY;

            var rotatedX = xDiff * cos - yDiff * sin;
            var rotatedY = xDiff * sin + yDiff * cos;

            return new PointF(rotatedX + originX, rotatedY + originY);
        }

        public static PointF Rotate(float x, float y, float baseAngle)
        {
            var cos = (float)Math.Cos(baseAngle);
            var sin = (float)Math.Sin(baseAngle);

            var rotatedX = x * cos - y * sin;
            var rotatedY = x * sin + y * cos;

            return new PointF(rotatedX, rotatedY);
        }

        public static PointF Rotate(this PointF point, float baseAngle)
        {
            return Rotate(point.X, point.Y, baseAngle);
        }

        public static void ApplyForce(ref double velocity, ref double angle, double force, double direction)
        {
            //Break velocity + angle down into X and Y components, then add the .rotation-based force, then convert back with atan2 and the distance formula
            double xSpeed = velocity * Math.Cos(angle);
            double ySpeed = velocity * Math.Sin(angle);

            xSpeed += force * Math.Cos(direction);
            ySpeed += force * Math.Sin(direction);

            velocity = Math.Sqrt(xSpeed * xSpeed + ySpeed * ySpeed);
            angle = Math.Atan2(ySpeed, xSpeed);
        }

        /// <summary>
        /// Calculate the angle from one point to another (to make x1,y1 face toward x2,y2)
        /// </summary>
        public static double Face(double x1, double y1, double x2, double y2)
        {
            return Math.Atan2(y1 - y2, x1 - x2);
        }

        public static double DistanceFromOrigin(double x, double y)
        {
            return Math.Sqrt(x * x + y * y);
        }

        /// <summary>
        /// Returns the angle restricted from 0 inclusive to Math.PI * 2 exclusive
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static double FixAngle(double angle)
        {
            while (angle < 0)
                angle += Math.PI * 2;
            while (angle > Math.PI * 2)
                angle -= Math.PI * 2;
            return angle;
        }
    }
}
