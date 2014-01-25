using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScreenToGif.Util
{
    using System;
    using System.Threading;

    /// <summary>
    /// Represents a variable whose value
    /// is determined by an asynchronous worker function.
    /// </summary>
    /// <typeparam name="TResult">The type
    ///     of value represented by the variable.</typeparam>
    public class AsyncVar<TResult>
    {
        private Func<TResult> workerFunction;
        private Thread workerThread;

        private TResult value;
        private Exception error;

        /// <summary>
        /// Initializes a new instance of the <see 
        ///     cref="AsyncVar&lt;TResult&gt;"/> class.
        /// </summary>
        /// <param name="worker">The worker function
        ///       that returns the value to be held.</param>
        public AsyncVar(Func<TResult> worker)
        {
            this.workerFunction = worker;
            this.workerThread = new Thread(this.ThreadStart);

            this.workerThread.Start();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="AsyncVar&lt;TResult&gt;"/> class.
        /// </summary>
        ~AsyncVar()
        {
            if (this.workerThread.IsAlive)
            {
                this.workerThread.Abort();
            }
        }

        /// <summary>
        /// The ThreadStart method used for the worker thread.
        /// </summary>
        private void ThreadStart()
        {
            TResult returnValue = default(TResult);

            try
            {
                returnValue = this.workerFunction();
            }
            catch (Exception ex)
            {
                this.error = ex;
            }

            if (this.error == null)
            {
                this.value = returnValue;
            }
        }

        /// <summary>
        /// Gets the value returned by the worker function.
        /// If the worker thread is still running, blocks until the thread is complete.
        /// </summary>
        /// <value>The value returned by the worker function.</value>
        public TResult Value
        {
            get
            {
                if (this.workerThread.IsAlive)
                {
                    this.workerThread.Join();
                }

                if (this.error != null)
                {
                    throw new InvalidOperationException("Thread encountered " +
                              "an exception during execution.", this.error);
                }
                else
                {
                    return this.value;
                }
            }
        }

        /// <summary>
        /// Gets an exception thrown by the worker function. If the worker
        /// thread is still running, blocks until the thread is complete.
        /// If no unhandled exceptions occurred in the worker thread,
        /// returns <see langword="null"/>.
        /// </summary>
        /// <value>An exception thrown by the worker function.</value>
        public Exception Error
        {
            get
            {
                if (this.workerThread.IsAlive)
                {
                    this.workerThread.Join();
                }

                return this.error;
            }
        }
    }
}