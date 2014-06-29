using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// We use this TreeView instead of delivered one,
    /// cos the last has a bug with double click event.
    /// </summary>
    /// <example>
    /// http://social.msdn.microsoft.com/Forums/windows/en-US/9d717ce0-ec6b-4758-a357-6bb55591f956/possible-bug-in-net-treeview-treenode-checked-state-inconsistent?forum=winforms
    /// </example>
    public class NoDoubleClickTreeView : TreeView
    {
        /// <summary>
        /// Update the list of frames on the TreeView control
        /// </summary>
        /// <param name="frameCount">Total number of frames, should not be 0</param>
        /// <param name="parentNodeLabel">Label that will be displayed on the parent node</param>
        public void UpdateListFrames(int frameCount, string parentNodeLabel)
        {
            this.Cursor = Cursors.WaitCursor;

            if (frameCount <= 0) return;

            #region If Frame Count > 0

            //Remove before inserting new node
            this.Nodes.Clear();

            var arrayNode = new TreeNode[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                //Without + 1, Starts with Zero.
                arrayNode[i] = new TreeNode("Frame " + i);
            }

            // Finalize of the list
            this.BeginUpdate();
            this.Nodes.Add(new TreeNode(parentNodeLabel, arrayNode));
            this.Nodes[0].Name = parentNodeLabel;
            
            this.EndUpdate();

            // Display the list of frames
            if (!this.Nodes[0].IsExpanded)
                this.ExpandAll();

            Application.DoEvents();

            this.Cursor = Cursors.Default;

            #endregion
        }

        /// <summary>
        ///Adds more frames on the TreeView control
        /// </summary>
        /// <param name="frameCount">The ammount of frames to add, should not be 0</param>
        public void Add(int frameCount)
        {
            if (frameCount <= 0) return;
            if (this.Nodes.Count != 1) return;

            //without the -1 because the for starts with 0
            int nodeInside = this.Nodes[0].Nodes.Count;

            this.BeginUpdate();
            for (int i = 0; i < frameCount; i++)
            {
                this.Nodes[0].Nodes.Add("Frame " + (i + nodeInside));
            }
            this.EndUpdate();
        }

        /// <summary>
        ///Deletes frames on the TreeView control
        /// </summary>
        /// <param name="frameCount">Total number of frames, should not be 0</param>
        public void Remove(int frameCount)
        {
            if (frameCount <= 0) return;
            if (this.Nodes.Count != 1) return;

            var nodeInside = this.Nodes[0].Nodes.Count;

            this.BeginUpdate();
            for (int i = 0; i < frameCount; i++)
            {
                this.Nodes[0].Nodes.RemoveAt(nodeInside - (i + 1));
            }
            this.EndUpdate();
        }

        /// <summary>
        /// Disable double click event on the TreeView control
        /// </summary>
        /// <param name="m">A Windows Message as reference</param>
        protected override void WndProc(ref Message m)
        {
            // Suppress WM_LBUTTONDBLCLK
            if (m.Msg == 0x203) { m.Result = IntPtr.Zero; }
            else base.WndProc(ref m);
        }
    }
}