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
        #region Properties

        public string Greater { private get; set; }

        public string Lower { private get; set; }
        
        public string Unit { private get; set; }

        public int Value { get; set; }

        #endregion

        public ValuePicker(int max, int min, string title, string unit = "", int? startValue = null)
        {
            InitializeComponent();

            this.Text = title;
            trackBar.Maximum = max;
            trackBar.Minimum = min;

            if (startValue == null)
            {
                trackBar.Value = trackBar.Minimum;
            }
            else
            {
                trackBar.Value = startValue.Value;
            }

            lblValue.Text = trackBar.Value + unit;
        }

        private void trackBar_Scroll(object sender, EventArgs e)
        {
            Value = trackBar.Value;

            if (trackBar.Value < 0)
            {
                lblValue.Text = Math.Abs(trackBar.Value) + Unit + " " + Lower;
            }
            else if (trackBar.Value > 0)
            {
                lblValue.Text = trackBar.Value + Unit + " " + Greater;
            }
            else
            {
                lblValue.Text = "0" + Unit;
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            Value = trackBar.Value;
        }
    }
}
