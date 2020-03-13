using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace ScreenToGif.ModelEx
{
    public class Project
    {
        #region Identity

        public string Name { get; set; }

        public Version Version { get; set; }

        public DateTime? CreationDate { get; set; }

        public DateTime? LastModificationDate { get; set; }

        #endregion

        #region Visual

        /// <summary>
        /// The canvas width of the project.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// The canvas height of the project.
        /// </summary>
        public int Heigth { get; set; }

        /// <summary>
        /// The DPI of the X axis of the project.
        /// </summary>
        public double HorizontalDpi { get; set; }

        /// <summary>
        /// The DPI of the Y axis of the project.
        /// </summary>
        public double VerticalDpi { get; set; }

        /// <summary>
        /// The background of the whole project.
        /// </summary>
        public Brush Background { get; set; }

        #endregion

        /// <summary>
        /// Tracks can hold multiple sequences of the same type, but not overlapping.
        /// </summary>
        public List<Track> Tracks { get; set; }
    }
}