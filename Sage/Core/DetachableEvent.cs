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
        private static int _lockNum = 0;
        private readonly string _lock = "Lock #" + _lockNum++;
        private readonly Executive _exec;
        private readonly ExecEvent _currEvent;
        private bool _abortRequested = false;
        private long _isWaitingCount;
        private DateTime _timeOfLastWait;
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

            // This method runs in the executive's event service thread.
            lock (_lock)
            {
                object userData = _currEvent.UserData;
                IAsyncResult iar = _currEvent.ExecEventReceiver.BeginInvoke(_exec, userData, new AsyncCallback(End), null);
                Interlocked.Increment(ref _isWaitingCount);
                _timeOfLastWait = _exec.Now;
                Monitor.Wait(_lock); // Keeps exec from running off and launching another event.
                Interlocked.Decrement(ref _isWaitingCount);
                if (_abortRequested)
                    Abort();
            }
        }

        /// <summary>
        /// Suspends this detachable event until it is explicitly resumed.
        /// </summary>
        public void Suspend()
        {
            Debug.Assert(!_abortRequested, "Suspending an aborted DetachableEvent");
            Debug.Assert(_exec.CurrentEventType.Equals(ExecEventType.Detachable), _errMsg1 + _exec.CurrentEventType, _errMsg1Explanation);

            // This method runs on the DE's execution thread. The only way to get the DE is to use Executive's
            // CurrentEventController property, and this DE should be used immediately, not held for later.
            Debug.Assert(Equals(_exec.CurrentEventController), "Suspend called from someone else's thread!");
            lock (_lock)
            {
                //_Debug.WriteLine(this.m_currEvent.m_eer.Target+"."+this.m_currEvent.m_eer.Method + "de is suspending." + GetHashCode());
                if (_diagnostics)
                    _suspendedStackTrace = new StackTrace();

                _exec.SetCurrentEventController(null);
                Monitor.Pulse(_lock);
                Interlocked.Increment(ref _isWaitingCount);
                _timeOfLastWait = _exec.Now;
                Monitor.Wait(_lock);
                _exec.SetCurrentEventType(ExecEventType.Detachable); // Whatever it was, it is now a detachable.
                Interlocked.Decrement(ref _isWaitingCount);
                if (_abortRequested)
                    DoAbort();
            }
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
            lock (_lock)
            {
                _abortRequested = true;
                Monitor.Pulse(_lock);
            }
        }

        private void DoAbort()
        {
            Debug.Assert(!Equals(_exec.CurrentEventController), "DoAbort called from someone else's thread!");

            lock (_lock)
            {
                Monitor.Pulse(_lock);
            }

            if (Diagnostics.DiagnosticAids.Diagnostics("Executive.BreakOnThreadAbort"))
            {
                Debugger.Break();
            }

            Thread.CurrentThread.Abort();

        }

        private void resume(IExecutive exec, object userData)
        {
            // This method is always called on the Executive's event service thread.
            lock (_lock)
            {
                //_Debug.WriteLine(this.m_currEvent.m_eer.Target+"."+this.m_currEvent.m_eer.Method + "de is resuming." + GetHashCode());

                if (_diagnostics)
                    _suspendedStackTrace = null;

                //_Debug.WriteLine(DateTime.Now.Ticks + "Task Resume is Pulsing " + m_lock);Trace.Out.Flush();
                _exec.SetCurrentEventController(this);
                Monitor.Pulse(_lock);
                Interlocked.Increment(ref _isWaitingCount);
                if (_isWaitingCount > 1)
                    Monitor.Wait(_lock);
                Interlocked.Decrement(ref _isWaitingCount);

            }
        }

        private void End(IAsyncResult iar)
        {

            try
            {
                _exec.RunningDetachables.Remove(this);
                //_Debug.WriteLine(this.m_currEvent.m_eer.Target+"."+this.m_currEvent.m_eer.Method + "de is finishing." + GetHashCode());
                lock (_lock)
                {
                    _currEvent.OnServiceCompleted();
                    Monitor.Pulse(_lock);
                }
                //_Debug.WriteLine(this.m_currEvent.m_eer.Target+"."+this.m_currEvent.m_eer.Method + "de is really finishing." + GetHashCode());
                _currEvent.ExecEventReceiver.EndInvoke(iar);
            }
            catch (ThreadAbortException)
            {
                //_Debug.WriteLine(tae.Message); // Must explicitly catch the ThreadAbortException or it bubbles up.
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
            return _isWaitingCount > 0;
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
