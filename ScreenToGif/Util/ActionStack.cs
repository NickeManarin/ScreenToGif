using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScreenToGif.Util
{
    /// <summary>
    /// Do, Undo, Redo stack.
    /// </summary>
    public static class ActionStack
    {
        #region Variables

        private static readonly Stack<StateChange> UndoStack = new Stack<StateChange>();
        private static readonly Stack<StateChange> RedoStack = new Stack<StateChange>();

        private static string _actualFolder;
        private static string _undoFolder;
        private static string _redoFolder;

        #endregion

        public enum EditAction
        {
            Remove,
            ImageAndProperties,
            Properties,
            Add,
            Reorder
        }

        public class StateChange
        {
            public EditAction Cause { get; set; }
            public List<FrameInfo> Frames { get; set; }

            public List<int> Indexes { get; set; }

            /// <summary>
            /// Signals that this state should not be available.
            /// </summary>
            public bool IgnoreWhenReset { get; set; }
        }

        #region Save State

        public static void SaveState(EditAction action, List<FrameInfo> frames, List<int> positions)
        {
            var orderedPositions = positions.OrderBy(x => x).ToList();
            var savedFrames = new List<FrameInfo>();
            var currentFolder = CreateCurrent(true);

            switch (action)
            {
                case EditAction.Remove:

                    //Saves the frames that will be deleted (using the given list of positions).
                    foreach (var position in orderedPositions)
                    {
                        var frame = frames[position];
                        var savedFrame = Path.Combine(currentFolder, Path.GetFileName(frame.ImageLocation)); // position + ".png");

                        //Copy to a folder.
                        File.Copy(frame.ImageLocation, savedFrame);

                        savedFrames.Add(new FrameInfo(savedFrame, frame.Delay));
                    }

                    //Create a StageChange object with the saved frames and push to the undo stack.
                    UndoStack.Push(new StateChange
                    {
                        Cause = action,
                        Frames = savedFrames,
                        Indexes = orderedPositions
                    });

                    break;
                case EditAction.ImageAndProperties:

                    //Saves the frames that will be altered (using the given list of positions).
                    foreach (var position in orderedPositions)
                    {
                        var frame = frames[position];
                        var savedFrame = Path.Combine(currentFolder, Path.GetFileName(frame.ImageLocation)); // position + ".png");

                        //Copy to a folder.
                        File.Copy(frame.ImageLocation, savedFrame);

                        savedFrames.Add(new FrameInfo(savedFrame, frame.Delay));
                    }

                    //Create a StageChange object with the saved frames and push to the undo stack.
                    UndoStack.Push(new StateChange
                    {
                        Cause = action,
                        Frames = savedFrames,
                        Indexes = orderedPositions
                    });

                    break;
                case EditAction.Properties:

                    //Saves the frames that will be altered, without copying the images (using the given list of positions).
                    foreach (var position in orderedPositions)
                    {
                        var frame = frames[position];

                        savedFrames.Add(new FrameInfo(frame.ImageLocation, frame.Delay));
                    }

                    //Create a StageChange object with the saved frames and push to the undo stack.
                    UndoStack.Push(new StateChange
                    {
                        Cause = action,
                        Frames = savedFrames,
                        Indexes = orderedPositions
                    });

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }

            //Clear the Redo stack.
            ClearRedo();
        }

        /// <summary>
        /// Save the state of the list of frames. This overload is used by the EditAction.Add.
        /// </summary>
        /// <param name="action">The action, currently just the Add.</param>
        /// <param name="position">The position where the frames will be inserted.</param>
        /// <param name="quantity">The quantity of inserted frames.</param>
        public static void SaveState(EditAction action, int position, int quantity)
        {
            //Saves the position where the new frames will be inserted.
            UndoStack.Push(new StateChange
            {
                Cause = action,
                Indexes = Util.Other.CreateIndexList2(position, quantity)
            });

            //Clear the Redo stack.
            ClearRedo();
        }

        /// <summary>
        /// Save the state of the list of frames. This overload is used by the EditAction.Reorder.
        /// </summary>
        /// <param name="action">The action, currently just the Reorder.</param>
        /// <param name="frames">The old (current) list of frames.</param>
        public static void SaveState(EditAction action, List<FrameInfo> frames)
        {
            //Saves the frames before the reordering.
            UndoStack.Push(new StateChange
            {
                Cause = action,
                Frames = frames,
            });

            //Clear the Redo stack.
            ClearRedo();
        }

        #endregion

        #region Actions

        public static List<FrameInfo> Undo(List<FrameInfo> current, bool pushToRedo = true)
        {
            //Pop from Undo stack.
            var latestUndo = UndoStack.Pop();

            #region Push into Redo stack

            if (pushToRedo)
            {
                var redoStateChange = new StateChange();

                //To redo the action, it should be saved as the inverse of the current undo.
                switch (latestUndo.Cause)
                {
                    case EditAction.Remove:

                        #region Add (Inverse)

                        redoStateChange.Cause = EditAction.Add;
                        redoStateChange.Indexes = new List<int>(latestUndo.Indexes);

                        #endregion

                        break;
                    case EditAction.Add:

                        #region Remove (Inverse)

                        var savedFrames = new List<FrameInfo>();
                        var redoFolder = CreateCurrent(false);

                        redoStateChange.Cause = EditAction.Remove;
                        redoStateChange.Indexes = new List<int>(latestUndo.Indexes);

                        //Saves the frames that will be deleted (using the given list of positions).
                        foreach (var position in latestUndo.Indexes)
                        {
                            var frame = current[position];
                            var savedFrame = Path.Combine(redoFolder, Path.GetFileName(frame.ImageLocation));

                            //Copy to a folder.
                            File.Copy(frame.ImageLocation, savedFrame);

                            savedFrames.Add(new FrameInfo(savedFrame, frame.Delay, frame.CursorInfo));
                        }

                        redoStateChange.Frames = savedFrames;

                        #endregion

                        break;
                    case EditAction.ImageAndProperties:

                        #region Alter the images and properties (Inverse)

                        var savedFrames2 = new List<FrameInfo>();
                        var redoFolder2 = CreateCurrent(false);

                        redoStateChange.Cause = EditAction.ImageAndProperties;
                        redoStateChange.Indexes = latestUndo.Indexes;

                        //Saves the frames that will be deleted (using the given list of positions).
                        foreach (var position in latestUndo.Indexes)
                        {
                            var frame = current[position];
                            var savedFrame = Path.Combine(redoFolder2, Path.GetFileName(frame.ImageLocation));

                            //Copy to a folder.
                            File.Copy(frame.ImageLocation, savedFrame);

                            savedFrames2.Add(new FrameInfo(savedFrame, frame.Delay, frame.CursorInfo));
                        }

                        redoStateChange.Frames = savedFrames2;

                        #endregion

                        break;
                    case EditAction.Properties:

                        #region Alter the properties (Inverse)

                        redoStateChange.Cause = EditAction.Properties;
                        redoStateChange.Frames = latestUndo.Frames;
                        redoStateChange.Indexes = latestUndo.Indexes;

                        #endregion

                        break;
                    case EditAction.Reorder:

                        #region Reorder (Inverse)

                        redoStateChange.Cause = EditAction.Reorder;
                        redoStateChange.Frames = current.CopyList();

                        #endregion

                        break;
                }

                RedoStack.Push(redoStateChange);
            }

            #endregion

            #region Undo

            switch (latestUndo.Cause)
            {
                case EditAction.Remove:

                    #region Insert again the frames

                    if (latestUndo.Frames == null || latestUndo.Frames.Count == 0)
                        throw new Exception("No frames to undo.");

                    var folder = Path.GetDirectoryName(current[0].ImageLocation);

                    var currentIndex = 0;
                    foreach (var index in latestUndo.Indexes)
                    {
                        var frame = latestUndo.Frames[currentIndex];
                        var file = Path.Combine(folder, Path.GetFileName(frame.ImageLocation));

                        //Copy file to folder.
                        File.Copy(frame.ImageLocation, file);

                        //Add to list.
                        current.Insert(index, new FrameInfo(file, frame.Delay, frame.CursorInfo));

                        currentIndex++;
                    }

                    //Erase the undo folder.
                    Directory.Delete(Path.GetDirectoryName(latestUndo.Frames[0].ImageLocation), true);

                    #endregion

                    break;
                case EditAction.ImageAndProperties:

                    #region Alter the image and properties

                    if (latestUndo.Frames == null || latestUndo.Frames.Count == 0)
                        throw new Exception("No frames to redo.");

                    var alteredIndex2 = 0;
                    foreach (var frame in latestUndo.Frames)
                    {
                        //Get the current frame before or after returning the properties values?
                        var currentFrame = current[latestUndo.Indexes[alteredIndex2]];

                        current[latestUndo.Indexes[alteredIndex2]] = new FrameInfo(currentFrame.ImageLocation, frame.Delay, frame.CursorInfo); //Image location stays the same.

                        //Copy file to folder.
                        File.Copy(frame.ImageLocation, currentFrame.ImageLocation, true);

                        alteredIndex2++;
                    }

                    //Erase the undo folder.
                    Directory.Delete(Path.GetDirectoryName(latestUndo.Frames[0].ImageLocation), true);

                    #endregion

                    break;
                case EditAction.Properties:

                    #region Alter the properties

                    if (latestUndo.Frames == null || latestUndo.Frames.Count == 0)
                        throw new Exception("No frames to undo.");

                    var alteredIndex = 0;
                    foreach (var frame in latestUndo.Frames)
                    {
                        current[latestUndo.Indexes[alteredIndex]] = new FrameInfo(frame.ImageLocation, frame.Delay);

                        alteredIndex++;
                    }

                    #endregion

                    break;
                case EditAction.Add:

                    #region Remove the added frames

                    foreach (var index in latestUndo.Indexes.OrderByDescending(x => x))
                    {
                        File.Delete(current[index].ImageLocation);

                        current.RemoveAt(index);
                    }

                    #endregion

                    break;
                case EditAction.Reorder:

                    #region Reorder the frames to a previous order

                    current = latestUndo.Frames.CopyList();

                    #endregion

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            #endregion

            GC.Collect();

            return current;
        }

        public static List<FrameInfo> Redo(List<FrameInfo> current)
        {
            //Pop from Redo stack.
            var latestRedo = RedoStack.Pop();

            #region Push into Undo stack

            var undoStateChange = new StateChange();

            switch (latestRedo.Cause)
            {
                case EditAction.Remove:

                    #region Add (Inverse)

                    undoStateChange.Cause = EditAction.Add;
                    undoStateChange.Indexes = new List<int>(latestRedo.Indexes);

                    #endregion

                    break;
                case EditAction.Add:
                    
                    #region Remove (Inverse)

                    var savedFrames = new List<FrameInfo>();
                    var redoFolder = CreateCurrent(true);

                    undoStateChange.Cause = EditAction.Remove;
                    undoStateChange.Indexes = new List<int>(latestRedo.Indexes);

                    //Saves the frames that will be deleted (using the given list of positions).
                    foreach (var position in latestRedo.Indexes)
                    {
                        var frame = current[position];
                        var savedFrame = Path.Combine(redoFolder, Path.GetFileName(frame.ImageLocation));

                        //Copy to a folder.
                        File.Copy(frame.ImageLocation, savedFrame);

                        savedFrames.Add(new FrameInfo(savedFrame, frame.Delay, frame.CursorInfo));
                    }

                    undoStateChange.Frames = savedFrames;

                    #endregion

                    break;
                case EditAction.ImageAndProperties:

                    #region Alter the images and properties (Inverse)

                    var savedFrames2 = new List<FrameInfo>();
                    var redoFolder2 = CreateCurrent(false);

                    undoStateChange.Cause = EditAction.ImageAndProperties;
                    undoStateChange.Indexes = latestRedo.Indexes;

                    //Saves the frames that will be deleted (using the given list of positions).
                    foreach (var position in latestRedo.Indexes)
                    {
                        var frame = current[position];
                        var savedFrame = Path.Combine(redoFolder2, Path.GetFileName(frame.ImageLocation));

                        //Copy to a folder.
                        File.Copy(frame.ImageLocation, savedFrame);

                        savedFrames2.Add(new FrameInfo(savedFrame, frame.Delay, frame.CursorInfo));
                    }

                    undoStateChange.Frames = savedFrames2;

                    #endregion

                    break;
                case EditAction.Properties:

                    #region Alter the properties (Inverse)

                    undoStateChange.Cause = EditAction.Properties;
                    undoStateChange.Frames = latestRedo.Frames;
                    undoStateChange.Indexes = latestRedo.Indexes;

                    #endregion

                    break;

                case EditAction.Reorder:

                    #region Reorder (Inverse)

                    undoStateChange.Cause = EditAction.Reorder;
                    undoStateChange.Frames = current.CopyList();

                    #endregion

                    break;
            }

            UndoStack.Push(undoStateChange);

            #endregion

            #region Redo

            switch (latestRedo.Cause)
            {
                case EditAction.Remove:

                    #region Insert again the frames

                    if (latestRedo.Frames == null || latestRedo.Frames.Count == 0)
                        throw new Exception("No frames to redo.");

                    var folder = Path.GetDirectoryName(current[0].ImageLocation);

                    var currentIndex = 0;
                    foreach (var index in latestRedo.Indexes)
                    {
                        var frame = latestRedo.Frames[currentIndex];
                        var file = Path.Combine(folder, Path.GetFileName(frame.ImageLocation));

                        //Copy file to folder.
                        File.Copy(frame.ImageLocation, file);

                        //Add to list.
                        current.Insert(index, new FrameInfo(file, frame.Delay, frame.CursorInfo));

                        currentIndex++;
                    }

                    //Erase the redo folder.
                    Directory.Delete(Path.GetDirectoryName(latestRedo.Frames[0].ImageLocation), true);

                    #endregion

                    break;
                case EditAction.ImageAndProperties:

                    #region Alter the image

                    if (latestRedo.Frames == null || latestRedo.Frames.Count == 0)
                        throw new Exception("No frames to redo.");

                    var folder2 = Path.GetDirectoryName(current[0].ImageLocation);

                    foreach (var frame in latestRedo.Frames)
                    {
                        var file = Path.Combine(folder2, Path.GetFileName(frame.ImageLocation));

                        //Copy file to folder.
                        File.Copy(frame.ImageLocation, file, true);
                    }

                    //Erase the undo folder.
                    Directory.Delete(Path.GetDirectoryName(latestRedo.Frames[0].ImageLocation), true);

                    #endregion

                    break;
                case EditAction.Properties:

                    #region Alter the properties

                    if (latestRedo.Frames == null || latestRedo.Frames.Count == 0)
                        throw new Exception("No frames to redo.");

                    var alteredIndex = 0;
                    foreach (var frame in latestRedo.Frames)
                    {
                        current[latestRedo.Indexes[alteredIndex]] = new FrameInfo(frame.ImageLocation, frame.Delay, frame.CursorInfo);

                        alteredIndex++;
                    }

                    #endregion

                    break;
                case EditAction.Add:

                    #region Remove the added frames                   

                    foreach (var index in latestRedo.Indexes.OrderByDescending(x => x))
                    {
                        File.Delete(current[index].ImageLocation);

                        current.RemoveAt(index);
                    }

                    #endregion

                    break;
                case EditAction.Reorder:

                    #region Reorder the frames to a previous order

                    current = latestRedo.Frames.CopyList();

                    #endregion

                    break;
            }

            #endregion

            GC.Collect();

            return current;
        }

        public static List<FrameInfo> Reset(List<FrameInfo> current)
        {
            //TODO: Save the current state before resetting all.
            //Signal that it was reset.

            var count = UndoStack.Count;

            //Pop all iteration from Undo stack
            for (int i = 0; i < count; i++)
            {
                current = Undo(current, false);
            }

            ClearUndo();
            ClearRedo();

            return current;
        }

        #endregion

        #region Auxiliar

        /// <summary>
        /// Creates the folders used by the ActionStack.
        /// </summary>
        /// <param name="path">The file path of one of the frames.</param>
        public static void Prepare(string path)
        {
            _actualFolder = Path.GetDirectoryName(path);
            var actionPath = Path.Combine(_actualFolder, "ActionStack");

            if (!Directory.Exists(actionPath))
            {
                Directory.CreateDirectory(actionPath);
            }

            _undoFolder = Path.Combine(actionPath, "Undo");
            _redoFolder = Path.Combine(actionPath, "Redo");

            if (!Directory.Exists(_undoFolder))
            {
                Directory.CreateDirectory(_undoFolder);
            }

            if (!Directory.Exists(_redoFolder))
            {
                Directory.CreateDirectory(_redoFolder);
            }
        }

        /// <summary>
        ///Creates the destination folder where the frames will be stored.
        /// </summary>
        private static string CreateCurrent(bool isUndo)
        {
            var folder = Path.Combine(isUndo ? _undoFolder : _redoFolder, DateTime.Now.ToString("yy-MM-dd hh-mm-ss fff"));

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return folder;
        }


        /// <summary>
        /// Clear the Action Stack.
        /// </summary>
        public static void Clear()
        {
            ClearUndo();
            ClearRedo();
        }

        private static void ClearUndo()
        {
            try
            {
                foreach (var frame in UndoStack.Where(x => x.Frames != null)
                    .SelectMany(list => list.Frames.Where(frame => frame.ImageLocation != null && File.Exists(frame.ImageLocation) && frame.ImageLocation.Contains("ActionStack" + Path.DirectorySeparatorChar + "Undo"))))
                {
                    File.Delete(frame.ImageLocation);
                }
            }
            finally
            {
                UndoStack.Clear();
            }
        }

        private static void ClearRedo()
        {
            try
            {
                foreach (var frame in RedoStack.Where(x => x.Frames != null)
                    .SelectMany(list => list.Frames.Where(frame => frame.ImageLocation != null && File.Exists(frame.ImageLocation) && frame.ImageLocation.Contains("ActionStack" + Path.DirectorySeparatorChar + "Redo"))))
                {
                    File.Delete(frame.ImageLocation);
                }
            }
            finally
            {
                RedoStack.Clear();
            }
        }


        /// <summary>
        /// Verifies if the Undo stack has elements and nothing else is happening.
        /// </summary>
        /// <returns>True if able to Undo.</returns>
        public static bool CanUndo()
        {
            return UndoStack.Count > 0;
        }

        /// <summary>
        /// Verifies if the Redo stack has elements and nothing else is happening.
        /// </summary>
        /// <returns>True if able to Redo.</returns>
        public static bool CanRedo()
        {
            return RedoStack.Count > 0;
        }

        /// <summary>
        /// Verifies if it's possible to reset.
        /// </summary>
        /// <returns>True if able to Reset.</returns>
        public static bool CanReset()
        {
            //Can only reset if there's one or more state changes that won't be ignored. 
            return UndoStack.Count > 0 || (UndoStack.Count == 1 && UndoStack.All(x => !x.IgnoreWhenReset));
        }

        #endregion
    }
}
