/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Diagnostics;

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// This interface is implemented by a DetachableEventController - a DEC is associated
    /// with an event that is fired as a detachable event, and in that event's thread, can
    /// be obtained from the executive.
    /// </summary>
    public interface IDetachableEventController
    {
        /// <summary>
        /// Suspends this detachable event until it is explicitly resumed.
        /// </summary>
        void Suspend();

        /// <summary>
        /// Explicitly resumes this detachable event.
        /// </summary>
        void Resume();

        /// <summary>
        /// Explicitly resumes this detachable event with a specified (override) priority.
        /// This does not replace the initiating event's actual priority, and affects only the scheduling of the resuming event.
        /// </summary>
        void Resume(double overridePriority);

        /// <summary>
        /// Suspends this detachable event for a specified duration.
        /// </summary>
        /// <param name="howLong"></param>
        void SuspendFor(TimeSpan howLong);

        /// <summary>
        /// Suspends this detachable event until a specified time.
        /// </summary>
        /// <param name="when"></param>
        void SuspendUntil(DateTime when);

        /// <summary>
		/// When a detachable event is suspended, and if DetachableEventController diagnostics are turned on,
		/// this will return a stackTrace of the location where the DEC is suspended.
		/// </summary>
		StackTrace SuspendedStackTrace
        {
            get;
        }

        /// <summary>
        /// Returns true if the IDetachableEventController is at a wait. If this is true,
        /// and the IExecutive has completed its run, it usually means that some event in
        /// the simulation is blocked.
        /// </summary>
        /// <returns>true if the IDetachableEventController is at a wait.</returns>
        bool IsWaiting();

        /// <summary>
        /// Sets the abort handler.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="args">The args.</param>
        void SetAbortHandler(DetachableEventAbortHandler handler, params object[] args);
        /// <summary>
        /// Clears the abort handler.
        /// </summary>
        void ClearAbortHandler();

        /// <summary>
        /// Fires, and then clears, the abort handler.
        /// </summary>
        void FireAbortHandler();

        /// <summary>
        /// Returns the event that initially created this DetachableEventController.
        /// </summary>
        IExecEvent RootEvent
        {
            get;
        }
    }
}
