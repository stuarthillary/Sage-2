/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.ItemBased.Ports
{
    /// <summary>
    /// IOutputPort is the portion of an output port that is intended to be visible 
    /// and accessible from outside the scope of its owner. 
    /// </summary>
    public interface IOutputPort : IPort
    {
        /// <summary>
        /// This method removes and returns the current contents of the port.
        /// </summary>
        /// <param name="selector">An object that is used in the dataProvider to
        /// determine which of potentially more than one available data element is
        /// to be provided to the requestor.</param>
        /// <returns>The current contents of the port.</returns>
        object Take(object selector);

        /// <summary>
        /// True if Peek can be expected to return meaningful data.
        /// </summary>
        bool IsPeekable
        {
            get;
        }

        /// <summary>
        /// Nonconsumptively returns the contents of this port. A subsequent Take
        /// may or may not produce the same object, if, for example, the stuff
        /// produced from this port is time-sensitive.
        /// </summary>
        /// <param name="selector">An object that is used in the dataProvider to
        /// determine which of potentially more than one available data element is
        /// to be provided to the requestor.</param>
        /// <returns>
        /// The current contents of this port. Null if this port is not peekable.
        /// </returns>
        object Peek(object selector);

        /// <summary>
        /// This event is fired when new data is available to be taken from a port.
        /// </summary>
        event PortEvent DataAvailable;


        /// <summary>
        /// This sets the DataProvisionHandler that this port will use to handle requests
        /// to take data from this port, replacing the current one. This should be used
        /// only by objects under the control of, or owned by, the IPortOwner that owns
        /// this port.
        /// </summary>
        /// <value>The take handler.</value>
        DataProvisionHandler TakeHandler
        {
            get; set;
        }

        /// <summary>
        /// This sets the DataProvisionHandler that this port will use to handle requests
        /// to peek at data on this port, replacing the current one. This should be used
        /// only by objects under the control of, or owned by, the IPortOwner that owns
        /// this port.
        /// </summary>
        /// <value>The peek handler.</value>
        DataProvisionHandler PeekHandler
        {
            get; set;
        }

    }

}
