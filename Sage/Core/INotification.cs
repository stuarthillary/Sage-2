/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// This interface is implemented by an object that provides notification of some event.
    /// </summary>
    public interface INotification
    {
        /// <summary>
        /// The name of the notification.
        /// </summary>
        string Name
        {
            get;
        }
        /// <summary>
        /// A descriptive text that describes what happened.
        /// </summary>
        string Narrative
        {
            get;
        }
        /// <summary>
        /// Target is the place that the notification occurred.
        /// </summary>
        object Target
        {
            get;
        }
        /// <summary>
        /// Subject is the thing that (probably) caused the notification.
        /// </summary>
        object Subject
        {
            get;
        }
    }


}

