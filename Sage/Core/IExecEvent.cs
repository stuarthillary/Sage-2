/* This source code licensed under the GNU Affero General Public License */

using System;
// ReSharper disable RedundantDefaultMemberInitializer

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// Interface IExecEvent is implemented by an internal class that keeps track of all of the key data
    /// about an event that is to be served by the Executive.
    /// </summary>
    public interface IExecEvent
    {
        /// <summary>
        /// Gets the ExecEventReceiver (the delegate into which the event will be served.)
        /// </summary>
        /// <value>The execute event receiver.</value>
        ExecEventReceiver ExecEventReceiver
        {
            get;
        }
        /// <summary>
        /// Gets the date &amp; time that the event is to be served.
        /// </summary>
        /// <value>The date &amp; time that the event is to be served.</value>
        DateTime When
        {
            get;
        }
        /// <summary>
        /// Gets the priority of the event.
        /// </summary>
        /// <value>The priority.</value>
        double Priority
        {
            get;
        }
        /// <summary>
        /// Gets the user data to be provided to the method into which the event will be served.).
        /// </summary>
        /// <value>The user data.</value>
        object UserData
        {
            get;
        }
        /// <summary>
        /// Gets the <see cref="ExecEventType"/> of the event.
        /// </summary>
        /// <value>The type of the event.</value>
        ExecEventType EventType
        {
            get;
        }
        /// <summary>
        /// Gets the key by which the event is known. This is useful when the event is being rescinded or logged.
        /// </summary>
        /// <value>The key.</value>
        long Key
        {
            get;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a daemon event.
        /// </summary>
        /// <value><c>true</c> if this instance is daemon; otherwise, <c>false</c>.</value>
        bool IsDaemon
        {
            get;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        string ToString();
    }
}
