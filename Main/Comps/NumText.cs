using System;
using System.Windows.Forms;

namespace U6.Comp
{
    public partial class NumText : NumericUpDown
    {
        public NumText()
        {
            InitializeComponent();
            Controls[0].Hide();
        }
        protected override void OnTextBoxResize(object source, EventArgs e)
        {
            Controls[1].Width = Width - 4;
        }
    }
}