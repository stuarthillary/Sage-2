/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.ItemBased.Connectors;
using Highpoint.Sage.SimCore;
using System;

// 20080130 : Considered a generic version of ports (and therefore, blocks), but
// decided not to. It would give me type safety, but introduce a lot of new issues,
// like differentiation of multiple port sets (by type), requirement to know the
// type irrespective of whether you cared (such as when responding to a (now-)typed
// event). I can achieve the same thing by adding Type info to the PortChannelInfo,
// and refusing connections where both ends declare incompatible types...

namespace Highpoint.Sage.ItemBased.Ports
{
    /// <summary>
    /// This interface specifies the methods common to all types of ports, that are visible 
    /// to objects other than the owner of the port.
    /// </summary>
    public interface IPort : IPortEvents, IModelObject
    {
        /// <summary>
        /// This property represents the connector object that this port is associated with.
        /// </summary>
        IConnector Connector
        {
            get; set;
        }

        /// <summary>
        /// This property contains the owner of the port.
        /// </summary>
        IPortOwner Owner
        {
            get;
        }

        /// <summary>
        /// Returns the key by which this port is known to its owner.
        /// </summary>
        Guid Key
        {
            get;
        }

        /// <summary>
        /// This property returns the port at the other end of the connector to which this
        /// port is connected, or null, if there is no connector, and/or no port on the
        /// other end of a connected connector.
        /// </summary>
        IPort Peer
        {
            get;
        }

        /// <summary>
        /// Returns the default out-of-band data from this port. Out-of-band data
        /// is data that is not material that is to be transferred out of, or into,
        /// this port, but rather context, type, or other metadata to the transfer
        /// itself.
        /// </summary>
        /// <returns>The default out-of-band data from this port.</returns>
        object GetOutOfBandData();

        /// <summary>
        /// Returns out-of-band data from this port. Out-of-band data is data that is
        /// not material that is to be transferred out of, or into, this port, but
        /// rather context, type, or other metadata to the transfer itself.
        /// </summary>
        /// <param name="selector">The key of the sought metadata.</param>
        /// <returns>The desired out-of-band metadata.</returns>
        object GetOutOfBandData(object selector);

        /// <summary>
        /// Gets a value indicating whether this <see cref="Highpoint.Sage.ItemBased.Ports.IPort"/> is intrinsic. An intrinsic
        /// port is a hard-wired part of its owner. It is there when its owner is created, and
        /// cannot be removed.
        /// </summary>
        /// <value><c>true</c> if intrinsic; otherwise, <c>false</c>.</value>
        bool Intrinsic
        {
            get;
        }

        /// <summary>
        /// Detaches this port's data handlers.
        /// </summary>
        void DetachHandlers();

        /// <summary>
        /// The port index represents its sequence, if any, with respect to the other ports.
        /// </summary>
        int Index
        {
            get; set;
        }

    }

}
