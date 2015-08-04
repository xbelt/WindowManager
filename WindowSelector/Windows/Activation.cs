using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowSelector.Configuration;

namespace WindowSelector
{
    public partial class Activation : Form
    {
        public Activation()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Config.Settings["Settings"]["Email"].Value = textBox1.Text.Trim().ToLower();
            Config.Settings["Settings"]["License"].Value = textBox2.Text;
            Config.Commit();
            DialogResult = DialogResult.OK;
        }
    }
}
