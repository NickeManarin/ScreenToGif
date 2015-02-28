using System;
using System.Drawing;
using System.Windows.Forms;
using ImageVisualizer.Properties;

namespace ImageVisualizer
{
    public partial class Visual : Form
    {
        public Visual(Image image)
        {
            InitializeComponent();

            pbImage.Image = image;
        }

        private void cbShowGrid_CheckedChanged(object sender, System.EventArgs e)
        {
            this.BackgroundImage = cbShowGrid.Checked ? Resources.grid : null;
        }
    }
}
