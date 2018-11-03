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
        public Form1()
        {
            InitializeComponent();
            var core = new Core();
            core.MenuLoop = MenuLoop;
            core.Begin();
        }

        public void MenuLoop()
        {

        }


    }
}
