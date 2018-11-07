using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace DDaikontin
{
    /// <summary>
    /// This class contains delegates for drawing functions and holds the drawing object
    /// </summary>
    /// <typeparam name="T">Projection matrix datatype (such as System.Drawing.Drawing2D.Matrix)</typeparam>
    public abstract class Artist<T>
    {
        public Action BeforeFrame;
        public Action AfterFrame;
        //These functions are all based on Graphics object since I am arbitrarily refused the ability to inherit from it
        public Action<float, float> TranslateTransform;
        public Action<float> RotateTransform;
        public Func<T> GetMatrix;
        public Action<T> SetMatrix;
        public Action ResetMatrix;
        public Func<string, Font, int, SizeF> MeasureString;
        public Action<string, Font, Brush, RectangleF> DrawStringRect;
        public Action<string, Font, Brush, float, float> DrawString;
        public Action<Pen, float, float, float, float> DrawLine;
        public Action<Pen, float, float, float, float> DrawEllipse;
        public Action<Pen, PointF[]> DrawLines;
    }
}
