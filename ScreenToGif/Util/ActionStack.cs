using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ScreenToGif.Model;

namespace ScreenToGif.Util
{
    /// <summary>
    /// Do, Undo, Redo stack.
    /// </summary>
    public static class ActionStack
    {
        #region Variables

        public static ProjectInfo Project { get; set; }

        private static readonly ExtendedStack<StateChange> UndoStack = new ExtendedStack<StateChange>();
        private static readonly ExtendedStack<StateChange> RedoStack = new ExtendedStack<StateChange>();

        #endregion

        public enum EditAction
        {
            Remove,
            ImageAndProperties,
            Properties,
            Add,
            Reorder,
            RemoveAndAlter,
            AddAndAlter,
        }

        public class StateChange
        {
            public EditAction Cause { get; set; }
            public List<FrameInfo> Frames { get; set; }
            public string CurrentFolder { get; set; }

            public List<int> Indexes { get; set; }
            public List<int> Indexes2 { get; set; }

            /// <summary>
            /// Signals that this state should not be available.
            /// </summary>
            public bool IgnoreWhenReset { get; set; }
        }

        #region Save State

        public static void SaveState(EditAction action, List<FrameInfo> frames, List<int> positions)
        {
            if (!ShouldSaveState())
                return;

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
                        var savedFrame = Path.Combine(currentFolder, Path.GetFileName(frame.Path)); // position + ".png");

                        //Copy to a folder.
                        File.Copy(frame.Path, savedFrame);

                        savedFrames.Add(new FrameInfo(savedFrame, frame.Delay, frame.CursorX, frame.CursorY, frame.WasClicked, frame.KeyList, frame.Index));
                    }

                    //Create a StageChange object with the saved frames and push to the undo stack.
                    UndoStack.Push(new StateChange
                    {
                        Cause = action,
                        Frames = savedFrames,
                        CurrentFolder = currentFolder,
                        Indexes = orderedPositions
                    });

                    break;
                case EditAction.ImageAndProperties:

                    //Saves the frames that will be altered (using the given list of positions).
                    foreach (var position in orderedPositions)
                    {
                        var frame = frames[position];
                        var savedFrame = Path.Combine(currentFolder, Path.GetFileName(frame.Path)); // position + ".png");

                        //Copy to a folder.
                        File.Copy(frame.Path, savedFrame);

                        savedFrames.Add(new FrameInfo(savedFrame, frame.Delay, frame.CursorX, frame.CursorY, frame.WasClicked, frame.KeyList, frame.Index));
                    }

                    //Create a StageChange object with the saved frames and push to the undo stack.
                    UndoStack.Push(new StateChange
                    {
                        Cause = action,
                        Frames = savedFrames,
                        CurrentFolder = currentFolder,
                        Indexes = orderedPositions
                    });

                    break;
                case EditAction.Properties:

                    //Saves the frames that will be altered, without copying the images (using the given list of positions).
                    foreach (var position in orderedPositions)
                    {
                        var frame = frames[position];

                        savedFrames.Add(new FrameInfo(frame.Path, frame.Delay, frame.CursorX, frame.CursorY, frame.WasClicked, frame.KeyList, frame.Index));
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

            ClearRedo();
            TrimUndo();
        }

        public static void SaveState(EditAction action, List<FrameInfo> frames, List<int> removeList, List<int> alterList)
        {
            if (action != EditAction.RemoveAndAlter)
                throw new ArgumentException("Parameters different than RemoveAndAlter are not supported.", nameof(action));

            if (!ShouldSaveState())
                return;

            var savedFrames = new List<FrameInfo>();
            var currentFolder = CreateCurrent(true);

            #region Removed

            //Saves the frames that will be deleted (using the given list of positions).
            foreach (var position in removeList)
            {
                var frame = frames[position];
                var savedFrame = Path.Combine(currentFolder, Path.GetFileName(frame.Path));

                //Copy to a folder.
                File.Copy(frame.Path, savedFrame);

                savedFrames.Add(new FrameInfo(savedFrame, frame.Delay, frame.CursorX, frame.CursorY, frame.WasClicked, frame.KeyList, frame.Index));
            }

            #endregion

            #region Altered

            //Saves the frames that will be altered, without copying the images (using the given list of positions).
            foreach (var position in alterList)
            {
                var frame = frames[position];

                savedFrames.Add(new FrameInfo(frame.Path, frame.Delay, frame.CursorX, frame.CursorY, frame.WasClicked, frame.KeyList, frame.Index));
            }

            #endregion

            UndoStack.Push(new StateChange
            {
                Cause = action,
                Frames = savedFrames,
                CurrentFolder = currentFolder, //Ignore this property when frame is set as "Altered".
                Indexes = removeList,
                Indexes2 = alterList,
            });

            ClearRedo();
            TrimUndo();
        }

        /// <summary>
        /// Save the state of the list of frames. This overload is used by the EditAction.Add.
        /// </summary>
        /// <param name="action">The action, currently just the Add.</param>
        /// <param name="position">The position where the frames will be inserted.</param>
        /// <param name="quantity">The quantity of inserted frames.</param>
        public static void SaveState(EditAction action, int position, int quantity)
        {
            if (action != EditAction.Add)
                throw new ArgumentException("Parameters different than Add are not supported.", nameof(action));

            if (!ShouldSaveState())
                return;

            //Saves the position where the new frames will be inserted.
            UndoStack.Push(new StateChange
            {
                Cause = action,
                Indexes = Util.Other.ListOfIndexes(position, quantity)
            });

            ClearRedo();
            TrimUndo();
        }

        /// <summary>
        /// Save the state of the list of frames. This overload is used by the EditAction.Reorder.
        /// </summary>
        /// <param name="action">The action, currently just the Reorder.</param>
        /// <param name="frames">The old (current) list of frames.</param>
        public static void SaveState(EditAction action, List<FrameInfo> frames)
        {
            if (action != EditAction.Reorder)
                throw new ArgumentException("Parameters different than Reorder are not supported.", nameof(action));

            if (!ShouldSaveState())
                return;

            //Saves the frames before the reordering.
            UndoStack.Push(new StateChange
            {
                Cause = action,
                Frames = frames,
            });

            ClearRedo();
            TrimUndo();
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
                            var savedFrame = Path.Combine(redoFolder, Path.GetFileName(frame.Path));

                            //Copy to a folder.
                            File.Copy(frame.Path, savedFrame);

                            savedFrames.Add(new FrameInfo(savedFrame, frame.Delay, frame.CursorX, frame.CursorY, frame.WasClicked, frame.KeyList, frame.Index));
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
                            var savedFrame = Path.Combine(redoFolder2, Path.GetFileName(frame.Path));

                            //Copy to a folder.
                            File.Copy(frame.Path, savedFrame);

                            savedFrames2.Add(new FrameInfo(savedFrame, frame.Delay, frame.CursorX, frame.CursorY, frame.WasClicked, frame.KeyList, frame.Index));
                        }

                        redoStateChange.Frames = savedFrames2;

                        #endregion

                        break;
                    case EditAction.Properties:

                        #region Alter the properties (Inverse)

                        redoStateChange.Cause = EditAction.Properties;
                        redoStateChange.Frames = new List<FrameInfo>(latestUndo.Frames);
                        redoStateChange.Indexes = new List<int>(latestUndo.Indexes);

                        #endregion

                        break;
                    case EditAction.Reorder:

                        #region Reorder (Inverse)

                        redoStateChange.Cause = EditAction.Reorder;
                        redoStateChange.Frames = current.CopyList();

                        #endregion

                        break;
                    case EditAction.RemoveAndAlter:

                        #region Add and alter the properties (Inverse)

                        redoStateChange.Cause = EditAction.AddAndAlter;
                        //Save only the altered frames.
                        redoStateChange.Frames = new List<FrameInfo>(latestUndo.Frames).Skip(latestUndo.Indexes.Count).Take(latestUndo.Indexes2.Count).ToList();
                        redoStateChange.Indexes = new List<int>(latestUndo.Indexes);
                        redoStateChange.Indexes2 = new List<int>(latestUndo.Indexes2);

                        #endregion

                        break;

                    case EditAction.AddAndAlter: //Check.

                        #region Remove and alter the properties (Inverse)

                        var savedFrames3 = new List<FrameInfo>();
                        var redoFolder3 = CreateCurrent(false);

                        redoStateChange.Cause = EditAction.RemoveAndAlter;
                        redoStateChange.Indexes = new List<int>(latestUndo.Indexes);
                        redoStateChange.Indexes2 = new List<int>(latestUndo.Indexes2);

                        //Saves the frames that will be deleted (using the given list of positions).
                        foreach (var position in latestUndo.Indexes)
                        {
                            var frame = current[position];
                            var savedFrame = Path.Combine(redoFolder3, Path.GetFileName(frame.Path));

                            //Copy to a folder.
                            File.Copy(frame.Path, savedFrame);

                            savedFrames3.Add(new FrameInfo(savedFrame, frame.Delay, frame.CursorX, frame.CursorY, frame.WasClicked, frame.KeyList, frame.Index));
                        }

                        //Saves the altered frames, without saving the images.
                        foreach (var position in latestUndo.Indexes2)
                        {
                            var frame = current[position];
                            savedFrames3.Add(new FrameInfo(frame.Path, frame.Delay, frame.CursorX, frame.CursorY, frame.WasClicked, frame.KeyList, frame.Index));
                        }

                        redoStateChange.Frames = savedFrames3;

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

                    var folder = Path.GetDirectoryName(current[0].Path);

                    var currentIndex = 0;
                    foreach (var index in latestUndo.Indexes)
                    {
                        var frame = latestUndo.Frames[currentIndex];
                        var file = Path.Combine(folder, Path.GetFileName(frame.Path));

                        //Copy file to folder.
                        File.Copy(frame.Path, file);

                        //Add to list.
                        current.Insert(index, new FrameInfo(file, frame.Delay, frame.CursorX, frame.CursorY, frame.WasClicked, frame.KeyList, frame.Index));

                        currentIndex++;
                    }

                    //Erase the undo folder.
                    Directory.Delete(Path.GetDirectoryName(latestUndo.Frames[0].Path), true);

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

                        current[latestUndo.Indexes[alteredIndex2]] = new FrameInfo(currentFrame.Path, frame.Delay, frame.CursorX, frame.CursorY, frame.WasClicked, frame.KeyList, frame.Index); //Image location stays the same.

                        //Copy file to folder.
                        File.Copy(frame.Path, currentFrame.Path, true);

                        alteredIndex2++;
                    }

                    //Erase the undo folder.
                    Directory.Delete(Path.GetDirectoryName(latestUndo.Frames[0].Path), true);

                    #endregion

                    break;
                case EditAction.Properties:

                    #region Alter the properties

                    if (latestUndo.Frames == null || latestUndo.Frames.Count == 0)
                        throw new Exception("No frames to undo.");

                    var alteredIndex = 0;
                    foreach (var frame in latestUndo.Frames)
                    {
                        current[latestUndo.Indexes[alteredIndex]] = new FrameInfo(frame.Path, frame.Delay, frame.CursorX, frame.CursorY, frame.WasClicked, frame.KeyList, frame.Index);

                        alteredIndex++;
                    }

                    #endregion

                    break;
                case EditAction.Add:

                    #region Remove the added frames

                    foreach (var index in latestUndo.Indexes.OrderByDescending(x => x))
                    {
                        File.Delete(current[index].Path);

                        current.RemoveAt(index);
                    }

                    #endregion

                    break;
                case EditAction.Reorder:

                    #region Reorder the frames to a previous order

                    current = latestUndo.Frames.CopyList();

                    #endregion

                    break;
                case EditAction.AddAndAlter: //Check.

                    #region Remove the added frames and alter the properties

                    for (var i = latestUndo.Indexes.Count -1; i >= 0; i--)
                    {
                        var removeIndex = latestUndo.Indexes[i];

                        //Alter the properties.
                        if (removeIndex > 0)
                            current[removeIndex - 1].Delay = current[removeIndex].Delay;

                        //Remove the file.
                        File.Delete(current[removeIndex].Path);
                        current.RemoveAt(removeIndex);
                    }

                    #endregion

                    break;
                case EditAction.RemoveAndAlter:

                    #region Insert again the frames

                    if (latestUndo.Frames == null || latestUndo.Frames.Count == 0)
                        throw new Exception("No frames to undo.");

                    var folder2 = Path.GetDirectoryName(current[0].Path);

                    var currentIndex2 = 0;
                    foreach (var index in latestUndo.Indexes)
                    {
                        var frame = latestUndo.Frames[currentIndex2];
                        var file = Path.Combine(folder2, Path.GetFileName(frame.Path));

                        //Copy file to folder.
                        File.Copy(frame.Path, file);

                        //Add to list.
                        current.Insert(index, new FrameInfo(file, frame.Delay, frame.CursorX, frame.CursorY, frame.WasClicked, frame.KeyList, frame.Index));

                        currentIndex2++;
                    }

                    //Erase the undo folder.
                    Directory.Delete(Path.GetDirectoryName(latestUndo.Frames[0].Path), true);

                    #endregion

                    #region Alter the properties

                    var alteredIndex3 = 0;
                    foreach (var frame in latestUndo.Frames.Skip(latestUndo.Indexes.Count))
                    {
                        current[latestUndo.Indexes2[alteredIndex3]] = new FrameInfo(frame.Path, frame.Delay, frame.CursorX, frame.CursorY, frame.WasClicked, frame.KeyList, frame.Index);

                        alteredIndex3++;
                    }

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
                        var savedFrame = Path.Combine(redoFolder, Path.GetFileName(frame.Path));

                        //Copy to a folder.
                        File.Copy(frame.Path, savedFrame);

                        savedFrames.Add(new FrameInfo(savedFrame, frame.Delay, frame.CursorX, frame.CursorY, frame.WasClicked, frame.KeyList, frame.Index));
                    }

                    undoStateChange.Frames = savedFrames;

                    #endregion

                    break;
                case EditAction.ImageAndProperties:

                    #region Alter the images and properties (Inverse)

                    var savedFrames2 = new List<FrameInfo>();
                    var redoFolder2 = CreateCurrent(false);

                    undoStateChange.Cause = EditAction.ImageAndProperties;
                    undoStateChange.Indexes = new List<int>(latestRedo.Indexes);

                    //Saves the frames that will be deleted (using the given list of positions).
                    foreach (var position in latestRedo.Indexes)
                    {
                        var frame = current[position];
                        var savedFrame = Path.Combine(redoFolder2, Path.GetFileName(frame.Path));

                        //Copy to a folder.
                        File.Copy(frame.Path, savedFrame);

                        savedFrames2.Add(new FrameInfo(savedFrame, frame.Delay, frame.CursorX, frame.CursorY, frame.WasClicked, frame.KeyList, frame.Index));
                    }

                    undoStateChange.Frames = savedFrames2;

                    #endregion

                    break;
                case EditAction.Properties:

                    #region Alter the properties (Inverse)

                    undoStateChange.Cause = EditAction.Properties;
                    undoStateChange.Frames = new List<FrameInfo>(latestRedo.Frames);
                    undoStateChange.Indexes = new List<int>(latestRedo.Indexes);

                    #endregion

                    break;

                case EditAction.Reorder:

                    #region Reorder (Inverse)

                    undoStateChange.Cause = EditAction.Reorder;
                    undoStateChange.Frames = current.CopyList();

                    #endregion

                    break;

                case EditAction.RemoveAndAlter:

                    #region Add and alter the properties (Inverse)

                    undoStateChange.Cause = EditAction.AddAndAlter;
                    //Save only the altered frames.
                    undoStateChange.Frames = new List<FrameInfo>(latestRedo.Frames).Skip(latestRedo.Indexes.Count).Take(latestRedo.Indexes2.Count).ToList();
                    undoStateChange.Indexes = new List<int>(latestRedo.Indexes);
                    undoStateChange.Indexes2 = new List<int>(latestRedo.Indexes2);

                    #endregion

                    break;

                case EditAction.AddAndAlter: //Check.

                    #region Remove and alter the properties (Inverse)

                    var savedFrames3 = new List<FrameInfo>();
                    var redoFolder3 = CreateCurrent(false);

                    undoStateChange.Cause = EditAction.RemoveAndAlter;
                    undoStateChange.Indexes = new List<int>(latestRedo.Indexes);
                    undoStateChange.Indexes2 = new List<int>(latestRedo.Indexes2);

                    //Saves the frames that will be deleted (using the given list of positions).
                    foreach (var position in latestRedo.Indexes)
                    {
                        var frame = current[position];
                        var savedFrame = Path.Combine(redoFolder3, Path.GetFileName(frame.Path));

                        //Copy to a folder.
                        File.Copy(frame.Path, savedFrame);

                        savedFrames3.Add(new FrameInfo(savedFrame, frame.Delay, frame.CursorX, frame.CursorY, frame.WasClicked, frame.KeyList, frame.Index));
                    }

                    //Saves the altered frames, without saving the images.
                    foreach (var position in latestRedo.Indexes2)
                    {
                        var frame = current[position];
                        savedFrames3.Add(new FrameInfo(frame.Path, frame.Delay, frame.CursorX, frame.CursorY, frame.WasClicked, frame.KeyList, frame.Index));
                    }

                    undoStateChange.Frames = savedFrames3;

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

                    var folder = Path.GetDirectoryName(current[0].Path);

                    var currentIndex = 0;
                    foreach (var index in latestRedo.Indexes)
                    {
                        var frame = latestRedo.Frames[currentIndex];
                        var file = Path.Combine(folder, Path.GetFileName(frame.Path));

                        //Copy file to folder.
                        File.Copy(frame.Path, file);

                        //Add to list.
                        current.Insert(index, new FrameInfo(file, frame.Delay, frame.CursorX, frame.CursorY, frame.WasClicked, frame.KeyList, frame.Index));

                        currentIndex++;
                    }

                    //Erase the redo folder.
                    Directory.Delete(Path.GetDirectoryName(latestRedo.Frames[0].Path), true);

                    #endregion

                    break;
                case EditAction.ImageAndProperties:

                    #region Alter the image

                    if (latestRedo.Frames == null || latestRedo.Frames.Count == 0)
                        throw new Exception("No frames to redo.");

                    var folder2 = Path.GetDirectoryName(current[0].Path);

                    foreach (var frame in latestRedo.Frames)
                    {
                        var file = Path.Combine(folder2, Path.GetFileName(frame.Path));

                        //Copy file to folder.
                        File.Copy(frame.Path, file, true);
                    }

                    //Erase the undo folder.
                    Directory.Delete(Path.GetDirectoryName(latestRedo.Frames[0].Path), true);

                    #endregion

                    break;
                case EditAction.Properties:

                    #region Alter the properties

                    if (latestRedo.Frames == null || latestRedo.Frames.Count == 0)
                        throw new Exception("No frames to redo.");

                    var alteredIndex = 0;
                    foreach (var frame in latestRedo.Frames)
                    {
                        current[latestRedo.Indexes[alteredIndex]] = new FrameInfo(frame.Path, frame.Delay, frame.CursorX, frame.CursorY, frame.WasClicked, frame.KeyList, frame.Index);

                        alteredIndex++;
                    }

                    #endregion

                    break;
                case EditAction.Add:

                    #region Remove the added frames                   

                    foreach (var index in latestRedo.Indexes.OrderByDescending(x => x))
                    {
                        File.Delete(current[index].Path);

                        current.RemoveAt(index);
                    }

                    #endregion

                    break;
                case EditAction.Reorder:

                    #region Reorder the frames to a previous order

                    current = latestRedo.Frames.CopyList();

                    #endregion

                    break;

                case EditAction.AddAndAlter: //Check.

                    #region Remove the added frames and alter the properties

                    for (var i = latestRedo.Indexes.Count - 1; i >= 0; i--)
                    {
                        var removeIndex = latestRedo.Indexes[i];

                        //Alter the properties.
                        if (removeIndex > 0)
                            current[removeIndex - 1].Delay += current[removeIndex].Delay;

                        //Remove the file.
                        File.Delete(current[removeIndex].Path);
                        current.RemoveAt(removeIndex);
                    }

                    #endregion

                    break;
                case EditAction.RemoveAndAlter:

                    #region Insert again the frames

                    if (latestRedo.Frames == null || latestRedo.Frames.Count == 0)
                        throw new Exception("No frames to redo.");

                    var folder3 = Path.GetDirectoryName(current[0].Path);

                    var currentIndex2 = 0;
                    foreach (var index in latestRedo.Indexes)
                    {
                        var frame = latestRedo.Frames[currentIndex2];
                        var file = Path.Combine(folder3, Path.GetFileName(frame.Path));

                        //Copy file to folder.
                        File.Copy(frame.Path, file);

                        //Add to list.
                        current.Insert(index, new FrameInfo(file, frame.Delay, frame.CursorX, frame.CursorY, frame.WasClicked, frame.KeyList, frame.Index));

                        currentIndex2++;
                    }

                    //Erase the undo folder.
                    Directory.Delete(Path.GetDirectoryName(latestRedo.Frames[0].Path), true);

                    #endregion

                    #region Alter the properties

                    var alteredIndex3 = 0;
                    foreach (var frame in latestRedo.Frames.Skip(latestRedo.Indexes.Count))
                    {
                        current[latestRedo.Indexes2[alteredIndex3]] = new FrameInfo(frame.Path, frame.Delay, frame.CursorX, frame.CursorY, frame.WasClicked, frame.KeyList, frame.Index);

                        alteredIndex3++;
                    }

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
            for (var i = 0; i < count; i++)
                current = Undo(current, false);
            
            ClearUndo();
            ClearRedo();

            return current;
        }

        #endregion

        #region Auxiliar

        /// <summary>
        ///Creates the destination folder where the frames will be stored.
        /// </summary>
        private static string CreateCurrent(bool isUndo)
        {
            var folder = Path.Combine(isUndo ? Project.UndoStackPath : Project.RedoStackPath, DateTime.Now.ToString("yy-MM-dd hh-mm-ss fff"));

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
                    .SelectMany(list => list.Frames.Where(frame => frame.Path != null && File.Exists(frame.Path) && frame.Path.Contains("ActionStack" + Path.DirectorySeparatorChar + "Undo"))))
                {
                    File.Delete(frame.Path);
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
                    .SelectMany(list => list.Frames.Where(frame => frame.Path != null && File.Exists(frame.Path) && frame.Path.Contains("ActionStack" + Path.DirectorySeparatorChar + "Redo"))))
                {
                    File.Delete(frame.Path);
                }
            }
            finally
            {
                RedoStack.Clear();
            }
        }

        private static void TrimUndo()
        {
            if (!UserSettings.All.SetHistoryLimit)
                return;

            if (UndoStack.Count <= UserSettings.All.HistoryLimit)
                return;

            try
            {
                for (var i = UserSettings.All.HistoryLimit; i < UndoStack.Count; i++)
                {
                    var last = UndoStack.PopBottom();

                    if (last?.Frames == null)
                        continue;

                    foreach (var frame in last.Frames.Where(frame => frame.Path != null && File.Exists(frame.Path) && frame.Path.Contains("ActionStack" + Path.DirectorySeparatorChar + "Undo")))
                        File.Delete(frame.Path);
                }
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Impossible to trim the undo stack.");
            }
        }


        public static bool ShouldSaveState()
        {
            return !UserSettings.All.SetHistoryLimit || UserSettings.All.HistoryLimit > 0;
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