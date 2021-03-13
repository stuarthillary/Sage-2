/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// Used to select the way the Executive dispatches an event once its time has arrived.
    /// These mechanisms are as follows:
    /// Synchronous – the callback is called on the dispatch thread, and upon completion,
    /// the next callback is selected based upon scheduled time and priority.
    /// Detachable – the callback is called on a thread from the .Net thread pool, and the
    /// dispatch thread then suspends awaiting the completion or suspension of that thread.
    /// If the event thread is suspended, an event controller is made available to other
    /// entities which can be used to resume or abort that thread. This is useful for modeling
    /// “intelligent entities” and situations where the developer wants to easily represent a
    /// delay or interruption of a process.
    /// Batched – all events at the current time and priority are called, each on separate
    /// threads, and the executive, except for servicing any new events registered for that
    /// time and priority, awaits completion of all running events. This may bring about higher
    /// performance in cases such as battlefield and transportation simulations where multiple
    /// entities may sense current conditions, plan and execute against that plan.
    /// Asynchronous - This mechanism is not yet supported.
    /// </summary>
    public enum ExecEventType
    {
        /// <summary>
        /// The executive event (served to a requester) is synchronous. It will execute to its
        /// completion on the executive's thread, and no new events will be serviced until after
        /// its return.
        /// </summary>
        Synchronous,
        //		/// <summary>
        //		/// The execution event (served to a requester) is batched. All events of a specified
        //		/// priority, and for the 'latest' time, are pulled off the stack and executed at the
        //		/// same time, in different threads. Not yet implemented.
        //		/// </summary>
        //		Batched,
        /// <summary>
        /// The execution event (served to a requester) is detachable. It is executed in its own
        /// thread, and may be paused or put to sleep, joined with other threads, or allowed by
        /// the programmer to run in parallel to other executing executive threads. 
        /// </summary>
        Detachable,
        /// <summary>
        /// The execution event (served to a requester) is asynchronous. The thread is given the
        /// callback, the callback is fired, and the executive runs on. Useful for I/O. 
        /// </summary>
        Asynchronous,
        /// <summary>
        /// This enumeration value should not be used to request an event. It is used to indicate
        /// that no event is currently being serviced.
        /// </summary>
        None
    }
}
