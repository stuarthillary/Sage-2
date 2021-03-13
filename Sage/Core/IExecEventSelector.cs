/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// Implemented by an object that can select events, typically for removal from the
    /// event queue. It is, effectively, a filter. It is able to discern whether an event
    /// meets some criteria.
    /// </summary>
    public interface IExecEventSelector
    {
        /// <summary>
        /// Determines if the presented event is a candidate for the operation being considered, such as removal from the event queue.
        /// </summary>
        /// <param name="eer">The ExecEventReceiver that is to receive this event.</param>
        /// <param name="when">The DateTime that the event was to have been fired.</param>
        /// <param name="priority">The priority of the event.</param>
        /// <param name="userData">The user data that was presented with this event.</param>
        /// <param name="eet">The type of event (synchronous, batched, detachable, etc.)</param>
        /// <returns>True if this event is a candidate for the operation (e.g. removal), False if not.</returns>
        bool SelectThisEvent(ExecEventReceiver eer, DateTime when, double priority, object userData, ExecEventType eet);
    }
}
