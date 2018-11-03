using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using DDaikore;

namespace DDaikontin
{
    public partial class Form1 : Form
    {
        Core core = new Core();
        private int enterKey = 0;
        private bool exited = false;

        private bool drawMeow2 = false;

        public Form1()
        {
            InitializeComponent();
            new Thread(() =>
            {
                core.MenuLoop = MenuLoop;
                core.MenuDraw = MenuDraw;
                enterKey = core.RegisterInput(Keys.Enter);
                core.Begin();
            }).Start();
        }

        public void MenuLoop()
        {
            var state = core.GetInputState(enterKey);
            if (state == Core.InputState.JustPressed) drawMeow2 = true;
            else drawMeow2 = false;
        }

        public void MenuDraw()
        {
            pictureBox1.Invalidate();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            core.Exit();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            //Clear the background
            g.FillRectangle(Brushes.White, 0, 0, pictureBox1.Width, pictureBox1.Height);

            //TODO: This is where you draw stuff
            g.DrawString("Meow", this.Font, Brushes.Black, new PointF(20, 20));
            if (drawMeow2) g.DrawString("Meow", this.Font, Brushes.Black, new PointF(50, 50));
        }
    }
}
