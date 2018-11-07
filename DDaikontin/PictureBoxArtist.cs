using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DDaikontin
{
    /// <summary>
    /// Class to connect GameRenderer to a PictureBox
    /// </summary>
    public class PictureBoxArtist : Artist<System.Drawing.Drawing2D.Matrix>
    {
#if DEBUG
        private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        private double frameSeconds = 0;
        private double avgFrameSeconds = 0.05;
#endif

        public void Prepare(Graphics g)
        {
            AfterFrame = () => {
#if DEBUG
                g.DrawString(String.Format("fps: {0:0.00}", 1 / frameSeconds), SystemFonts.DefaultFont, Brushes.White, 300, 0);
                g.DrawString(String.Format("avg: {0:0.00}", 1 / avgFrameSeconds), SystemFonts.DefaultFont, Brushes.White, 350, 0);
                sw.Stop();
                frameSeconds = (double)sw.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency;
                avgFrameSeconds = (9 * avgFrameSeconds + frameSeconds) / 10; //Moving average (estimate of last 10 frames' average; not exact)
                sw.Reset();
#endif
            };
            BeforeFrame = () => {
#if DEBUG
                sw.Start();
#endif
                g.Clear(Color.Black);
            };
            TranslateTransform = g.TranslateTransform;
            RotateTransform = g.RotateTransform;
            GetMatrix = () => { return g.Transform; };
            SetMatrix = (m) => { g.Transform = m; };
            ResetMatrix = g.ResetTransform;
            MeasureString = g.MeasureString;
            DrawStringRect = g.DrawString;
            DrawString = g.DrawString;
            DrawLine = g.DrawLine;
            DrawEllipse = g.DrawEllipse;
            DrawLines = g.DrawLines;
        }
    }
}
