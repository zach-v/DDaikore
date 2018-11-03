﻿using System;
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
        private int upArrowKey = 0;
        private int downArrowKey = 0;

        public Form1()
        {
            InitializeComponent();
            new Thread(() =>
            {
                core.MenuLoop = MenuLoop;
                core.MenuDraw = MenuDraw;

                regKeys();

                core.Begin();
            }).Start();
        }

        public void MenuLoop()
        {
            if (core.GetInputState(enterKey) == Core.InputState.JustPressed) core.menuIndex = -1;
            if (core.GetInputState(upArrowKey) == Core.InputState.JustPressed) core.menuOption = (core.menuOption + 1) % 2;
            if (core.GetInputState(downArrowKey) == Core.InputState.JustPressed) core.menuOption = core.menuOption == 0 ? 1 : core.menuOption - 1;
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

            if (core.menuOption == 0)
            {
                drawMenu(g);
            }
            if (core.menuOption == -1)
            {
                drawGame(g);
            }
        }

        private void drawMenu(Graphics g)
        {
            g.DrawString("Daikontinum", this.Font, Brushes.Black, new PointF(20, 10));
            g.DrawString("Play", this.Font, core.menuOption == 0 ? Brushes.Blue : Brushes.Black, new PointF(20, 40));
            g.DrawString("Exit", this.Font, core.menuOption == 1 ? Brushes.Blue : Brushes.Black, new PointF(20, 60));
        }

        private void drawGame(Graphics g)
        {

        }

        private void regKeys()
        {
            enterKey = core.RegisterInput(Keys.Enter);
            upArrowKey = core.RegisterInput(Keys.Up);
            downArrowKey = core.RegisterInput(Keys.Down);
        }
    }
}
