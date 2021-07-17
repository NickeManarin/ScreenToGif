using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Windows;

using ScreenToGif.Model;

namespace ScreenToGif.Capture
{
    public abstract class BaseCapture : ICapture
    {
        private Task _task;

        #region Properties

        public bool WasStarted { get; set; }
        public int FrameCount { get; set; }
        public int MinimumDelay { get; set; }

        public int Left { get; set; }
        public int Top { get; set; }

        /// <summary>
        /// The current width of the capture. It can fluctuate, based on the DPI of the current screen.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// The current height of the capture. It can fluctuate, based on the DPI of the current screen.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// The starting width of the capture. 
        /// </summary>
        public int StartWidth { get; set; }

        /// <summary>
        /// The starting height of the capture.
        /// </summary>
        public int StartHeight { get; set; }

        /// <summary>
        /// The starting scale of the recording.
        /// </summary>
        public double StartScale { get; set; }

        /// <summary>
        /// The current scale of the recording.
        /// </summary>
        public double Scale { get; set; }

        /// <summary>
        /// The difference in scale from the start frame to the current frame.
        /// </summary>
        public double ScaleDiff => StartScale / Scale;

        /// <summary>
        /// The name of the monitor device where the recording is supposed to happen.
        /// </summary>
        public string DeviceName { get; set; }

        public ProjectInfo Project { get; set; }
        public Action<Exception> OnError { get; set; }

        protected BlockingCollection<FrameInfo> BlockingCollection { get; private set; } = new BlockingCollection<FrameInfo>();

        #endregion

        /// <summary>
        /// This destructor is only for someone who forgets to release
        /// managed things when generating lots of them. When the destructor
        /// function is called, this object will never be useful, so we don't
        /// need to care about when/how it is released or block the finalizer
        /// thread from doing anything else behind.
        /// </summary>
        ~BaseCapture()
        {
            Dispose(false);
        }

        public virtual void Start(int delay, int left, int top, int width, int height, double scale, ProjectInfo project)
        {
            if (WasStarted)
                throw new Exception("Screen capture was already started. Stop before trying again.");

            FrameCount = 0;
            MinimumDelay = delay;
            Left = left;
            Top = top;
            StartWidth = Width = width;
            StartHeight = Height = height;
            StartScale = scale;
            Scale = scale;

            Project = project;
            Project.Width = width;
            Project.Height = height;
            Project.Dpi = 96 * scale;

            //Spin up a Task to consume the BlockingCollection.
            _task = Task.Factory.StartNew(() =>
            {
                try
                {
                    FrameInfo fi = null;

                    // Try to fetch the next FrameInfo until
                    // it's empty
                    while (BlockingCollection.TryTake(out fi))
                    {
                        Save(fi);
                    }
                }
                catch (Exception e)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => OnError?.Invoke(e)));
                }
            });

            WasStarted = true;
        }

        public virtual void ResetConfiguration()
        { }

        public virtual void Save(FrameInfo info)
        { }

        public virtual int Capture(FrameInfo frame)
        {
            return 0;
        }

        public virtual Task<int> CaptureAsync(FrameInfo frame)
        {
            return null;
        }

        public virtual int CaptureWithCursor(FrameInfo frame)
        {
            return 0;
        }

        public virtual Task<int> CaptureWithCursorAsync(FrameInfo frame)
        {
            return null;
        }

        public virtual int ManualCapture(FrameInfo frame, bool showCursor = false)
        {
            return showCursor ? CaptureWithCursor(frame) : Capture(frame);
        }

        public virtual Task<int> ManualCaptureAsync(FrameInfo frame, bool showCursor = false)
        {
            return showCursor ? CaptureWithCursorAsync(frame) : CaptureAsync(frame);
        }

        /// <summary>
        /// Safely release all the managed things.
        /// </summary>
        public virtual async Task Stop()
        {
            if (!WasStarted)
                return;

            using (_task)
            {
                // Stop adding things into the BlockingCollection
                // and mark it finished
                BlockingCollection.CompleteAdding();
                await _task;
                using (BlockingCollection) { }
            }

            WasStarted = false;
        }

        /// <summary>
        /// The function is used to make sure whether you're calling to
        /// release the managed sources or not.
        /// If yes, after releasing managed things, DON'T let GC call
        /// destructor again, directly GC them.
        /// If not, this means the destructor function will call it at
        /// some time, we should let the destructor function executed first
        /// to help us release managed things first and then release the whole
        /// object of this itself.
        /// </summary>
        /// <param name="isManualReleased">
        /// A flag to notify whether you're calling it manually or
        /// called by the destructor.
        /// </param>
        protected virtual async Task Dispose(bool isManualReleased)
        {
            if (WasStarted)
                await Stop();

            if (isManualReleased)
                GC.SuppressFinalize(this);
        }
        public virtual async Task Dispose()
        {
            await Dispose(true);
        }
    }
}
