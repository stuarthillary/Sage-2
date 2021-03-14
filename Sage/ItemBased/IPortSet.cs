/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using System.Collections.ObjectModel;

namespace Highpoint.Sage.ItemBased.Ports
{
    /// <summary>
    /// An interface implemented by a PortSet. Permits indexing to a port by key.
    /// </summary>
    public interface IPortSet : IEnumerable, IPortEvents
    {
        /// <summary>
        /// Permits a caller to retrieve a port by its guid.
        /// </summary>
        IPort this[Guid key] { get; }
        /// <summary>
        /// Permits a caller to retrieve a port by its name.
        /// </summary>
        IPort this[string name] { get; }
        /// <summary>
        /// Gets the <see cref="Highpoint.Sage.ItemBased.Ports.IPort"/> with the specified index, i.
        /// </summary>
        /// <value>The <see cref="Highpoint.Sage.ItemBased.Ports.IPort"/>.</value>
        IPort this[int i] { get; }
        /// <summary>
        /// Adds a port to this object's port set.
        /// </summary>
        /// <param name="port">The port to be added to the portSet.</param>
        void AddPort(IPort port);

        /// <summary>
        /// Removes a port from an object's portset. Any entity having references
        /// to the port may still use it, though this may be wrong from an application
        /// perspective.
        /// </summary>
        /// <param name="port">The port to be removed from the portSet.</param>
        void RemovePort(IPort port);

        /// <summary>
        /// Unregisters all ports.
        /// </summary>
        void ClearPorts();

        /// <summary>
        /// Fired when a port has been added to this IPortSet.
        /// </summary>
        event PortEvent PortAdded;

        /// <summary>
        /// Fired when a port has been removed from this IPortSet.
        /// </summary>
        event PortEvent PortRemoved;

        /// <summary>
        /// Returns a collection of the keys that belong to ports known to this PortSet.
        /// </summary>
        ICollection PortKeys
        {
            get;
        }

        /// <summary>
        /// Looks up the key associated with a particular port.
        /// </summary>
        /// <param name="port">The port for which we want the key.</param>
        /// <returns>The key for the provided port.</returns>
        [Obsolete("Ports use their Guids as the key.")]
        Guid GetKey(IPort port);

        /// <summary>
        /// Gets the count of all kids of ports in this collection.
        /// </summary>
        /// <value>The count.</value>
        int Count
        {
            get;
        }

        /// <summary>
        /// Gets the output ports owned by this PortSet.
        /// </summary>
        /// <value>The output ports.</value>
        ReadOnlyCollection<IOutputPort> Outputs
        {
            get;
        }

        /// <summary>
        /// Gets the input ports owned by this PortSet.
        /// </summary>
        /// <value>The input ports.</value>
        ReadOnlyCollection<IInputPort> Inputs
        {
            get;
        }

        /// <summary>
        /// Sorts the ports based on one element of their Out-of-band data sets.
        /// Following a return from this call, the ports will be in the order requested.
        /// The &quot;T&quot; parameter will usually be int, double or string, but it must
        /// represent the IComparable-implementing type of the data stored under the
        /// provided OOBDataKey.
        /// </summary>
        /// <param name="oobDataKey">The oob data key.</param>
        void SetSortOrder<T>(object oobDataKey) where T : IComparable;

    }

}
