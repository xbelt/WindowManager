using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowSelector
{
    public partial class RuleManager : Form
    {
        public Position Result = null;
        public RuleManager()
        {
            InitializeComponent();

            InitializeNumericUpDowns();
        }

        public RuleManager(Position pos)
        {
            InitializeComponent();
            InitializeNumericUpDowns();

            Result = pos;

            ((NumericUpDownWitoutButtons) tableLayoutPanel1.GetControlFromPosition(1, 0)).Value = (decimal) pos.X;
            ((NumericUpDownWitoutButtons) tableLayoutPanel1.GetControlFromPosition(1, 1)).Value = (decimal) pos.Y;
            ((NumericUpDownWitoutButtons) tableLayoutPanel1.GetControlFromPosition(1, 2)).Value = (decimal) pos.Width;
            ((NumericUpDownWitoutButtons) tableLayoutPanel1.GetControlFromPosition(1, 3)).Value = (decimal) pos.Height;

        }

        private void InitializeNumericUpDowns()
        {
            for (var i = 0; i < 4; i++)
            {
                tableLayoutPanel1.Controls.Add(new NumericUpDownWitoutButtons()
                {
                    DecimalPlaces = 2,
                    Maximum = 100,
                    Minimum = 0,
                    Anchor = AnchorStyles.Right
                }, 1, i);
            }
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            Result =
                new Position(
                    (float) ((NumericUpDownWitoutButtons) tableLayoutPanel1.GetControlFromPosition(1, 0)).Value,
                    (float) ((NumericUpDownWitoutButtons) tableLayoutPanel1.GetControlFromPosition(1, 1)).Value,
                    (float) ((NumericUpDownWitoutButtons) tableLayoutPanel1.GetControlFromPosition(1, 2)).Value,
                    (float) ((NumericUpDownWitoutButtons) tableLayoutPanel1.GetControlFromPosition(1, 3)).Value);
            DialogResult = DialogResult.OK;
        }
    }
}
