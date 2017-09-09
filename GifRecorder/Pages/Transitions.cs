using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ScreenToGif.Encoding;
using ScreenToGif.Properties;

namespace ScreenToGif.Pages
{
    /// <summary>
    /// Transition Tool Form.
    /// </summary>
    public partial class Transitions : Form
    {
        #region Variables

        private readonly List<Bitmap> _listToShow = new List<Bitmap>();
        private readonly List<float> _listAlpha = new List<float>();

        private readonly Bitmap _previous;
        private readonly Bitmap _next;

        private float _increment = 1F / (2);

        #endregion

        #region Properties

        /// <summary>
        /// List of transition frames.
        /// </summary>
        public List<Bitmap> ListToExport { get; set; }

        /// <summary>
        /// List of transition frame delays.
        /// </summary>
        public List<int> ListDelayExport { get; set; }

        #endregion

        /// <summary>
        /// Default constructor of the Transitions Form.
        /// </summary>
        /// <param name="previous">The Bitmap of the previous frame.</param>
        /// <param name="next">The Bitmap of the next frame.</param>
        /// <param name="indexFirst">The index of the previous frame.</param>
        /// <param name="indexNext">The index of the next frame.</param>
        public Transitions(Bitmap previous, Bitmap next, int indexFirst, int indexNext)
        {
            ListToExport = new List<Bitmap>();
            ListDelayExport = new List<int>();
            InitializeComponent();

            _previous = previous;
            _next = next;

            #region Update Indexes

            lblFirst.Text = indexFirst.ToString();
            lblNext.Text = indexNext.ToString();

            #endregion

            _listToShow.Add(previous);
            _listToShow.Add(next);

            trackFrames.Maximum = _listToShow.Count - 1;
            trackFrames.Value = 0;

            trackQuant_ValueChanged(null, null);
            trackBar_ValueChanged(null, null);

            #region Localize Labels

            lblTransition.Text = Resources.Label_TransitionCount + " 1";
            this.Text = Resources.Title_Transitions;

            #endregion
        }

        #region Events

        private void trackBar_ValueChanged(object sender, EventArgs e)
        {
            pbFrame.Image = _listToShow[trackFrames.Value];

            #region Updates the UI

            if (trackFrames.Value == 0)
            {
                tbPrevious.Text = "100 %";
                tbNext.Text = "0 %";
            }
            else if (trackFrames.Value == trackFrames.Maximum)
            {
                tbPrevious.Text = "0 %";
                tbNext.Text = "100 %";
            }
            else
            {
                tbPrevious.Text = Math.Round(_listAlpha[trackFrames.Value - 1], 3, MidpointRounding.AwayFromZero) * 100 + " %";
                tbNext.Text = Math.Round(Math.Abs(_listAlpha[trackFrames.Value - 1] - 1), 3, MidpointRounding.AwayFromZero) * 100 + " %";
            }

            #endregion
        }

        private void trackQuant_ValueChanged(object sender, EventArgs e)
        {
            trackFrames.Maximum = trackQuant.Value + 1;
            _increment = 1F / (trackQuant.Value + 1);

            #region Updates the UI

            lblTransition.Text = Resources.Label_TransitionCount + " " + +trackQuant.Value;

            #endregion

            #region Prepares the Lists

            ListToExport.Clear();
            _listAlpha.Clear();
            _listToShow.Clear();
            _listToShow.Add(_previous);
            _listToShow.Add(_next);

            #endregion

            float alpha = _increment;

            for (int i = 0; i < trackQuant.Value; i++)
            {
                _listAlpha.Insert(i, alpha);
                ListToExport.Insert(i, new Bitmap(_previous).Merge(ImageUtil.Transparency(new Bitmap(_next), alpha)));
                _listToShow.Insert(i + 1, new Bitmap(_previous).Merge(ImageUtil.Transparency(new Bitmap(_next), alpha)));
                ListDelayExport.Insert(i, 66);

                alpha += _increment;
            }

            trackBar_ValueChanged(null, null);
        }

        #endregion
    }
}
