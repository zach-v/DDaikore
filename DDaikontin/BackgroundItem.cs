using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDaikontin
{
    public class BackgroundItem
    {

        public double posX = 0;
        public double posY = 0;

        UnitGraphics uGraphics;

        public BackgroundItem(double posX, double posY, UnitGraphics uGraphics)
        {
            this.posX = posX;
            this.posY = posY;
            this.uGraphics = uGraphics;
        }
    }
}
