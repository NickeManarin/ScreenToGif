using System;
using System.Diagnostics;
using System.Drawing;
using ImageVisualizer;
using Microsoft.VisualStudio.DebuggerVisualizers;

[assembly: DebuggerVisualizer(typeof(ImageVisualizer.ImageVisualizer), typeof(VisualizerObjectSource), Target = typeof(Image), Description = "Image Visualizer")]
namespace ImageVisualizer
{
    #region Bitmap

    /// <summary>
    /// A Visualizer for Bitmaps.  
    /// </summary>
    public class ImageVisualizer : DialogDebuggerVisualizer
    {
        protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
        {
            if (windowService == null)
                throw new ArgumentNullException("windowService");
            if (objectProvider == null)
                throw new ArgumentNullException("objectProvider");

            var image = objectProvider.GetObject() as Image;

            if (image == null) return;

            using (var displayForm = new Visual(image))
            {
                windowService.ShowDialog(displayForm);
            }
        }

        /// <summary>
        /// Tests the visualizer by hosting it outside of the debugger.
        /// </summary>
        /// <param name="objectToVisualize">The object to display in the visualizer.</param>
        public static void TestShowVisualizer(object objectToVisualize)
        {
            var visualizerHost = new VisualizerDevelopmentHost(objectToVisualize, typeof(ImageVisualizer));
            visualizerHost.ShowVisualizer();
        }
    }

    #endregion
}

//TODO: Add the following to your testing code to test the visualizer:
//Visualizer.TestShowVisualizer(new SomeType());

