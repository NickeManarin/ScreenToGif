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

        private static string _actualFolder;
        private static string _undoFolder;
        private static string _redoFolder;
        private static readonly Stack<List<FrameInfo>> UndoStack = new Stack<List<FrameInfo>>();
        private static readonly Stack<List<FrameInfo>> RedoStack = new Stack<List<FrameInfo>>();

        private static bool _happening = false;

        #endregion

        /// <summary>
        /// Add the change to the list.
        /// </summary>
        /// <param name="list">The List of frames to stack.</param>
        /// <param name="changedList">Index list of the changed frames. If null or empty, act like everything changed.</param>
        public static void Did(List<FrameInfo> list, List<int> changedList)
        {
            _happening = true;

            var newList = new List<FrameInfo>(); //list.CopyList();
            var folder = Path.Combine(_undoFolder, UndoStack.Count.ToString());

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            //Save the current list to a dynamic folder.
            foreach (FrameInfo frameInfo in list)
            {
                if (!changedList.Contains(list.IndexOf(frameInfo)))
                {
                    newList.Add(new FrameInfo(null, 0));
                    continue;
                }

                var filename = Path.Combine(folder, Path.GetFileName(frameInfo.ImageLocation));

                File.Copy(frameInfo.ImageLocation, filename, true);

                newList.Add(new FrameInfo(filename, frameInfo.Delay));
            }

            UndoStack.Push(newList);
            ClearRedo();
            GC.Collect();

            _happening = false;
        }

        public static void Did(List<FrameInfo> list, int firstIndex)
        {
            _happening = true;

            var newList = new List<FrameInfo>(); //list.CopyList();
            var folder = Path.Combine(_undoFolder, UndoStack.Count.ToString());

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            //Save the current list to a dynamic folder.
            foreach (FrameInfo frameInfo in list)
            {
                //Ignore unchanged frames.
                if (list.IndexOf(frameInfo) < firstIndex)
                {
                    newList.Add(new FrameInfo(null, 0));
                    continue;
                }

                var filename = Path.Combine(folder, Path.GetFileName(frameInfo.ImageLocation));

                File.Copy(frameInfo.ImageLocation, filename, true);

                newList.Add(new FrameInfo(filename, frameInfo.Delay));
            }

            UndoStack.Push(newList);
            ClearRedo();
            GC.Collect();

            _happening = false;
        }

        /// <summary>
        /// Add the change to the list.
        /// </summary>
        /// <param name="list">The List of frames to stack.</param>
        public static void Did(List<FrameInfo> list)
        {
            _happening = true;

            var newList = list.CopyList();
            var folder = Path.Combine(_undoFolder, UndoStack.Count.ToString());

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            //Save the current list to a dynamic folder.
            foreach (FrameInfo frameInfo in newList)
            {
                var filename = Path.Combine(folder, Path.GetFileName(frameInfo.ImageLocation));

                File.Copy(frameInfo.ImageLocation, filename, true);

                frameInfo.ImageLocation = filename;
            }

            UndoStack.Push(newList);
            ClearRedo();
            GC.Collect();

            _happening = false;
        }

        /// <summary>
        /// Redo the last action.
        /// </summary>
        /// <returns>The List to Undo.</returns>
        public static List<FrameInfo> Undo(List<FrameInfo> list)
        {
            #region Push into Redo

            var folder = Path.Combine(_redoFolder, RedoStack.Count.ToString());

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            //Save the current list to a dynamic folder.
            foreach (FrameInfo frameInfo in list)
            {
                var filename = Path.Combine(folder, Path.GetFileName(frameInfo.ImageLocation));

                File.Copy(frameInfo.ImageLocation, filename, true);

                frameInfo.ImageLocation = filename;
            }

            RedoStack.Push(list);

            #endregion

            #region Pop the Undo

            var undoItem = UndoStack.Pop();

            foreach (FrameInfo frameInfo in undoItem)
            {
                if (frameInfo.ImageLocation == null)
                {
                    var index = undoItem.IndexOf(frameInfo);
                    frameInfo.ImageLocation = list[index].ImageLocation;
                    frameInfo.Delay = list[index].Delay;
                    continue;
                }

                var filename = Path.Combine(_actualFolder, Path.GetFileName(frameInfo.ImageLocation));

                File.Copy(frameInfo.ImageLocation, filename, true);

                frameInfo.ImageLocation = filename;
            }

            #endregion

            GC.Collect();
            return undoItem;
        }

        /// <summary>
        /// Redo the last Undo action.
        /// </summary>
        /// <returns>The List to Redo.</returns>
        public static List<FrameInfo> Redo(List<FrameInfo> list)
        {
            #region Push into Undo

            var folder = Path.Combine(_undoFolder, UndoStack.Count.ToString());

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            //Save the current list to a dynamic folder.
            foreach (FrameInfo frameInfo in list)
            {
                var filename = Path.Combine(folder, Path.GetFileName(frameInfo.ImageLocation));

                File.Copy(frameInfo.ImageLocation, filename, true);

                frameInfo.ImageLocation = filename;
            }

            UndoStack.Push(list);

            #endregion

            #region Pop the Redo

            var redoItem = RedoStack.Pop();

            foreach (FrameInfo frameInfo in redoItem)
            {
                var filename = Path.Combine(_actualFolder, Path.GetFileName(frameInfo.ImageLocation));

                File.Copy(frameInfo.ImageLocation, filename, true);

                frameInfo.ImageLocation = filename;
            }

            #endregion

            GC.Collect();
            return redoItem;
        }

        /// <summary>
        /// Resets the ActionStack, clearing everything.
        /// </summary>
        /// <returns>The List to Undo.</returns>
        public static List<FrameInfo> Reset(List<FrameInfo> list)
        {
            #region Pop the Undo

            var undoItem = UndoStack.Last().CopyList();
 
            foreach (FrameInfo frameInfo in undoItem)
            {
                var filename = Path.Combine(_actualFolder, Path.GetFileName(frameInfo.ImageLocation));

                File.Copy(frameInfo.ImageLocation, filename, true);

                frameInfo.ImageLocation = filename;
            }

            #endregion

            ClearUndo();
            ClearRedo();

            Did(list);

            GC.Collect();
            return undoItem;
        }

        #region Auxiliar

        /// <summary>
        /// Creates the folders used by the ActionStack.
        /// </summary>
        /// <param name="path">The file path of one of the frames.</param>
        public static void Prepare(string path)
        {
            _actualFolder = Path.GetDirectoryName(path);
            string actionPath = Path.Combine(_actualFolder, "ActionStack");

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
                foreach (var frame in UndoStack.SelectMany(list => list.Where(frame => frame.ImageLocation != null && File.Exists(frame.ImageLocation))))
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
                foreach (var frame in RedoStack.SelectMany(list => list.Where(frame => frame.ImageLocation != null && File.Exists(frame.ImageLocation))))
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
            return UndoStack.Count > 0 && !_happening;
        }

        /// <summary>
        /// Verifies if the Redo stack has elements and nothing else is happening.
        /// </summary>
        /// <returns>True if able to Redo.</returns>
        public static bool CanRedo()
        {
            return RedoStack.Count > 0 && !_happening;
        }

        #endregion
    }
}
