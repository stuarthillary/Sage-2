/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.ItemBased.Ports
{
    /// <summary>
    /// IInputPort is the portion of an InputPort that is intended to be visible
    /// and accessible from outside the scope of its owner.
    /// </summary>
    public interface IInputPort : IPort
    {
        /// <summary>
        /// This method attempts to place the provided data object onto the port from
        /// upstream of its owner. It will succeed if the port is unoccupied, or if
        /// the port is occupied and the port permits overwrites.
        /// </summary>
        /// <param name="obj">the data object</param>
        /// <returns>True if successful. False if it fails.</returns>
        bool Put(object obj); // True if accepted.

        /// <summary>
        /// This is called by a peer to let the input port know that there is data
        /// available at the peer, in case the input port wants to pull the data.
        /// </summary>
        void NotifyDataAvailable();

        /// <summary>
        /// This sets the PutHandler that this port will use, replacing the current
        /// one. This should be used only by objects under the control of, or owned by, the
        /// IPortOwner that owns this port.
        /// </summary>
        /// <value>The new PutHandler.</value>
        DataArrivalHandler PutHandler
        {
            get; set;
        }
    }

}
