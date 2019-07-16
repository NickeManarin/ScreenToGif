using System;
using System.Collections.Generic;

namespace ScreenToGif.ModelEx
{
    public class ProjectModel
    {
        #region Identity

        internal string Name { get; set; }

        internal DateTime? CreationDate { get; set; }

        internal DateTime? LastModificationDate { get; set; }

        #endregion

        #region Visual

        internal int Width { get; set; }

        internal int Heigth { get; set; }

        /// <summary>
        /// The Dpi of all frames of this project. Example: 96dpi.
        /// All frames should use the same Dpi. Image data should be converted if necessary.
        /// </summary>
        internal double Dpi { get; set; }

        /// <summary>
        /// The list of frames of this project.
        /// </summary>
        internal List<FrameModel> Frames { get; set; }

        #endregion

        /// <summary>
        /// Triggers rendering of the frames.
        /// </summary>
        /// <param name="from">The inclusive index of the first frame to be rendered. Starts with index 0.</param>
        /// <param name="to">The exclusive index of the last frame to be rendered. Starts with index 0.</param>
        internal void Render(int from, int to)
        {
            for (var i = from; i < to; i++)
                Frames[i].Render();

            //foreach (var frame in Frames)
            //    frame.Render();
        }
    }
}