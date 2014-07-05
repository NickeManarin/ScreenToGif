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
    /// <summary>
    /// A custom form to select values.
    /// </summary>
    public partial class ValuePicker : Form
    {
        #region Properties

        /// <summary>
        /// The description for a greater value.
        /// </summary>
        public string Greater { private get; set; }

        /// <summary>
        /// The description for a normal value.
        /// </summary>
        public string Normal { private get; set; }

        /// <summary>
        /// The description for a lower value.
        /// </summary>
        public string Lower { private get; set; }

        /// <summary>
        /// The description unit for the value.
        /// </summary>
        public string Unit { private get; set; }

        /// <summary>
        /// The value selected.
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// The prefix description for the value.
        /// </summary>
        public string Prefix { private get; set; }

        #endregion

        /// <summary>
        /// ValuePicker constructor.
        /// </summary>
        /// <param name="max">The maximum value.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="title">The title of the ValuePicker window.</param>
        /// <param name="unit">The unit of the value.</param>
        /// <param name="startValue">The start value.</param>
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

        /// <summary>
        /// Constructor for the "Change Speed";
        /// </summary>
        public ValuePicker(string title, int max, int min, string unit = "", int? startValue = null)
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

            lblValue.Text = String.Format("{0:0.0}", (trackBar.Value / 10F)) + " - " + unit;

            trackBar.Scroll -= trackBar_Scroll;
            trackBar.Scroll += trackBar_Scroll_Delay;
        }

        private void trackBar_Scroll(object sender, EventArgs e)
        {
            Value = trackBar.Value;

            if (trackBar.Value < 0)
            {
                lblValue.Text = Prefix + Math.Abs(trackBar.Value) + Unit + " " + Lower;
            }
            else if (trackBar.Value > 0)
            {
                lblValue.Text = Prefix + trackBar.Value + Unit + " " + Greater;
            }
            else
            {
                lblValue.Text = "0" + Unit;
            }
        }

        private void trackBar_Scroll_Delay(object sender, EventArgs e)
        {
            Value = trackBar.Value;

            if (trackBar.Value < 100)
            {
                lblValue.Text = String.Format("{0:0.00}", (trackBar.Value / 100F)) + " - " + Lower;
            }
            else if (trackBar.Value > 100)
            {
                lblValue.Text = String.Format("{0:0.00}", (trackBar.Value / 100F)) + " - " + Greater;
            }
            else
            {
                lblValue.Text = String.Format("{0:0.00}", (trackBar.Value / 100F)) + " - " + Normal;
            }

            //lblTest.Text = String.Format("{0:0.00}", 1000F / (trackBar.Value / 100F));
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            Value = trackBar.Value;
        }
    }
}
