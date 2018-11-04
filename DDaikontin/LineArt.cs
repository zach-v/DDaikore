using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDaikontin
{
    public static class LineArt
    {
        public static readonly List<PointF> TinyTim = new List<PointF>()
        {
            new PointF(2,0),
            new PointF(0, 2),
            new PointF(-2,0),
            new PointF(0, -2),
            new PointF(2, 0)
        };
        public static readonly List<PointF> PlayerShip = new List<PointF>()
        {
            new PointF(16,0),
            new PointF(-16,14),
            new PointF(-6,0),
            new PointF(-16,-14),
            new PointF(16,0)
        };

        /// <summary>
        /// Shooting points for the Player ship art (3 points)
        /// </summary>
        public static List<PointF> PlayerShootPoints = new List<PointF>()
        {
            new PointF(16,0),
            new PointF(-18,16),
            new PointF(-18, -16)
        };

        /// <summary>
        /// Shooting points for the Player ship art (1 point)
        /// </summary>
        public static List<PointF> PlayerShootPoints1 = new List<PointF>()
        {
            new PointF(18,0)
        };

        /// <summary>
        /// Enemy Ship (complex ship design)
        /// </summary>
        public static readonly List<PointF> EnemyShip1 = new List<PointF>()
        {
            new PointF(4,16),
            new PointF(2,12),
            new PointF(0,12),
            new PointF(4,12),
            new PointF(-4,12),
            new PointF(-4,-12),
            new PointF(-8,-12),
            new PointF(-8,12),
            new PointF(-4,12),
            new PointF(-4,-12),
            new PointF(4,-12),
            new PointF(0,-12),
            new PointF(8,-12),
            new PointF(4,16)
        };

        public static List<PointF> EnemyShip1_ShootPoints = new List<PointF>()
        {
             new PointF(4,16),
             new PointF(0,12),
             new PointF(0,-12)
        };

        public static readonly List<PointF> EnemyShip2 = new List<PointF>()
        {
            new PointF(0,8),
            new PointF(4,11),
            new PointF(2,8),
            new PointF(4,4),
            new PointF(8,2),
            new PointF(11,4),
            new PointF(8,0),
            new PointF(11,-4),
            new PointF(8,-2),
            new PointF(4,-4),
            new PointF(2,-8),
            new PointF(4,-11),
            new PointF(0,-8),
            new PointF(-4,-11),
            new PointF(-2,-8),
            new PointF(-4,-4),
            new PointF(-8,-2),
            new PointF(-11,-4),
            new PointF(-8,0),
            new PointF(-11,4),
            new PointF(-8,2),
            new PointF(-4,4),
            new PointF(-2,8),
            new PointF(-4,11),
            new PointF(0,8),
        };

        public static List<PointF> EnemyShip2_ShootPoints = new List<PointF>()
        {
            new PointF(8,0),
        };
        
        /// <summary>
        /// Bullet design (naruto star thing)
        /// </summary>
        public static readonly List<PointF> BulletStar = new List<PointF>()
        {
            new PointF(-1,-2),
            new PointF(-2,-1),
            new PointF(2,-1),
            new PointF(1,-2),
            new PointF(1,2),
            new PointF(2,1),
            new PointF(-2,1),
            new PointF(-1,-2)
        };

        /// <summary>
        /// Bullet saw
        /// </summary>
        public static readonly List<PointF> BulletSaw = new List<PointF>()
        {
            new PointF(-1,-1),
            new PointF(1,2),
            new PointF(2,1),
            new PointF(1,1),
            new PointF(2,-1),
            new PointF(1,-2),
            new PointF(1,-1),
            new PointF(-1,-2),
            new PointF(-2,-1),
            new PointF(-2,1),
            new PointF(-1,2),
            new PointF(-1,-1)
        };
    }
}
