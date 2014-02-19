using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ScreenToGif.Pages
{
    public partial class ValuePicker : Form
    {
        public int Value { get; private set; }

        public ValuePicker(int max, int min, string title)
        {
            InitializeComponent();

            this.Text = title;
            trackBar.Maximum = max;
            trackBar.Minimum = min;

            trackBar.Value = trackBar.Minimum;
        }

        private void trackBar_Scroll(object sender, EventArgs e)
        {
            Value = trackBar.Value;
            lblValue.Text = trackBar.Value.ToString();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            Value = trackBar.Value;
        }
    }
}
