/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Diagnostics;
using System.Threading;
using _Debug = System.Diagnostics.Debug;
// ReSharper disable RedundantDefaultMemberInitializer

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// Delegate DetachableEventAbortHandler is the signature implemented by a method intended to respond to the aborting of a detachable event.
    /// </summary>
    /// <param name="exec">The executive whose detachable event is being aborted.</param>
    /// <param name="idec">The detachable event controller.</param>
    /// <param name="args">The arguments that were to have been provided to the ExecEventReceiver.</param>
    public delegate void DetachableEventAbortHandler(IExecutive exec, IDetachableEventController idec, params object[] args);

    internal class DetachableEvent : IDetachableEventController
    {

        #region >>> Private Fields <<<
        private static readonly bool _diagnostics = Diagnostics.DiagnosticAids.Diagnostics("DetachableEventController");
        private StackTrace _suspendedStackTrace = null;
        private readonly Executive _exec;
        private readonly ExecEvent _currEvent;
        private bool _abortRequested = false;
        private DateTime _timeOfLastWait;

        private ManualResetEventSlim _beginResetEvent = null;
        private ManualResetEventSlim _resumeResetEvent = null;
        private ManualResetEventSlim _suspendResetEvent = null;

        #endregion

        private DetachableEventAbortHandler _abortHandler;
        private object[] _args = null;
        public void SetAbortHandler(DetachableEventAbortHandler handler, params object[] args)
        {
            _abortHandler = handler;
            _args = args;
        }

        public void ClearAbortHandler()
        {
            _abortHandler = null;
            _args = null;
        }

        public void FireAbortHandler()
        {
            if (_abortHandler != null)
            {
                _abortHandler(_exec, this, _args);
                ClearAbortHandler();
            }
        }

        public DetachableEvent(Executive exec, ExecEvent currentEvent)
        {
            _exec = exec;
            _currEvent = currentEvent;
            _exec.RunningDetachables.Add(this);
        }

        public void Begin()
        {
            Debug.Assert(!_abortRequested, "(Re)beginning an aborted DetachableEvent");
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == _exec.ThreadId,
                    $"DetachableEvent.Begin running on thread {Thread.CurrentThread.ManagedThreadId}, which should be same as Executive {_exec.ThreadId}");

            // This method runs in the executive's event service thread.

            _beginResetEvent = new ManualResetEventSlim(false);

            var currentTask = System.Threading.Tasks.Task
                    .Run(() =>
                    {
                        _currEvent.ExecEventReceiver.Invoke(_exec, _currEvent.UserData);
                    })
                    .ContinueWith(End);

            _timeOfLastWait = _exec.Now;

            _beginResetEvent.Wait(); // Keeps exec from running off and launching another event.

            if (_abortRequested)
                Abort();
        }

        /// <summary>
        /// Suspends this detachable event until it is explicitly resumed.
        /// </summary>
        public void Suspend()
        {
            Debug.Assert(!_abortRequested, "Suspending an aborted DetachableEvent");
            Debug.Assert(_exec.CurrentEventType.Equals(ExecEventType.Detachable), _errMsg1 + _exec.CurrentEventType, _errMsg1Explanation);
            Debug.Assert(Thread.CurrentThread.ManagedThreadId != _exec.ThreadId,
                    $"DetachableEvent.Suspend running on thread {Thread.CurrentThread.ManagedThreadId}, which should NOT be same as Executive {_exec.ThreadId}");

            // This method runs on the DE's execution thread. The only way to get the DE is to use Executive's
            // CurrentEventController property, and this DE should be used immediately, not held for later.
            Debug.Assert(Equals(_exec.CurrentEventController), "Suspend called from someone else's thread!");
            if (_diagnostics)
                _suspendedStackTrace = new StackTrace();

            _exec.SetCurrentEventController(null);

            _suspendResetEvent = new ManualResetEventSlim(false);
            _beginResetEvent.Set();

            if (_resumeResetEvent != null)
                _resumeResetEvent.Set();

            _timeOfLastWait = _exec.Now;
            _suspendResetEvent.Wait();

            _exec.SetCurrentEventType(ExecEventType.Detachable); // Whatever it was, it is now a detachable.
            _suspendResetEvent.Dispose();
            _suspendResetEvent = null;

            if (_abortRequested)
                DoAbort();
        }

        public void SuspendUntil(DateTime when)
        {
            // This method runs on the DE's execution thread.
            Debug.Assert(_exec.CurrentEventType.Equals(ExecEventType.Detachable), _errMsg1 + _exec.CurrentEventType, _errMsg1Explanation);
            _exec.RequestEvent(new ExecEventReceiver(resume), when, 0, null);
            Suspend();
        }

        public void SuspendFor(TimeSpan howLong)
        {
            SuspendUntil(_exec.Now + howLong);
        }


        public void Resume(double overridePriorityLevel)
        {
            Debug.Assert(!_abortRequested, "Resumed an aborted DetachableEvent");
            // This method is called by someone else's thread. The DE should be suspended at this point.
            Debug.Assert(!Equals(_exec.CurrentEventController), "Resume called from DE's own thread!");
            _exec.AcquireEventLock();
            _exec.RequestEvent(new ExecEventReceiver(resume), _exec.Now, overridePriorityLevel, null);
            _exec.ReleaseEventLock();
        }

        public void Resume()
        {
            Resume(_exec.CurrentPriorityLevel);
        }

        public bool HasAbortHandler
        {
            get
            {
                return _abortHandler != null;
            }
        }

        internal void Abort()
        {
            Debug.Assert(!_abortRequested, "(Re)aborting an aborted DetachableEvent");
            FireAbortHandler();
            _abortRequested = true;

            if (_suspendResetEvent != null && !_suspendResetEvent.IsSet)
                _suspendResetEvent.Set();
            if (_resumeResetEvent != null && !_resumeResetEvent.IsSet)
                _resumeResetEvent.Set();
            if (_beginResetEvent != null && !_beginResetEvent.IsSet)
                _beginResetEvent.Set();
        }

        private void DoAbort()
        {
            Debug.Assert(!Equals(_exec.CurrentEventController), "DoAbort called from someone else's thread!");

            if (Diagnostics.DiagnosticAids.Diagnostics("Executive.BreakOnThreadAbort"))
            {
                Debugger.Break();
            }
        }

        private void resume(IExecutive exec, object userData)
        {
            // This method is always called on the Executive's event service thread.
            //_Debug.WriteLine(this.m_currEvent.m_eer.Target+"."+this.m_currEvent.m_eer.Method + "de is resuming." + GetHashCode());

            Debug.Assert(Thread.CurrentThread.ManagedThreadId == _exec.ThreadId,
                    $"DetachableEvent.resume running on thread {Thread.CurrentThread.ManagedThreadId}, which should be same as Executive {_exec.ThreadId}");

            if (_diagnostics)
                _suspendedStackTrace = null;

            //_Debug.WriteLine(DateTime.Now.Ticks + "Task Resume is Pulsing " + m_lock);Trace.Out.Flush();
            _exec.SetCurrentEventController(this);
            _resumeResetEvent = new ManualResetEventSlim(false);
            _suspendResetEvent.Set();

            _resumeResetEvent.Wait();
        }

        private void End(IAsyncResult iar)
        {
            try
            {
                _exec.RunningDetachables.Remove(this);
                //_Debug.WriteLine(this.m_currEvent.m_eer.Target+"."+this.m_currEvent.m_eer.Method + "de is finishing." + GetHashCode());
                _currEvent.OnServiceCompleted();
                if (_suspendResetEvent != null)
                    _suspendResetEvent.Set();
                if (_resumeResetEvent != null)
                    _resumeResetEvent.Set();
                if (_beginResetEvent != null)
                    _beginResetEvent.Set();
                //_Debug.WriteLine(this.m_currEvent.m_eer.Target+"."+this.m_currEvent.m_eer.Method + "de is really finishing." + GetHashCode());
            }
            catch (Exception e)
            {
                // TODO: Report this to an Executive Errors & Warnings collection.
                _Debug.WriteLine("Caught model error : " + e);
                _exec.Stop();
            }
        }


        public StackTrace SuspendedStackTrace
        {
            get
            {
                return _suspendedStackTrace;
            }
        }

        public IExecEvent RootEvent
        {
            get
            {
                return _currEvent;
            }
        }

        public bool IsWaiting()
        {
            return (_beginResetEvent != null && !_beginResetEvent.IsSet)
                   || (_resumeResetEvent != null && !_resumeResetEvent.IsSet)
                   || (_suspendResetEvent != null && !_suspendResetEvent.IsSet);
        }

        public DateTime TimeOfLastWait
        {
            get
            {
                return _timeOfLastWait;
            }
        }

        private readonly string _errMsg1 = "Detachable event control being inappropriately exercised from an event of type ";
        private readonly string _errMsg1Explanation = "The caller is trying to suspend an event thread from a thread that was not launched as result of a detachable event.";
    }
}
