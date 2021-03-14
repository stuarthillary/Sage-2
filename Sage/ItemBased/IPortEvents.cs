/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.ItemBased.Ports
{
    /// <summary>
    /// An interface describing the events that are fired by all IPort objects.
    /// </summary>
    public interface IPortEvents
    {
        /// <summary>
        /// This event fires when data is presented on a port. For an input port, this
        /// implies presentation by an outsider, and for an output port, it implies 
        /// presentation by the port owner.
        /// </summary>
        event PortDataEvent PortDataPresented;

        /// <summary>
        /// This event fires when data is accepted by a port. For an input port, this
        /// implies acceptance by the port owner, and for an output port, it implies 
        /// acceptance by an outsider.
        /// </summary>
        event PortDataEvent PortDataAccepted;

        /// <summary>
        /// This event fires when data is rejected by a port. For an input port, this
        /// implies rejection by the port owner, and for an output port, it implies 
        /// rejection by an outsider.
        /// </summary>
        event PortDataEvent PortDataRejected;

        /// <summary>
        /// This event fires immediately before the port's connector property becomes non-null.
        /// </summary>
        event PortEvent BeforeConnectionMade;

        /// <summary>
        /// This event fires immediately after the port's connector property becomes non-null.
        /// </summary>
        event PortEvent AfterConnectionMade;

        /// <summary>
        /// This event fires immediately before the port's connector property becomes null.
        /// </summary>
        event PortEvent BeforeConnectionBroken;

        /// <summary>
        /// This event fires immediately after the port's connector property becomes null.
        /// </summary>
        event PortEvent AfterConnectionBroken;
    }

}
