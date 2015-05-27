using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenToGif.Util
{
    /// <summary>
    /// Do, Undo, Redo stack.
    /// </summary>
    public static class ActionStack
    {
        private static readonly Stack<List<FrameInfo>> UndoStack = new Stack<List<FrameInfo>>();
        private static readonly Stack<List<FrameInfo>> RedoStack = new Stack<List<FrameInfo>>();

        /// <summary>
        /// Do a change to the list.
        /// </summary>
        /// <param name="list">The List of frames to stack.</param>
        public static void Do(List<FrameInfo> list)
        {
            //TODO: Cant enter here while Undoing or Redoing, use a flag.

            UndoStack.Push(list);

            RedoStack.Clear();
        }

        /// <summary>
        /// Redo the last action.
        /// </summary>
        /// <returns>The List to Undo.</returns>
        public static List<FrameInfo> Undo()
        {
            if (CanUndo())
                return null;

            RedoStack.Push(UndoStack.Peek());

            return UndoStack.Pop();
        }

        /// <summary>
        /// Redo the last Undo action.
        /// </summary>
        /// <returns>The List to Redo.</returns>
        public static List<FrameInfo> Redo()
        {
            if (CanRedo())
                return null;

            UndoStack.Push(RedoStack.Peek());

            return RedoStack.Pop();
        }

        /// <summary>
        /// Verifies if the Undo stack has elements.
        /// </summary>
        /// <returns>True if able to Undo.</returns>
        public static bool CanUndo()
        {
            return UndoStack.Count > 0;
        }

        /// <summary>
        /// Verifies if the Redo stack has elements.
        /// </summary>
        /// <returns>True if able to Redo.</returns>
        public static bool CanRedo()
        {
            return RedoStack.Count > 0;
        }
    }
}
