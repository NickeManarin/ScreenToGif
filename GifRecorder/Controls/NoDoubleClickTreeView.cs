using System;
using System.Collections;
using System.Windows.Documents;
using System.Windows.Forms;
using ScreenToGif.Properties;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// We use this TreeView instead of delivered one,
    /// because the last has a bug with double click event.
    /// </summary>
    /// <example>
    /// http://social.msdn.microsoft.com/Forums/windows/en-US/9d717ce0-ec6b-4758-a357-6bb55591f956/possible-bug-in-net-treeview-treenode-checked-state-inconsistent?forum=winforms
    /// </example>
    public class NoDoubleClickTreeView : TreeView
    {
        #region Properties

        private bool _shift = false;
        private int _first = -1;
        private int _last = -1;

        /// <summary>
        /// True if the Shift key is pressed.
        /// </summary>
        public bool Shift
        {
            get { return _shift; }
            set { _shift = value; }
        }

        /// <summary>
        /// The first frame checked.
        /// </summary>
        public int First
        {
            get { return _first; }
            set { _first = value; }
        }

        /// <summary>
        /// The last frame checked.
        /// </summary>
        public int Last
        {
            get { return _last; }
            set { _last = value; }
        }

        #endregion

        /// <summary>
        /// Update the list of frames on the TreeView control
        /// </summary>
        /// <param name="frameCount">Total number of frames, should not be 0</param>
        /// <param name="parentNodeLabel">Label that will be displayed on the parent node</param>
        public void UpdateListFrames(int frameCount, string parentNodeLabel)
        {
            this.Cursor = Cursors.WaitCursor;

            this.KeyDown += NoDoubleClickTreeView_KeyDown;
            this.KeyUp += NoDoubleClickTreeView_KeyUp;

            if (frameCount <= 0) return;

            #region If Frame Count > 0

            //Remove before inserting new node
            this.Nodes.Clear();

            var arrayNode = new TreeNode[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                //Without + 1, Starts with Zero.
                arrayNode[i] = new TreeNode(Resources.Msg_Frame + " " + i);
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

        #region Events

        void NoDoubleClickTreeView_KeyUp(object sender, KeyEventArgs e)
        {
            Shift = e.Shift;
        }

        void NoDoubleClickTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            Shift = e.Shift;
        }

        #endregion

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
                this.Nodes[0].Nodes.Add(Resources.Msg_Frame + " " + (i + nodeInside));
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
        /// Uncheck all this.Nodes[0].nodes from this control.
        /// </summary>
        public void UncheckAll()
        {
            for (int i = 0; i < this.Nodes[0].Nodes.Count; i++)
            {
                this.Nodes[0].Nodes[i].Checked = false;
            }
        }

        /// <summary>
        /// Check all this.Nodes[0].nodes from this control.
        /// </summary>
        public void CheckAll()
        {
            for (int i = 0; i < this.Nodes[0].Nodes.Count; i++)
            {
                this.Nodes[0].Nodes[i].Checked = true;
            }
        }

        /// <summary>
        /// Returns true if all frames are checked.
        /// </summary>
        public bool IsAllChecked()
        {
            for (int i = 0; i < this.Nodes[0].Nodes.Count; i++)
            {
                if (!this.Nodes[0].Nodes[i].Checked)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the a given amount of frames are checked.
        /// </summary>
        /// <param name="amount">The minimum amount of frames checked.</param>
        /// <returns>True if at least the given amount of frames are checked.</returns>
        public bool IsSomeChecked(int amount)
        {
            int count = 0;
            for (int i = 0; i < this.Nodes[0].Nodes.Count; i++)
            {
                if (!this.Nodes[0].Nodes[i].Checked)
                    count++;
                if (count >= amount)
                    return true;
            }

            return true;
        }

        /// <summary>
        /// Check if there is at least one frame, 
        /// and return list of indexes for selected frames as parameter.
        /// </summary>
        /// <param name="listIndexSelectedFrames">
        /// List of indexes that reference selected frames </param>
        /// <param name="actualFrame">The actual frame</param>
        /// <returns>bool to indicate if there is frame(s) or not</returns>
        public bool IsFrameSelected(out IList listIndexSelectedFrames, int actualFrame)
        {
            listIndexSelectedFrames = new ArrayList();

            #region Get indexes of selected frames

            foreach (TreeNode node in this.Nodes[0].Nodes)
            {
                if (node.Checked)
                    listIndexSelectedFrames.Add(node.Index);
            }

            #endregion

            // Check if there is at least one frame.            
            if (listIndexSelectedFrames.Count == 0)
            {
                //If there is no frame selected, return only the frame being displayed.
                listIndexSelectedFrames.Add(actualFrame);
                return true;
            }

            return true; //TODO: Bool return not necessary.
        }

        /// <summary>
        /// Looks for all selected frames.
        /// </summary>
        /// <returns>Returns the index of all selected frames.</returns>
        public IList SelectedFrames()
        {
            var listIndexSelectedFrames = new ArrayList();

            #region Get indexes of selected frames

            foreach (TreeNode node in this.Nodes[0].Nodes)
            {
                if (node.Checked)
                    listIndexSelectedFrames.Add(node.Index);
            }

            #endregion

            return listIndexSelectedFrames;
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