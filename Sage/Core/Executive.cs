/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using _Debug = System.Diagnostics.Debug;
// ReSharper disable RedundantDefaultMemberInitializer

namespace Highpoint.Sage.SimCore
{

    /// <summary>
    /// This is a full-featured executive, with rescindable and detachable events, support for pause and resume and
    /// event priority sorting within the same timeslice, and detection of causality violations. Use FastExecutive
    /// if these features are unimportant and you want blistering speed.
    /// </summary>
    internal sealed class Executive : MarshalByRefObject, IExecutive
    {
        private DateTime? _lastEventServiceTime = null;
        private Exception _terminationException = null;
        private readonly ExecEventType _defaultEventType = ExecEventType.Synchronous;
        private ExecState _state = ExecState.Stopped;
        private DateTime _now = DateTime.MinValue;
        private SortedList _events = new SortedList(new ExecEventComparer());
        private Stack _removals = new Stack();
        private double _currentPriorityLevel = double.MinValue;
        private long _nextReqHashCode = 0;
        private bool _stopRequested = false;
        private bool _abortRequested = false;
        private Guid _guid;
        private int _runNumber = -1;
        private uint _eventCount = 0;
        private int _numDaemonEventsInQueue = 0;
        private int _numEventsInQueue = 0;
        private ExecEventType _currentEventType;

        private DetachableEvent _currentDetachableEvent = null;

        private static readonly bool _diagnostics = Diagnostics.DiagnosticAids.Diagnostics("Executive");
        private static bool _ignoreCausalityViolations = true;

        private static bool _clrConfigDone = false;

        private object _eventLock = new Object();
        public int EventLockCount = 0;

        internal Executive(Guid execGuid)
        {
            _guid = execGuid;
            _currentEventType = ExecEventType.None;


            #region >>> Set up from-config-file parameters <<<
            int desiredMinWorkerThreads = 100;
            int desiredMaxWorkerThreads = 900;
            int desiredMinIocThreads = 50;
            int desiredMaxIocThreads = 100;
            NameValueCollection nvc = null;
            try
            {
                nvc = (NameValueCollection)System.Configuration.ConfigurationManager.GetSection("Sage");
            }
            catch (Exception e )
            {
                Console.WriteLine(e);
            }
            if (nvc == null)
            {
                _Debug.WriteLine(cannot_Find_Sage_Section);
                // Leave at default values.
            }
            else
            {
                string workerThreads = nvc["WorkerThreads"];
                if (workerThreads == null || ( !int.TryParse(workerThreads, out desiredMaxWorkerThreads) ))
                {
                    _Debug.WriteLine(cannot_Find_Workerthread_Directive);
                } // else wt has been set to the desired value.

                string ignoreCausalityViolations = nvc["IgnoreCausalityViolations"];
                if (ignoreCausalityViolations == null || !bool.TryParse(ignoreCausalityViolations, out _ignoreCausalityViolations))
                {
                    _Debug.WriteLine(cannot_Find_Causality_Directive);
                } // else micv has been set to the desired value.
            }

            if (!_clrConfigDone)
            {
                if (desiredMinWorkerThreads > desiredMaxWorkerThreads)
                    Swap(ref desiredMinWorkerThreads, ref desiredMaxWorkerThreads);
                if ( desiredMinIocThreads > desiredMaxIocThreads )
                    Swap(ref desiredMinIocThreads, ref desiredMaxIocThreads);

                // We want to know the number of worker and IO Threads the executive wants available.
                // It must be 
                int anwt, axwt, aniot, axiot;
                ThreadPool.GetMinThreads(out anwt, out aniot);
                ThreadPool.GetMaxThreads(out axwt, out axiot);

                desiredMinWorkerThreads = Math.Max(anwt,desiredMinWorkerThreads);
                desiredMaxWorkerThreads = Math.Max(axwt, desiredMaxIocThreads);
                desiredMinIocThreads = Math.Max(aniot, desiredMinIocThreads);
                desiredMaxIocThreads = Math.Max(axiot, desiredMaxIocThreads);
                try {

                    ThreadPool.SetMinThreads(desiredMinWorkerThreads, desiredMinIocThreads);
                    ThreadPool.SetMinThreads(desiredMinIocThreads, desiredMaxIocThreads);

                    _clrConfigDone = true;
                }
                catch (System.Runtime.InteropServices.COMException ce)
                {
                    string msg = string.Format("Failed attempt to set CLR Threadpool Working Threads [{0},{1}] and IO Completion Threads [{2},{3}].\r\n{4}",
                        desiredMinWorkerThreads, desiredMaxWorkerThreads, desiredMinIocThreads, desiredMaxIocThreads, ce);
                    _Debug.WriteLine(msg);
                }
            }
        }
            #endregion

            #region Error Messages

        private static readonly string cannot_Find_Sage_Section =
    @"Missing Sage section of config file. Defaulting to maintaining between 100 and 900 execution threads.
Add the following two sections to your app.config to fix this issue:
<configSections>
    <section name=""Sage"" type=""System.Configuration.NameValueSectionHandler, System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" />
</configSections>
   ...and
<Sage>
    <add key=""WorkerThreads"" value=""100""/>
    <add key=""IgnoreCausalityViolations"" value=""true""/>
</Sage>
NOTE - everything will still work fine, we're just defaulting to maintaining between 100 and 900 worker threads for now, and ignoring causality exceptions.";

        private static readonly string cannot_Find_Workerthread_Directive =
    @"Unable to find (or parse) WorkerThread directive in Sage section of App Config file. Add the following to the Sage section:
<Sage>\r\n<add key=""WorkerThreads"" value=""100""/>\r\n</Sage>
NOTE - everything will still work fine, we're just defaulting to maintaining between 100 and 900 worker threads for now.";

        private static readonly string cannot_Find_Causality_Directive =
    @"Unable to find Causality Exception directive in Sage section of App Config file. Add the following to the Sage section:
    <add key=""IgnoreCausalityViolations"" value=""true""/>
NOTE - the engine will still run, we'll just ignore it if an event is requested earlier than tNow during a simulation.";

        #endregion

        private static void Swap(ref int a, ref int b)
        {
            int tmp = a;
            a = b;
            b = tmp;
        }

        internal ArrayList RunningDetachables = new ArrayList();

        /// <summary>
        /// Returns a read-only list of the detachable events that are currently running.
        /// </summary>
        public ArrayList LiveDetachableEvents
        {
            get
            {
                return ArrayList.ReadOnly(RunningDetachables);
            }
        }

        /// <summary>
        /// Returns a read-only list of the ExecEvents currently in queue for execution.
        /// Cast the elements in the list to IExecEvent to access the items' field values.
        /// </summary>
        public IList EventList
        {
            get
            {
                return ArrayList.ReadOnly(_events.GetKeyList());
            }
        }

        public Guid Guid => _guid;

        /// <summary>
        /// Returns the simulation time that the executive is currently processing. Any event submitted with a requested
        /// service time prior to this time, will initiate a causality violation. If the App.Config file is not set to
        /// ignore these (see below), this will result in a CausalityException being thrown.
        /// </summary>
        public DateTime Now
        {
            get
            {
                return _now;
            }
        }

        /// <summary>
        /// If this executive has been run, this holds the DateTime of the last event serviced. May be from a previous run.
        /// </summary>
        public DateTime? LastEventServed
        {
            get
            {
                return _lastEventServiceTime;
            }
        }

        /// <summary>
        /// Returns the simulation time that the executive is currently processing. For a given time, any priority event
        /// may be submitted. For example, if the executive is processing an event with priority 1.5, and another event
        /// is requested with priority 2.0, (higher priorities are serviced first), that event will be the next to be
        /// serviced.
        /// </summary>
        public double CurrentPriorityLevel
        {
            get
            {
                return _currentPriorityLevel;
            }
        }

        /// <summary>
        /// The state of the executive - Stopped, Running, Paused, Finished.
        /// </summary>
        public ExecState State
        {
            get
            {
                return _state;
            }
        }

        /// <summary>
        /// Requests that the executive queue up a daemon event to be serviced at a specific time and
        /// priority. If only daemon events are enqueued, the executive will not be kept alive.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="when">The date &amp; time at which the callback is to be made.</param>
        /// <param name="priority">The priority of the callback. Higher numbers mean higher priorities.</param>
        /// <param name="userData">Object data to be provided in the callback.</param>
        /// <returns>A code that can subsequently be used to identify the request, e.g. for removal.</returns>
        public long RequestDaemonEvent(ExecEventReceiver eer, DateTime when, double priority, object userData)
        {
            return RequestEvent(eer, when, priority, userData, _defaultEventType, true);
        }

        /// <summary>
        /// Requests that the executive queue up an event to be serviced at a specific time. Priority is assumed
        /// to be zero, and the userData object is assumed to be null.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="when">The date &amp; time at which the callback is to be made.</param>
        /// <returns>
        /// A code that can subsequently be used to identify the request, e.g. for removal.
        /// </returns>
        public long RequestEvent(ExecEventReceiver eer, DateTime when)
        {
            return RequestEvent(eer, when, 0.0, null, _defaultEventType, false);
        }

        /// <summary>
        /// Requests that the executive queue up an event to be serviced at a specific time. Priority is assumed
        /// to be zero.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="when">The date &amp; time at which the callback is to be made.</param>
        /// <param name="userData">Object data to be provided in the callback.</param>
        /// <returns>
        /// A code that can subsequently be used to identify the request, e.g. for removal.
        /// </returns>
        public long RequestEvent(ExecEventReceiver eer, DateTime when, object userData)
        {
            return RequestEvent(eer, when, 0.0, userData, _defaultEventType, false);
        }

        /// <summary>
        /// Requests scheduling of a synchronous event. Event service takes the form of a call, by the executive, into a specified method
        /// on a specified object, passing it the executive and a specified user data object. The method, the object and the
        /// user data are specified at the time of scheduling the event (i.e. when making this call). <p></p><p></p>
        /// <B>Note:</B> The event will be scheduled as a synchronous event. If you want another type of event, use the other
        /// form of this API.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver (a delegate) that will accept the call from the executive.</param>
        /// <param name="when">The DateTime at which the event is to be served.</param>
        /// <param name="priority">The priority at which the event is to be serviced. Higher values are serviced first,
        /// if both are scheduled for the same precise time.</param>
        /// <param name="userData">An object of any type that the code scheduling the event (i.e. making this call) wants to
        /// have passed to the code executing the event (i.e. the body of the ExecEventReceiver.)</param>
        /// <returns>A long, which is a number that serves as a key. This key is used, for example, to unrequest the event.</returns>
        public long RequestEvent(ExecEventReceiver eer, DateTime when, double priority, object userData)
        {
            return RequestEvent(eer, when, priority, userData, _defaultEventType, false);
        }

        /// <summary>
        /// Requests scheduling of an event, allowing the caller to specify the type of the event. Event service takes the
        /// form of a call, by the executive, into a specified method on a specified object, passing it the executive and a
        /// specified user data object. The method, the object and the user data are specified at the time of scheduling the
        /// event (i.e. when making this call). 
        /// </summary>
        /// <param name="eer">The ExecEventReceiver (a delegate) that will accept the call from the executive.</param>
        /// <param name="when">The DateTime at which the event is to be served.</param>
        /// <param name="priority">The priority at which the event is to be serviced. Higher values are serviced first,
        /// if both are scheduled for the same precise time.</param>
        /// <param name="userData">An object of any type that the code scheduling the event (i.e. making this call) wants to
        /// have passed to the code executing the event (i.e. the body of the ExecEventReceiver.)</param>
        /// <param name="execEventType">Specifies the type of event dispatching to be employed for this event.</param>
        /// <returns>A long, which is a number that serves as a key. This key is used, for example, to unrequest the event.</returns>
        public long RequestEvent(ExecEventReceiver eer, DateTime when, double priority, object userData, ExecEventType execEventType)
        {
            return RequestEvent(eer, when, priority, userData, execEventType, false);
        }

        /// <summary>
        /// Creates an event that looks like an event already in queue, but for a new time. Optionally deletes the old event.
        /// </summary>
        /// <param name="eventID">The ID number assigned to the event when it was initially requested.</param>
        /// <param name="newTime">The new time.</param>
        /// <param name="deleteOldOne">The old event will be removed.</param>
        /// <returns>A code that can subsequently be used to identify the request, e.g. for removal.</returns>
        public long ResubmitEventAtTime(long eventID, DateTime newTime, bool deleteOldOne)
        {
            long newKey = long.MinValue;
            foreach (IExecEvent @event in EventList)
            {
                if (@event.Key == eventID)
                {
                    newKey = RequestEvent(@event.ExecEventReceiver, newTime, @event.Priority, @event.UserData,
                        @event.EventType, @event.IsDaemon);
                    if (deleteOldOne)
                        UnRequestEvent(eventID);
                    break;
                }

            }
            return newKey;

        }

        /// <summary>
        /// Requests that the executive queue up an event to be serviced at the current executive time and
        /// priority.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="userData">Object data to be provided in the callback.</param>
        /// <param name="execEventType">The way the event is to be served by the executive.</param>
        /// <returns>A code that can subsequently be used to identify the request, e.g. for removal.</returns>
        public long RequestImmediateEvent(ExecEventReceiver eer, object userData, ExecEventType execEventType)
        {
            lock (_events)
            {
                return RequestEvent(eer, _now, _currentPriorityLevel, userData, execEventType, false);
            }
        }

        private long RequestEvent(ExecEventReceiver eer, DateTime when, double priority, object userData, ExecEventType execEventType, bool isDaemon)
        {
            if (!_stopRequested && !_abortRequested)
            {
                Debug.Assert(eer != null, "An event was requested to call into a null callback.");
                lock (_events)
                {
                    if (_state == ExecState.Running)
                    {
                        if (when < _now)
                        {
                            if (!_ignoreCausalityViolations)
                            {
                                throw new CausalityException("Event requested for time " + when + ", but executive is at time " + _now + ". " +
                                    "\r\nAdd \"<add key=\"IgnoreCausalityViolations\" value=\"true\"/>\r\n" +
                                    "to the Sage section of your app.config file to prevent these exceptions. (Submitted event requests will be ignored.)");
                            }
                            else
                            {
                                return long.MinValue;
                            }
                        }
                    }
                    if (_state != ExecState.Finished)
                    {
                        _nextReqHashCode++;
                        if (isDaemon)
                            _numDaemonEventsInQueue++;
                        _numEventsInQueue++;
                        _events.Add(ExecEvent.Get(eer, when, priority, userData, execEventType, _nextReqHashCode, isDaemon), _nextReqHashCode);
                        if (_diagnostics)
                        {
                            _Debug.WriteLine("Event requested for time " + when + ", to call back at " + eer.Target + "(" + eer.Target.GetHashCode() + ")." + eer.Method.Name);
                        }
                        return _nextReqHashCode;
                    }
                    else
                    {
                        throw new ApplicationException("Event service cannot be requested from an Executive that is in the \"Finished\" state.");
                    }
                }
            }
            else
            {
                return -1;
            }
        }

        public void UnRequestEvent(long requestedEventHashCode)
        {
            if (requestedEventHashCode == long.MinValue)
                return; // illegitimate key.
            _removals.Push(new ExecEventRemover(requestedEventHashCode));
        }

        public void UnRequestEvents(object execEventReceiverTarget)
        {
            _removals.Push(new ExecEventRemover(execEventReceiverTarget));
        }

        public void UnRequestEvents(IExecEventSelector eventSelector)
        {
            _removals.Push(new ExecEventRemover(eventSelector));
        }

        public void UnRequestEvents(Delegate execEventReceiverMethod)
        {
            _removals.Push(new ExecEventRemover((Delegate)execEventReceiverMethod));
        }

        #region Join Handling
        private class JoinSet
        {

            private readonly Executive _exec;
            private readonly List<long> _eventCodes;
            private IDetachableEventController _idec;

            public JoinSet(Executive exec, long[] eventCodes)
            {
                _exec = exec;
                _eventCodes = new List<long>(eventCodes);
                foreach (long eventCode in eventCodes)
                {
                    ((ExecEvent)_exec._events.GetKey(_exec._events.IndexOfValue(eventCode))).ServiceCompleted += new EventMonitor(ee_ServiceCompleted);
                }
            }

            private void ee_ServiceCompleted(long key, ExecEventReceiver eer, double priority, DateTime when, object userData, ExecEventType eventType)
            {
                _eventCodes.Remove(key);
                if (_eventCodes.Count == 0)
                {
                    _idec.Resume();
                }
            }

            public void Join()
            {
                Debug.Assert(_exec.CurrentEventType == ExecEventType.Detachable, "Cannot call Join on a non-Detachable event.");
                _idec = _exec.CurrentEventController;
                List<string> eventCodes = new List<string>();
                _eventCodes.ForEach(delegate (long ec)
                {
                    eventCodes.Add(ec.ToString());
                });
                Console.WriteLine("I am waiting to join on " + Utility.StringOperations.ToCommasAndAndedList(eventCodes) + ".");
                _idec.Suspend();
                Console.WriteLine("I am done waiting to join on " + Utility.StringOperations.ToCommasAndAndedList(eventCodes) + ".");
            }
        }

        /// <summary>
        /// This method blocks until the events that correlate to the provided event codes (which are returned from the RequestEvent
        /// APIs) have been completely serviced. The event on whose thread this method is called must be a detachable event, all of
        /// the provided events must have been requested already, and none can have already been serviced.
        /// </summary>
        /// <param name="eventCodes">The event codes.</param>
        public void Join(params long[] eventCodes)
        {
            JoinSet joinSet = new JoinSet(this, eventCodes);
            joinSet.Join();
        }

        #endregion

        public int RunNumber
        {
            get
            {
                return _runNumber;
            }
        }

        /// <summary>
        /// The number of events that have been serviced on this run.
        /// </summary>
        public uint EventCount
        {
            get
            {
                return _eventCount;
            }
        }

        private DateTime _startTime = DateTime.MinValue;
        public void SetStartTime(DateTime startTime)
        {
            _startTime = startTime;
        }

        public void Start()
        {

            _pauseMgr = new Thread(new ThreadStart(doPause));
            _pauseMgr.Name = "Pause Management";
            _pauseMgr.Start();

            lock (this)
            {

                _now = _startTime;

                #region Initial bookkeeping setup
                _terminationException = null;
                _runNumber++;
                _eventCount = 0;
                #endregion Initial bookkeeping setup

                #region Diagnostics
                if (_diagnostics)
                {
                    _Debug.WriteLine("Executive starting with the following events queued up...");
                }
                #endregion Diagnostics

                #region Kickoff Events
                _executiveStarted?.Invoke(this);
                if (_executiveStartedSingleShot != null)
                {
                    _executiveStartedSingleShot(this);
                    _executiveStartedSingleShot = (ExecutiveEvent)Delegate.RemoveAll(_executiveStartedSingleShot, _executiveStartedSingleShot);
                }
                #endregion Kickoff Events

                _state = ExecState.Running;

                uint initialEventCount = _eventCount;

                while (!_stopRequested && !_abortRequested && (_numEventsInQueue > _numDaemonEventsInQueue))
                {

                    Monitor.Enter(_runLock);
                    Monitor.Exit(_runLock);

                    #region Diagnostics
                    if (_diagnostics)
                        DumpEventQueue();
                    #endregion Diagnostics

                    _eventCount++;

                    #region Process queued-up event removal requests
                    while (_removals.Count > 0)
                    {
                        ExecEventRemover er = (ExecEventRemover)_removals.Pop();
                        er.Filter(ref _events);

                        // Now determine the correct number of regular and daemon events in the executive.
                        // TODO: Can we do this outside the while loop?
                        _numDaemonEventsInQueue = 0;
                        _numEventsInQueue = 0;
                        foreach (ExecEvent ee in _events.Keys)
                        {
                            _numEventsInQueue++;
                            if (ee.IsDaemon)
                                _numDaemonEventsInQueue++;
                        }
                    }
                    #endregion Process queued-up event removal requests

                    ExecEvent currentEvent;
                    #region Identify and select the current event
                    lock (_events)
                    {
                        // TODO: While awaiting this lock, the last even may have been resc
                        if (_numEventsInQueue > 0)
                        {
                            try
                            {  // MTHACK
                                currentEvent = (ExecEvent)_events.GetKey(0);
                                _events.RemoveAt(0);
                                _currentPriorityLevel = currentEvent.Priority;
                                _lastEventServiceTime = _now;
                                _now = currentEvent.When;
                            }
                            catch
                            { // MTHACK 
                                break;  // MTHACK 
                            } // MTHACK 
                        }
                        else
                        {
                            break;
                        }
                    }
                    #endregion Identify and select the current event

                    _eventAboutToFire?.Invoke(currentEvent.Key, currentEvent.ExecEventReceiver, currentEvent.Priority, currentEvent.When, currentEvent.UserData, currentEvent.EventType);

                    try
                    {
                        _currentEventType = currentEvent.EventType;
                        if (currentEvent.IsDaemon)
                            _numDaemonEventsInQueue--;
                        _numEventsInQueue--;
                        if (_diagnostics)
                            _Debug.WriteLine(string.Format(_eventSvcMsg, currentEvent, currentEvent.ExecEventReceiver.Target, currentEvent.ExecEventReceiver.Target.GetHashCode(), currentEvent.ExecEventReceiver.Method.Name));
                        switch (currentEvent.EventType)
                        {
                            case ExecEventType.Synchronous:
                                currentEvent.ExecEventReceiver(this, currentEvent.UserData);
                                currentEvent.OnServiceCompleted();
                                break;
                            case ExecEventType.Detachable:
                                _currentDetachableEvent = new DetachableEvent(this, currentEvent);
                                _currentDetachableEvent.Begin();
                                _currentDetachableEvent = null;
                                break;
                            case ExecEventType.Asynchronous:
                                ThreadPool.QueueUserWorkItem(AsyncExecutor, new object[] { this, currentEvent });
                                break;
                            default:
                                throw new ExecutiveException("EventType " + currentEvent.EventType + " is not yet supported.");
                        }
                        _eventHasCompleted?.Invoke(currentEvent.Key, currentEvent.ExecEventReceiver, currentEvent.Priority, currentEvent.When, currentEvent.UserData, currentEvent.EventType);

                    }
                    catch (Exception e)
                    {
                        _terminationException = e;
                        // TODO: Re-throw this exception on the simulation executor's thread.
                        //_Debug.WriteLine("Exception thrown back into the executive : " + e);
                        //Trace.Flush();
                        _stopRequested = true;
                    }
                    finally
                    {
                        _currentEventType = ExecEventType.None;
                    }

                    while (EventLockCount > 0)
                    {
                        Monitor.Pulse(_eventLock);
                        lock (_eventLock)
                        {
                        }
                    }

                    if (_clockAboutToChange != null)
                    {
                        if (_numEventsInQueue > _numDaemonEventsInQueue)
                        {
                            DateTime nextEventTime = ((ExecEvent)_events.GetKey(0)).When;
                            //DateTime nextEventTime = ((ExecEvent)m_events[0]).m_when;
                            if (nextEventTime > _now)
                            {
                                _clockAboutToChange(this);
                            }
                        }
                    }
                }

                if (initialEventCount == _eventCount)
                {
                    _Debug.WriteLine("Simulation completed without having executed a single event.");
                }

                if (_stopRequested)
                {
                    if (_events.Count > 0)
                    {
                        _state = ExecState.Paused;
                        if (_executiveStopped != null)
                            _executiveStopped(this);
                    }
                    else
                    {
                        _state = ExecState.Stopped;
                    }
                    _stopRequested = false; // We've taken care of it.
                }
                else
                {
                    _state = ExecState.Finished;
                }

                if (RunningDetachables.Count > 0)
                {
                    // TODO: Move this error reporting into a StringBuilder, and report it upward, rather than just to Console.
                    ArrayList tmp = new ArrayList(RunningDetachables);
                    foreach (DetachableEvent de in tmp)
                    {
                        bool issuedError = false;
                        if (de.IsWaiting())
                        {
                            if (!de.HasAbortHandler)
                            {
                                if (!issuedError)
                                    _Debug.WriteLine("ERROR : MODEL FINISHED WITH SOME TASKS STILL WAITING TO COMPLETE!");
                                issuedError = true;
                                _Debug.WriteLine("\tWaiting Event : " + de.RootEvent.ToString());
                                _Debug.WriteLine("\tEvent initially called into " + ((ExecEvent)de.RootEvent).ExecEventReceiver.Target + ", on method " + ((ExecEvent)de.RootEvent).ExecEventReceiver.Method.Name);
                                _Debug.WriteLine("\tThe event was suspended at time " + de.TimeOfLastWait + ", and was never resumed.");
                                if (de.SuspendedStackTrace != null)
                                    _Debug.WriteLine("CALL STACK:\r\n" + de.SuspendedStackTrace);
                            }
                            de.Abort();
                        }
                    }
                    _currentDetachableEvent = null;

                    while (RunningDetachables.Count > 0)
                        Thread.SpinWait(1);

                    if (_executiveAborted != null)
                        _executiveAborted(this);
                }

                //if (m_terminationException != null) {
                //    m_pauseMgr.Abort();
                //    if (m_executiveFinished != null)
                //        m_executiveFinished(this);
                //    throw new RuntimeException(String.Format("Executive with hashcode {0} experienced an exception in user code.", GetHashCode()), m_terminationException);
                //}
            }

            _pauseMgr.Abort();

            _executiveFinished?.Invoke(this);

            if (_terminationException != null && !_abortRequested)
            {
                throw new RuntimeException(String.Format("Executive with hashcode {0} experienced an exception in user code.", GetHashCode()), _terminationException);
            }
            _abortRequested = false;
        }
        private static string _eventSvcMsg = "Serving {0} event to {1}({2}).{3}";


        private void DumpEventQueue()
        {
            lock (_events)
            {
                _Debug.WriteLine("Event Queue: (highest number served next)");
                foreach (DictionaryEntry de in _events)
                {
                    ExecEvent ee = (ExecEvent)de.Key;
                    if (ee.ExecEventReceiver.Target is DetachableEvent)
                    {
                        ExecEventReceiver eer = ((ExecEvent)((DetachableEvent)ee.ExecEventReceiver.Target).RootEvent).ExecEventReceiver;
                        _Debug.WriteLine(de.Value + ").\t" + ee.EventType + " Event is waiting to be fired at time " + ee.When + " into " + eer.Target + "(" + eer.Target.GetHashCode() + "), " + eer.Method.Name);
                    }
                    else
                    {
                        _Debug.WriteLine(de.Value + ").\t" + ee.EventType + " Event is waiting to be fired at time " + ee.When + " into " + ee.ExecEventReceiver.Target + "(" + ee.ExecEventReceiver.Target.GetHashCode() + "), " + ee.ExecEventReceiver.Method.Name);
                    }
                }
                _Debug.WriteLine("***********************************");
            }
        }


        public IDetachableEventController CurrentEventController
        {
            get
            {
                return _currentDetachableEvent;
            }
        }

        /// <summary>
        /// The type of event currently being serviced by the executive.
        /// </summary>
        public ExecEventType CurrentEventType
        {
            get
            {
                return _currentEventType;
            }
        }

        internal void SetCurrentEventType(ExecEventType eet)
        {
            _currentEventType = eet;
        }


        internal void SetCurrentEventController(DetachableEvent de)
        {
            //if ( m_currentDetachableEvent != null ) {
            //    throw new ExecutiveException("Attempt to overwrite the current detachable event!");
            //}
            _currentDetachableEvent = de;
        }

        private readonly object _pauseLock = new object();
        private readonly object _runLock = new object();
        private Thread _pauseMgr = null;
        private void doPause()
        {
            try
            {
                while (true)
                {
                    lock (_runLock)
                    {
                        //Console.WriteLine("Pause thread is waiting for a pulse on RunLock.");

                        Monitor.Wait(_runLock);
                        //Console.WriteLine("Pause thread received a pulse on RunLock.");
                    }
                    _state = ExecState.Paused;
                    lock (_pauseLock)
                    {
                        //Console.WriteLine("Pause thread is acquiring an exclusive handle on RunLock.");
                        Monitor.Enter(_runLock);
                        //Console.WriteLine("Pause thread is waiting for a pulse on PauseLock.");
                        Monitor.Wait(_pauseLock);
                        //Console.WriteLine("Pause thread received a pulse on PauseLock.");
                        //Console.WriteLine("Pause thread is releasing its exclusive handle on RunLock.");
                        Monitor.Exit(_runLock);
                    }
                    _state = ExecState.Running;
                }
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
        }
        /// <summary>
        /// If running, pauses the executive and transitions its state to 'Paused'.
        /// </summary>
        public void Pause()
        {
            if (_state.Equals(ExecState.Running))
            {
                lock (_runLock)
                {
                    //Console.WriteLine("User thread is pulsing RunLock.");
                    Monitor.Pulse(_runLock);
                }
                if (_executivePaused != null)
                    _executivePaused(this);
            }
        }

        /// <summary>
        /// If paused, unpauses the executive and transitions its state to 'Running'.
        /// </summary>
        public void Resume()
        {
            if (_state.Equals(ExecState.Paused))
            {
                lock (_pauseLock)
                {
                    //Console.WriteLine("User thread is pulsing PauseLock.");
                    Monitor.Pulse(_pauseLock);
                }
                if (_executiveResumed != null)
                    _executiveResumed(this);
            }
        }

        public void Stop()
        {
            // State change happens in the processing loop when 'm_stopRequested' is discovered true.
            _stopRequested = true;
            if (_state.Equals(ExecState.Paused))
            {
                lock (_pauseLock)
                {
                    //Console.WriteLine("User thread is pulsing PauseLock.");
                    Monitor.Pulse(_pauseLock);
                }
            }
        }


        public void Abort()
        {
            // We need to do two things. First, we have to abort any Detachable Events that are currently
            // running. Second, we need to scrub all possible graphcontexts that could have been in process
            // in those detachable events.
            _abortRequested = true;
            foreach (DetachableEvent de in RunningDetachables)
                de.Abort();

            Reset();
        }

        /// <summary>
        /// Resets the executive - this clears the event list and resets now to 1/1/01, 12:00 AM
        /// </summary>
        public void Reset()
        {
            _state = ExecState.Stopped;
            _now = DateTime.MinValue;
            _events = new SortedList(new ExecEventComparer());
            _currentPriorityLevel = double.MinValue;
            _stopRequested = false;
            _numEventsInQueue = 0;
            _numDaemonEventsInQueue = 0;

            // We were hanging the executive if we reset while it was paused.
            Resume();

            if (_executiveReset != null)
                _executiveReset(this);
        }

        /// <summary>
        /// Removes all instances of .NET event and simulation discrete event callbacks from this executive.
        /// </summary>
        /// <param name="target">The object to be detached from this executive.</param>
        public void Detach(object target)
        {

            foreach (ExecutiveEvent md in new ExecutiveEvent[] { _clockAboutToChange, _executiveAborted, _executiveFinished, _executiveReset, _executiveStarted, _executiveStartedSingleShot, _executiveStopped })
            {
                if (md == null)
                    continue;
                ExecutiveEvent tmp = md;
                List<ExecutiveEvent> lstDels = new List<ExecutiveEvent>();
                foreach (ExecutiveEvent ee in md.GetInvocationList())
                {
                    if (ee.Target == target)
                    {
                        lstDels.Add(ee);
                    }
                }
                lstDels.ForEach(n => tmp -= n);
            }

            foreach (EventMonitor em in new EventMonitor[] { _eventAboutToFire, _eventHasCompleted })
            {
                if (em == null)
                    continue;
                EventMonitor tmp = em;
                List<EventMonitor> lstDels = new List<EventMonitor>();
                foreach (EventMonitor ee in em.GetInvocationList())
                {
                    if (ee.Target == target)
                    {
                        lstDels.Add(ee);
                    }
                }
                lstDels.ForEach(n => tmp -= n);
            }

            UnRequestEvents(target);

        }

        // If a lock is held on the Executive, then any attempt to add a handler to a public event is
        // blocked until that lock is released. This can cause client code to freeze, expecially if running
        // in a detachable event and adding a handler to an executive event. For that reason, all public
        // event members are methods with add {} and remove {} that defer to private event members. This
        // does not cause the aforementioned lockup.
        private event ExecutiveEvent _executiveStarted;
        private event ExecutiveEvent _executiveStartedSingleShot;
        private event ExecutiveEvent _executiveStopped;
        private event ExecutiveEvent _executivePaused;
        private event ExecutiveEvent _executiveResumed;
        private event ExecutiveEvent _executiveFinished;
        private event ExecutiveEvent _executiveAborted;
        private event ExecutiveEvent _executiveReset;
        private event ExecutiveEvent _clockAboutToChange;
        private event EventMonitor _eventAboutToFire;
        private event EventMonitor _eventHasCompleted;


        public event ExecutiveEvent ExecutiveStarted_SingleShot
        {
            add
            {
                _executiveStartedSingleShot += value;
            }
            remove
            {
                _executiveStartedSingleShot -= value;
            }
        }
        public event ExecutiveEvent ExecutiveStarted
        {
            add
            {
                _executiveStarted += value;
            }
            remove
            {
                _executiveStarted -= value;
            }
        }

        /// <summary>
        /// Fired when this executive pauses.
        /// </summary>
        public event ExecutiveEvent ExecutivePaused
        {
            add
            {
                _executivePaused += value;
            }
            remove
            {
                _executivePaused -= value;
            }
        }
        /// <summary>
        /// Fired when this executive resumes.
        /// </summary>
        public event ExecutiveEvent ExecutiveResumed
        {
            add
            {
                _executiveResumed += value;
            }
            remove
            {
                _executiveResumed -= value;
            }
        }

        public event ExecutiveEvent ExecutiveStopped
        {
            add
            {
                _executiveStopped += value;
            }
            remove
            {
                _executiveStopped -= value;
            }
        }
        public event ExecutiveEvent ExecutiveFinished
        {
            add
            {
                _executiveFinished += value;
            }
            remove
            {
                _executiveFinished -= value;
            }
        }
        public event ExecutiveEvent ExecutiveAborted
        {
            add
            {
                _executiveAborted += value;
            }
            remove
            {
                _executiveAborted -= value;
            }
        }
        public event ExecutiveEvent ExecutiveReset
        {
            add
            {
                _executiveReset += value;
            }
            remove
            {
                _executiveReset -= value;
            }
        }

        /// <summary>
        /// Fired after service of the last event scheduled in the executive to fire at a specific time,
        /// assuming that there are more non-daemon events to fire.
        /// </summary>
        public event ExecutiveEvent ClockAboutToChange
        {
            add
            {
                _clockAboutToChange += value;
            }
            remove
            {
                _clockAboutToChange -= value;
            }
        }

        /// <summary>
        /// Fired after an event has been selected to be fired, but before it actually fires.
        /// </summary>
        public event EventMonitor EventAboutToFire
        {
            add
            {
                _eventAboutToFire += value;
            }
            remove
            {
                _eventAboutToFire -= value;
            }
        }

        /// <summary>
        /// Fired after an event has been selected to be fired, and after it actually fires.
        /// </summary>
        public event EventMonitor EventHasCompleted
        {
            add
            {
                _eventHasCompleted += value;
            }
            remove
            {
                _eventHasCompleted -= value;
            }
        }

        /// <summary>
        /// Must call this before disposing of a model.
        /// </summary>
        /// <value></value>
        public void Dispose()
        {
            try
            {
                if (_pauseMgr != null && _pauseMgr.IsAlive)
                    _pauseMgr.Abort();
            }
            catch { }
        }

        /// <summary>
        /// Acquires the event lock.
        /// </summary>
        internal void AcquireEventLock()
        {
            lock (_eventLock)
            {
                Interlocked.Increment(ref EventLockCount);
            }
        }

        /// <summary>
        /// Releases the event lock.
        /// </summary>
        internal void ReleaseEventLock()
        {
            lock (_eventLock)
            {
                Interlocked.Decrement(ref EventLockCount);
            }
        }

        private static readonly bool _dumpVolatileClearing = false;
        public void ClearVolatiles(IDictionary dictionary)
        {
            if (_dumpVolatileClearing)
                _Debug.WriteLine("---------------------- C L E A R I N G   V O L A T I L E S --------------------------------");
            ArrayList entriesToRemove = new ArrayList();
            foreach (DictionaryEntry de in dictionary)
            {

                if (_dumpVolatileClearing)
                    _Debug.WriteLine("Checking key " + de.Key + " and value " + de.Value);

                if (de.Key.GetType().GetCustomAttributes(typeof(TaskGraphVolatileAttribute), true).Length > 0)
                {
                    entriesToRemove.Add(de.Key);
                }
                else if (de.Value != null)
                {
                    if (de.Value.GetType().GetCustomAttributes(typeof(TaskGraphVolatileAttribute), true).Length > 0)
                    {
                        entriesToRemove.Add(de.Key);
                    }
                }
            }
            foreach (object key in entriesToRemove)
            {
                if (_dumpVolatileClearing)
                    _Debug.WriteLine("Removing volatile listed under key " + key);
                dictionary.Remove(key);
            }
            if (_dumpVolatileClearing)
                _Debug.WriteLine("---------------------- C L E A R E D  " + entriesToRemove.Count + "   V O L A T I L E S --------------------------------");

            if (_dumpVolatileClearing)
            {
                _Debug.WriteLine("Here's what's left:");
                foreach (DictionaryEntry de in dictionary)
                {
                    _Debug.WriteLine(de.Key + "\t" + de.Value);
                }
            }
        }

        private static void AsyncExecutor(object payload)
        {
            object[] p = (object[])payload;
            IExecutive executive = (IExecutive)p[0];
            ExecEvent execEvent = (ExecEvent)p[1];
            execEvent.ExecEventReceiver(executive, execEvent.UserData);
        }
    }

}
