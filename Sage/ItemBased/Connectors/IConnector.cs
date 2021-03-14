/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.Persistence;
using Highpoint.Sage.SimCore;

namespace Highpoint.Sage.ItemBased.Connectors
{
    public interface IConnector : IModelObject, System.ComponentModel.INotifyPropertyChanged, IXElementSerializable
    {
        /// <summary>
        /// Gets the upstream port.
        /// </summary>
        /// <value>The upstream port.</value>
        IOutputPort Upstream
        {
            get;
        }
        /// <summary>
        /// Gets the downstream port.
        /// </summary>
        /// <value>The downstream port.</value>
        IInputPort Downstream
        {
            get;
        }
        /// <summary>
        /// Disconnects this connector from its upstream and downstream ports, and then removes them from their owners, if possible.
        /// </summary>
        void Disconnect();
        /// <summary>
        /// Connects the specified port, p1 (the upstream port) to the specified port, p2 (the downstream port.)
        /// </summary>
        /// <param name="p1">The upstream port.</param>
        /// <param name="p2">The downstream port.</param>
        void Connect(IPort p1, IPort p2);
        /// <summary>
        /// Called by the upstream port to inform the connector, and thereby the downstream port,
        /// that an item is available for pull by its owner.
        /// </summary>
        void NotifyDataAvailable();
        /// <summary>
        /// Retrieves the default out-of-band data for this port. This data is set via an API on GenericPort.
        /// </summary>
        /// <returns>The default out-of-band data for this port.</returns>
        object GetOutOfBandData();
        /// <summary>
        /// Retrieves the out-of-band data corresponding to the provided key, for this port.
        /// This data is set via an API on GenericPort.
        /// </summary>
        /// <param name="key">The key (such as "Priority") associated with this port's out of band data.</param>
        /// <returns>The out-of-band data corresponding to the provided key.</returns>
        object GetOutOfBandData(object key);
        /// <summary>
        /// Gets a value indicating whether this connector is peekable. The downstream port will call this API,
        /// resulting in a passed-through call to the upstream port, where it will declare whether it supports the
        /// 'peek' operation.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is peekable; otherwise, <c>false</c>.
        /// </value>
        bool IsPeekable
        {
            get;
        }
        /// <summary>
        /// Propagates a 'Peek' operation through this connector to the upstream port.
        /// </summary>
        /// <param name="selector">An object that is used in the dataProvider to
        /// determine which of potentially more than one available data element is
        /// to be provided to the requestor.</param>
        /// <returns>The object or item, if any, available on the upstream port. The item is left on the port.</returns>
        object Peek(object selector);
        /// <summary>
        /// Propagates a 'Take' operation through this connector to the upstream port.
        /// </summary>
        /// <param name="selector">An object that is used in the dataProvider to
        /// determine which of potentially more than one available data element is
        /// to be provided to the requestor.</param>
        /// <returns>The object or item, if any, available on the upstream port. The item is removed from the port.</returns>
        object Take(object selector);
        /// <summary>
        /// Puts the specified data onto the downstream port, if possible.
        /// </summary>
        /// <param name="data">The item or data to be put to the downstream port.</param>
        /// <returns>true if the put operation was successful, otherwise (if the port was blocked), false.</returns>
        bool Put(object data);

        /// <summary>
        /// Gets or sets a value indicating whether the connector is currently in use.
        /// </summary>
        /// <value><c>true</c> if [in use]; otherwise, <c>false</c>.</value>
        bool InUse
        {
            get; set;
        }
    }
}