using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScreenToGif.Util
{
    /// <summary>
    /// Other helper methods.
    /// </summary>
    public static class Other
    {
        public static List<FrameInfo> CopyList(this List<FrameInfo> target)
        {
            return target.Select(item => new FrameInfo(item.ImageLocation, item.Delay, item.CursorInfo)).ToList();
        }

        /// <summary>
        /// Copies the List and saves the images in another folder.
        /// </summary>
        /// <param name="target">The List to copy</param>
        /// <returns>The copied list.</returns>
        public static List<FrameInfo> CopyToEncode(this List<FrameInfo> target)
        {
            #region Folder

            string fileNameAux = Path.GetFileName(target[0].ImageLocation);

            if (fileNameAux == null)
                throw new ArgumentException("Impossible to get filename.");

            var encodeFolder = Path.Combine(target[0].ImageLocation.Replace(fileNameAux, ""), "Encode " + DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss"));

            if (!Directory.Exists(encodeFolder))
                Directory.CreateDirectory(encodeFolder);

            #endregion

            var newList = new List<FrameInfo>();

            foreach (FrameInfo frameInfo in target)
            {
                //Changes the path of the image.
                var filename = Path.Combine(encodeFolder, Path.GetFileName(frameInfo.ImageLocation));

                //Copy the image to the folder.
                File.Copy(frameInfo.ImageLocation, filename);

                //Create the new object and add to the list.
                newList.Add(new FrameInfo(filename, frameInfo.Delay, frameInfo.CursorInfo));
            }

            return newList;
        }
    }
}
