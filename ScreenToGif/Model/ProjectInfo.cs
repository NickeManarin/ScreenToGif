using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Interfaces;
using ScreenToGif.Domain.Models;
using ScreenToGif.Util;
using ScreenToGif.Util.Codification;
using ScreenToGif.Util.Extensions;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.Model;

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
    public List<FrameInfo> Frames { get; set; } = new();

    /// <summary>
    /// True if this project was recently created and was not yet loaded by the editor.
    /// </summary>
    [DataMember(Order = 3)]
    public bool IsNew { get; set; }

    /// <summary>
    /// Where this project was created?
    /// </summary>
    [DataMember(Order = 4)]
    public ProjectByType CreatedBy { get; set; } = ProjectByType.Unknown;

    /// <summary>
    /// The width of the canvas.
    /// </summary>
    [DataMember(Order = 5)]
    public int Width { get; set; }

    /// <summary>
    /// The height of the canvas.
    /// </summary>
    [DataMember(Order = 6)]
    public int Height { get; set; }

    /// <summary>
    /// The base dpi of the project.
    /// </summary>
    [DataMember(Order = 7)]
    public double Dpi { get; set; } = 96;

    /// <summary>
    /// The base bit depth of the project.
    /// 32 is RGBA
    /// 24 is RGB
    /// </summary>
    [DataMember(Order = 8)]
    public int BitDepth { get; set; } = 32;


    /// <summary>
    /// The full path of project based on current settings.
    /// </summary>
    public string FullPath => Path.Combine(UserSettings.All.TemporaryFolderResolved, "ScreenToGif", "Recording", RelativePath);

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
    /// The full path to the blob file, used by the recorder to write all frames pixels as a byte array, separated by a delimiter.
    /// </summary>
    public string CachePath => Path.Combine(UserSettings.All.TemporaryFolderResolved, "ScreenToGif", "Recording", RelativePath, "Frames.cache");

    /// <summary>
    /// Check if there's any frame on this project.
    /// </summary>
    public bool Any => Frames != null && Frames.Any();

    /// <summary>
    /// The latest index of the current list of frames, or -1.
    /// </summary>
    public int LatestIndex => Frames?.Count - 1 ?? -1;


    #region Methods

    public ProjectInfo CreateProjectFolder(ProjectByType creator)
    {
        IsNew = true;
        RelativePath = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + Path.DirectorySeparatorChar;
        CreatedBy = creator;

        Directory.CreateDirectory(FullPath);

        #region Create ActionStack folders

        if (!Directory.Exists(ActionStackPath))
            Directory.CreateDirectory(ActionStackPath);

        if (!Directory.Exists(UndoStackPath))
            Directory.CreateDirectory(UndoStackPath);

        if (!Directory.Exists(RedoStackPath))
            Directory.CreateDirectory(RedoStackPath);

        #endregion

        CreateMutex();

        return this;
    }

    public void Persist(string path = null)
    {
        try
        {
            using (var ms = new MemoryStream())
            {
                var ser = new DataContractJsonSerializer(typeof(ProjectInfo));

                ser.WriteObject(ms, this);

                File.WriteAllText(path ?? ProjectPath, Encoding.UTF8.GetString(ms.ToArray())); //Use Serializer
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

        MutexList.Remove(RelativePath);
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

    public void CreateMutex()
    {
        //TODO: Validate the possibility of opening this project.
        //I need to make sure that i'll release the mutexes.

        MutexList.Add(RelativePath);
    }

    public void ReleaseMutex()
    {
        MutexList.Remove(RelativePath);
    }

    /// <summary>
    /// Copy all necessary files to a new encode folder.
    /// </summary>
    /// <param name="copyJson">True if the Project.json file should be copied too.</param>
    /// <param name="useBytes">True if the images should be converted to byte array.</param>
    /// <param name="usePadding">True if the file names should have a left pad, to preserve the file ordering.</param>
    /// <returns>A list of frames with the new path.</returns>
    internal ExportProject CopyToExport(bool copyJson = false, bool useBytes = false, bool usePadding = false)
    {
        Persist();

        var export = new ExportProject();

        #region Output folder

        export.Path = Path.Combine(FullPath, "Encode " + DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss-ff"));

        if (!Directory.Exists(export.Path))
            Directory.CreateDirectory(export.Path);

        #endregion


        if (useBytes)
        {
            export.Frames = new List<ExportFrame>();
            export.ChunkPath = Path.Combine(export.Path, "Chunk");
            export.NewChunkPath = Path.Combine(export.Path, "NewChunk");

            try
            {
                //Create chunk file.
                using (var fileStream = new FileStream(export.ChunkPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var pos = 0L;

                    foreach (var info in Frames)
                    {
                        var image = new PixelUtil(info.Path.SourceFrom());
                        image.LockBits();

                        fileStream.WriteBytes(image.Pixels);

                        export.Frames.Add(new ExportFrame
                        {
                            DataPosition = pos,
                            DataLength = image.Pixels.LongLength,
                            Delay = info.Delay,
                            Rect = new Int32Rect(0,0, image.Width, image.Height),
                            ImageDepth = image.Depth
                        });

                        //Advances in the position.
                        pos += image.Pixels.LongLength;

                        image.UnlockBitsWithoutCommit();
                    }
                }
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "It was impossible to get the image bytes to encode.");
                throw;
            }

            return export;
        }

        export.UsesFiles = true;
        export.FramesFiles = new List<IFrame>();

        try
        {
            #region If it's being exported as project, maintain file naming

            if (copyJson)
            {
                foreach (var info in Frames)
                {
                    var filename = Path.Combine(export.Path, Path.GetFileName(info.Path));

                    //Copy the image to the folder.
                    File.Copy(info.Path, filename, true);

                    //Create the new object and add to the list.
                    export.FramesFiles.Add(new FrameInfo(filename, info.Delay));
                }

                File.Copy(ProjectPath, Path.Combine(export.Path, "Project.json"), true); //TODO: Filter out json too.

                return export;
            }

            #endregion

            //Detect pad size.
            var pad = usePadding ? (Frames.Count - 1).ToString().Length : 0;

            foreach (var info in Frames)
            {
                //Changes the path of the image. Writes as an ordered list of files, replacing the old filenames.
                var filename = Path.Combine(export.Path, export.FramesFiles.Count.ToString().PadLeft(pad, '0') + ".png");

                //Copy the image to the folder.
                File.Copy(info.Path, filename, true);

                //Create the new object and add to the list.
                export.FramesFiles.Add(new FrameInfo(filename, info.Delay));
            }
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "It was impossible to copy the files to encode.");
            throw;
        }

        return export;
    }

    internal ExportProject CopyToExport(List<int> indexes, bool copyJson = false, bool useBytes = false, bool usePadding = false)
    {
        Persist();

        var export = new ExportProject();

        #region Output folder

        var encodePath = "Encode " + DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss-ff");
        export.Path = Path.Combine(FullPath, encodePath);

        if (!Directory.Exists(export.Path))
            Directory.CreateDirectory(export.Path);

        #endregion

        if (useBytes)
        {
            export.Frames = new List<ExportFrame>();
            export.ChunkPath = Path.Combine(export.Path, "Chunk");
            export.NewChunkPath = Path.Combine(export.Path, "NewChunk");

            try
            {
                //Create chunk file.
                using (var fileStream = new FileStream(export.ChunkPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var pos = 0L;

                    foreach (var info in (indexes.Count > 0 ? Frames.Where((w, i) => indexes.Contains(i)) : Frames))
                    {
                        var image = new PixelUtil(info.Path.SourceFrom());
                        image.LockBits();

                        fileStream.WriteBytes(image.Pixels);

                        export.Frames.Add(new ExportFrame
                        {
                            Index = info.Index,
                            DataPosition = pos,
                            DataLength = image.Pixels.LongLength,
                            Delay = info.Delay,
                            Rect = new Int32Rect(0, 0, image.Width, image.Height),
                            ImageDepth = image.Depth
                        });

                        //Advances in the position.
                        pos += image.Pixels.LongLength;

                        image.UnlockBitsWithoutCommit();
                    }
                }
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "It was impossible to get the image bytes to encode.");
                throw;
            }

            return export;
        }

        export.UsesFiles = true;
        export.FramesFiles = new List<IFrame>();

        try
        {
            #region If it's being exported as project, maintain file naming

            if (copyJson)
            {
                foreach (var info in (indexes.Count > 0 ? Frames.Where((w, i) => indexes.Contains(i)) : Frames))
                {
                    var filename = Path.Combine(export.Path, Path.GetFileName(info.Path));

                    //Copy the image to the folder.
                    File.Copy(info.Path, filename, true);

                    //Create the new object and add to the list.
                    export.FramesFiles.Add(new FrameInfo
                    {
                        Index = info.Index,
                        Path = filename,
                        Delay = info.Delay
                    });
                }

                //Improve this.
                if (indexes.Count > 0)
                {
                    var projectAux = ShallowCopy();
                    projectAux.RelativePath = encodePath;
                    projectAux.Frames = Frames.Where((w, i) => indexes.Contains(i)).ToList();
                    projectAux.Persist(Path.Combine(export.Path, "Project.json"));
                }
                else
                {
                    File.Copy(ProjectPath, Path.Combine(export.Path, "Project.json"), true);
                }

                return export;
            }

            #endregion

            //Detect pad size.
            var pad = usePadding ? (Frames.Count - 1).ToString().Length : 0;

            foreach (var info in (indexes.Count > 0 ? Frames.Where((w, i) => indexes.Contains(i)) : Frames))
            {
                //Changes the path of the image. Writes as an ordered list of files, replacing the old filenames.
                var filename = Path.Combine(export.Path, export.FramesFiles.Count.ToString().PadLeft(pad, '0') + ".png");

                //Copy the image to the folder.
                File.Copy(info.Path, filename, true);

                //Create the new object and add to the list.
                export.FramesFiles.Add(new FrameInfo
                {
                    Index = info.Index,
                    Path = filename,
                    Delay = info.Delay
                });
            }
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "It was impossible to copy the files to encode.");
            throw;
        }

        return export;
    }

    public ProjectInfo ShallowCopy()
    {
        return (ProjectInfo) MemberwiseClone();
    }

    #endregion
}