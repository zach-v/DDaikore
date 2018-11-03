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
        public List<PointF> points;
        public Pen color;

        public UnitGraphics()
        {
            color = Pens.White;
            points = new List<PointF>();
        }

        public UnitGraphics(Pen color)
        {
            this.color = color;
            points = new List<PointF>();
        }

        public UnitGraphics(Pen color, List<PointF> points)
        {
            this.color = color;
            this.points = points;
        }

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
