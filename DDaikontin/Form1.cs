using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DDaikore;

namespace DDaikontin
{
    public partial class Form1 : Form
    {
        Core core = new Core();
        private int enterKey = 0;

        public Form1()
        {
            InitializeComponent();
            core.MenuLoop = MenuLoop;
            enterKey = core.RegisterInput(Keys.Enter);
            core.Begin();
        }

        public void MenuLoop()
        {
            var state = core.GetInputState(enterKey);
            if (state == Core.InputState.JustPressed)
            {
                textBox1.Text = "Im hit'n enter";
            } else
            {
                textBox1.Text = "";
            }
        }
    }
}
