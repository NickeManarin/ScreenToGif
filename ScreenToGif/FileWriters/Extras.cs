using System.IO;

namespace ScreenToGif.FileWriters
{
    /// <summary>
    /// Extra methods for dealing with Files, Folders and Directories.
    /// </summary>
    public static class Extras
    {
        /// <summary>
        /// Creates the temp folder that holds all frames.
        /// </summary>
        public static void CreateTemp(string tempFolder)
        {
            #region Temp Folder

            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
                //Directory.CreateDirectory(tempFolder + "Undo");
                //Directory.CreateDirectory(tempFolder + "Edit");
            }

            #endregion
        }
    }
}
