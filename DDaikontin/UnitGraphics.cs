using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDaikontin
{
    public class UnitGraphics
    {
        // Contains visual graphics about specific units
        List<PointF> points = new List<PointF>();
        public UnitGraphics()
        {
            init();
        }

        // just to test the points, creates a rectangle
        // -------------------------------------------------
        // THIS IS JUST A TEST MASON
        private void init()
        {
            points.Add(new PointF(10, 20));
            points.Add(new PointF(30, 20));
            points.Add(new PointF(30, 10));
            points.Add(new PointF(10, 10));
        }
        // -------------------------------------------------

        public void populate(List<PointF> points)
        {
            this.points = points;
        }

        public void addPoint(PointF point)
        {
            points.Add(point);
        }
    }
}
