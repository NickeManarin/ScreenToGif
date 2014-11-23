using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ScreenToGif.Windows
{
    /// <summary>
    /// Interaction logic for Create.xaml
    /// </summary>
    public partial class Create : Window
    {
        #region Properties

        /// <summary>
        /// The Height of the image to be created.
        /// </summary>
        public int HeightValue { get; set; }

        /// <summary>
        /// The Width of the image to be created.
        /// </summary>
        public int WidthValue { get; set; }

        #endregion

        public Create()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            int width, heigth;

            #region Validation

            if (!int.TryParse(WidthText.Text, out width))
            {
                //TODO: Message.
                return;
            }

            if (!int.TryParse(HeightText.Text, out heigth))
            {
                //TODO: Message.
                return;
            }

            #endregion

            HeightValue = heigth;
            WidthValue = width;

            this.DialogResult = true;
            this.Close();
        }
    }
}
