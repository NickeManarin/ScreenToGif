using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using ScreenToGif.FileWriters;

namespace ScreenToGif.Util.Model
{
    [DataContract]
    public class ProjectInfo
    {
        /// <summary>
        /// The relative path of initial destination of this project.
        /// </summary>
        [DataMember(Name = "Relative", Order = 0)]
        public string RelativePath { get; set; }

        /// <summary>
        /// The date of reation of this project.
        /// </summary>
        [DataMember(Order = 1)]
        public DateTime CreationDate { get; set; } = DateTime.Now;

        /// <summary>
        /// List of frames.
        /// </summary>
        [DataMember(Order = 2)]
        public List<FrameInfo> Frames { get; set; } = new List<FrameInfo>();


        /// <summary>
        /// The full path of project based on current settings.
        /// </summary>
        public string FullPath => Path.Combine(UserSettings.All.TemporaryFolder, "ScreenToGif", "Recording", RelativePath);

        /// <summary>
        /// Full path to the serialized project file. 
        /// </summary>
        public string ProjectPath => Path.Combine(FullPath, "Project.json");

        /// <summary>
        /// The full path to the action stack files (undo, redo).
        /// </summary>
        public string ActionStackPath => Path.Combine(FullPath, "ActionStack");

        /// <summary>
        /// The full path to the undo folder.
        /// </summary>
        public string UndoStackPath => Path.Combine(ActionStackPath, "Undo");

        /// <summary>
        /// The full path to the redo folder.
        /// </summary>
        public string RedoStackPath => Path.Combine(ActionStackPath, "Redo");
        
        /// <summary>
        /// Check if there's any frame on this project.
        /// </summary>
        public bool Any => Frames != null && Frames.Any();

        /// <summary>
        /// The latest index of the current list of frames, or -1.
        /// </summary>
        public int LatestIndex => Frames?.Count - 1 ?? -1;

        #region Methods

        public ProjectInfo CreateProjectFolder()
        {
            //Check if the parameter exists.
            if (string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolder))
                UserSettings.All.TemporaryFolder = Path.GetTempPath();

            RelativePath = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + Path.DirectorySeparatorChar;

            Directory.CreateDirectory(FullPath);

            #region Create ActionStack folders

            if (!Directory.Exists(ActionStackPath))
                Directory.CreateDirectory(ActionStackPath);
            
            if (!Directory.Exists(UndoStackPath))
                Directory.CreateDirectory(UndoStackPath);
            
            if (!Directory.Exists(RedoStackPath))
                Directory.CreateDirectory(RedoStackPath);
            
            #endregion

            return this;
        }

        public void Persist()
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    var ser = new DataContractJsonSerializer(typeof(ProjectInfo));
                    
                    ser.WriteObject(ms, this);

                    File.WriteAllText(ProjectPath, Encoding.UTF8.GetString(ms.ToArray()));
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Persisting the current project info.");
            }
        }

        public void Clear()
        {
            Frames?.Clear();
        }

        public string FilenameOf(int index)
        {
            return Any && LatestIndex >= index ? Path.Combine(FullPath, Frames[index].Name) : "";
        }

        /// <summary>
        /// Gets the index that is in range of the current list of frames.
        /// </summary>
        /// <param name="index">The index to compare.</param>
        /// <returns>A valid index.</returns>
        public int ValidIndex(int index)
        {
            if (index == -1)
                index = 0;

            return LatestIndex >= index ? index : LatestIndex;
        }

        #endregion
    }
}
