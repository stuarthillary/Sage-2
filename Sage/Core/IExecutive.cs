/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// This delegate is implemented by any method that is to receive a time-based callback
    /// from the executive.
    /// </summary>
    public delegate void ExecEventReceiver(IExecutive exec, object userData);

    /// <summary>
    /// Implemented by any object that wishes to be notified as an event is firing.
    /// </summary>
    public delegate void EventMonitor(long key, ExecEventReceiver eer, double priority, DateTime when, object userData, ExecEventType eventType);

    /// <summary>
    /// Implemented by any method that wants to receive notification of an executive event
    /// such as ExecutiveStarted, ExecutiveStopped, ExecutiveReset, ExecutiveFinished.
    /// </summary>
    public delegate void ExecutiveEvent(IExecutive exec);


    /// <summary>
    /// Interface that is implemented by an executive.
    /// </summary>
    public interface IExecutive : IDisposable
    {
        /// <summary>
        /// The Guid by which this executive is known.
        /// </summary>
        Guid Guid
        {
            get;
        }
        /// <summary>
        /// The current DateTime being managed by this executive. This is the 'Now' point of a
        /// simulation being run by this executive.
        /// </summary>
        DateTime Now
        {
            get;
        }
        /// <summary>
        /// If this executive has been run, this holds the DateTime of the last event serviced. May be from a previous run.
        /// </summary>
        DateTime? LastEventServed
        {
            get;
        }
        /// <summary>
        /// The priority of the event currently being serviced.
        /// </summary>
        double CurrentPriorityLevel
        {
            get;
        }
        /// <summary>
        /// The current state of this executive (running, stopped, paused, finished)
        /// </summary>
        ExecState State
        {
            get;
        }
        /// <summary>
        /// Requests that the executive queue up an event to be serviced at the current executive time and
        /// priority.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="userData">Object data to be provided in the callback.</param>
        /// <param name="execEventType">The way the event is to be served by the executive.</param>
        /// <returns>A code that can subsequently be used to identify the request, e.g. for removal.</returns>
        long RequestImmediateEvent(ExecEventReceiver eer, object userData, ExecEventType execEventType);
        /// <summary>
        /// Requests that the executive queue up a daemon event to be serviced at a specific time and
        /// priority. If only daemon events are enqueued, the executive will not be kept alive.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="when">The date &amp; time at which the callback is to be made.</param>
        /// <param name="priority">The priority of the callback. Higher numbers mean higher priorities.</param>
        /// <param name="userData">Object data to be provided in the callback.</param>
        /// <returns>A code that can subsequently be used to identify the request, e.g. for removal.</returns>
        long RequestDaemonEvent(ExecEventReceiver eer, DateTime when, double priority, object userData);
        /// <summary>
        /// Requests that the executive queue up an event to be serviced at a specific time. Priority is assumed
        /// to be zero, and the userData object is assumed to be null.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="when">The date &amp; time at which the callback is to be made.</param>
        /// <returns>A code that can subsequently be used to identify the request, e.g. for removal.</returns>
        long RequestEvent(ExecEventReceiver eer, DateTime when);
        /// <summary>
        /// Requests that the executive queue up an event to be serviced at a specific time. Priority is assumed
        /// to be zero.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="when">The date &amp; time at which the callback is to be made.</param>
        /// <param name="userData">Object data to be provided in the callback.</param>
        /// <returns>A code that can subsequently be used to identify the request, e.g. for removal.</returns>
        long RequestEvent(ExecEventReceiver eer, DateTime when, object userData);
        /// <summary>
        /// Requests that the executive queue up an event to be serviced at a specific time and
        /// priority.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
        /// <param name="when">The date &amp; time at which the callback is to be made.</param>
        /// <param name="priority">The priority of the callback. Higher numbers mean higher priorities.</param>
        /// <param name="userData">Object data to be provided in the callback.</param>
        /// <returns>A code that can subsequently be used to identify the request, e.g. for removal.</returns>
        long RequestEvent(ExecEventReceiver eer, DateTime when, double priority, object userData);
        /// <summary>
		/// Requests that the executive queue up an event to be serviced at a specific time and
		/// priority.
		/// </summary>
		/// <param name="eer">The ExecEventReceiver callback that is to receive the callback.</param>
		/// <param name="when">The date &amp; time at which the callback is to be made.</param>
		/// <param name="priority">The priority of the callback. Higher numbers mean higher priorities.</param>
		/// <param name="userData">Object data to be provided in the callback.</param>
		/// <param name="execEventType">The way the event is to be served by the executive.</param>
		/// <returns>A code that can subsequently be used to identify the request, e.g. for removal.</returns>
		long RequestEvent(ExecEventReceiver eer, DateTime when, double priority, object userData, ExecEventType execEventType);
        /// <summary>
        /// Resubmits a copy of an event already in queue, optionally .
        /// </summary>
        /// <param name="eventID">The ID number assigned to the event when it was initially requested.</param>
        /// <param name="newTime">The new time.</param>
        /// <param name="deleteOldOne">If true, will submit the old one for deletion.</param>
        /// <returns>A code that can subsequently be used to identify the request, e.g. for removal.</returns>
        long ResubmitEventAtTime(long eventID, DateTime newTime, bool deleteOldOne);
        /// <summary>
        /// Removes an already-submitted request for a time-based notification.
        /// </summary>
        /// <param name="eventHashCode">The code that identifies the event request to be removed.</param>
        void UnRequestEvent(long eventHashCode);
        /// <summary>
        /// Removes an already-submitted request for a time-based notification based on a user-provided selector object.
        /// </summary>
        /// <param name="ees">An object that will be used to select the events to remove.</param>
        void UnRequestEvents(IExecEventSelector ees);
        /// <summary>
        /// Removes all already-submitted requests for a time-based notification into a specific callback target object.
        /// </summary>
        /// <param name="execEventReceiverTarget">The callback target for which all queued events are to be removed.</param>
        void UnRequestEvents(object execEventReceiverTarget);
        /// <summary>
        /// Removes all already-submitted requests for a time-based notification into a specific callback target object.
        /// </summary>
        /// <param name="execEventReceiverMethod">The callback method for which all queued events are to be removed.</param>
        void UnRequestEvents(Delegate execEventReceiverMethod);
        /// <summary>
        /// This method blocks until the events that correlate to the provided event codes (which are returned from the RequestEvent
        /// APIs) are completely serviced. The event on whose thread this method is called must be a detachable event, all of the
        /// provided events must have been requested already, and none can have already been serviced.
        /// </summary>
        /// <param name="eventCodes">The event codes.</param>
        void Join(params long[] eventCodes);
        /// <summary>
        /// Starts the executive. The calling thread will be the primary execution thread, and will not return until
        /// execution is completed (via completion of all non-daemon events or the Abort method.)
        /// </summary>
        void Start();
        /// <summary>
        /// If running, pauses the executive and transitions its state to 'Paused'.
        /// </summary>
        void Pause();
        /// <summary>
        /// If paused, unpauses the executive and transitions its state to 'Running'.
        /// </summary>
        void Resume();
        /// <summary>
        /// Stops the executive. This may be a pause or a stop, depending on if events are queued or running at the time of call.
        /// </summary>
        void Stop();
        /// <summary>
        /// Aborts the executive. This always flushes the event queue and terminates all running events.
        /// </summary>
        void Abort();
        /// <summary>
        /// Resets the executive - this clears the event list and resets now to 1/1/01, 12:00 AM
        /// </summary>
        void Reset();

        /// <summary>
        /// Removes all instances of .NET event and simulation discrete event callbacks from this executive.
        /// </summary>
        /// <param name="target">The object to be detached from this executive.</param>
        void Detach(object target);

        /// <summary>
		/// Removes any entries in the task graph whose keys or values have the TaskGraphVolatile attribute.
		/// This is used, typically, to 'reset' the task graph for a new simulation run.
		/// </summary>
		/// <param name="dictionary">The task graph context to be 'reset'.</param>
		void ClearVolatiles(IDictionary dictionary);
        /// <summary>
        /// The DetachableEventController associated with the currently-executing event, if it was
        /// launched as a detachable event. Otherwise, it returns null.
        /// </summary>
        IDetachableEventController CurrentEventController
        {
            get;
        }

        /// <summary>
        /// The type of event currently being serviced by the executive.
        /// </summary>
        ExecEventType CurrentEventType
        {
            get;
        }

        /// <summary>
        /// Returns a list of the detachable events that are currently running.
        /// </summary>
        ArrayList LiveDetachableEvents
        {
            get;
        }

        /// <summary>
        /// Returns a read-only list of the ExecEvents currently in queue for execution.
        /// Cast the elements in the list to IExecEvent to access the items' field values.
        /// </summary>
        IList EventList
        {
            get;
        }

        /// <summary>
        /// The integer count of the number of times this executive has been run.
        /// </summary>
        int RunNumber
        {
            get;
        }

        /// <summary>
        /// The number of events that have been serviced on this run.
        /// </summary>
        UInt32 EventCount
        {
            get;
        }

        /// <summary>
        /// Fired when this executive starts. All events are fired once, and then cleared.
        /// This enables the designer to register this event on starting the model, to
        /// set up the simulation model when the executive starts. If it was not then cleared,
        /// it would be re-registered and then called twice on the second start, three times
        /// on the third call, etc.
        /// </summary>
        event ExecutiveEvent ExecutiveStarted_SingleShot;
        /// <summary>
        /// Fired when this executive starts.
        /// </summary>
        event ExecutiveEvent ExecutiveStarted;
        /// <summary>
        /// Fired when this executive pauses.
        /// </summary>
        event ExecutiveEvent ExecutivePaused;
        /// <summary>
        /// Fired when this executive resumes.
        /// </summary>
        event ExecutiveEvent ExecutiveResumed;
        /// <summary>
        /// Fired when this executive stops.
        /// </summary>
        event ExecutiveEvent ExecutiveStopped;
        /// <summary>
		/// Fired when this executive finishes (including after an abort).
		/// </summary>
		event ExecutiveEvent ExecutiveFinished;
        /// <summary>
        /// Fired when this executive is reset.
        /// </summary>
        event ExecutiveEvent ExecutiveReset;
        /// <summary>
        /// Fired when this executive has been aborted.
        /// </summary>
        event ExecutiveEvent ExecutiveAborted;
        /// <summary>
        /// Fired after service of the last event scheduled in the executive to fire at a specific time,
        /// assuming that there are more non-daemon events to fire.
        /// </summary>
        event ExecutiveEvent ClockAboutToChange;
        /// <summary>
		/// Fired after an event has been selected to be fired, but before it actually fires.
		/// </summary>
		event EventMonitor EventAboutToFire;
        /// <summary>
        /// Fired after an event has been selected to be fired, and after it actually fires.
        /// </summary>
        event EventMonitor EventHasCompleted;

        void SetStartTime(DateTime startTime);

    }
}
