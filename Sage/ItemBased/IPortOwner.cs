/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased.Ports
{

    /// <summary>
    /// Interface implemented by any object that exposes ports.
    /// </summary>
    public interface IPortOwner
    {
        /// <summary>
        /// Adds a user-created port to this object's port set.
        /// </summary>
        /// <param name="port">The port to be added to the portSet.</param>
        void AddPort(IPort port);

        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel.
        /// </summary>
        /// <param name="channelTypeName">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <returns>The newly-created port.</returns>
        IPort AddPort(string channelTypeName);

        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel with the provided Guid.
        /// </summary>
        /// <param name="channelTypeName">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <param name="guid">The GUID to be assigned to the new port.</param>
        /// <returns>The newly-created port.</returns>
        IPort AddPort(string channelTypeName, Guid guid);

        /// <summary>
        /// Gets the names of supported port channels.
        /// </summary>
        /// <value>The supported channels.</value>
        List<IPortChannelInfo> SupportedChannelInfo
        {
            get;
        }

        /// <summary>
        /// Removes a port from an object's portset. Any entity having references
        /// to the port may still use it, though this may be wrong from an application
        /// perspective. Implementers are responsible to refuse removal of a port that
        /// is a hard property exposed (e.g. this.InputPort0), since it will remain
        /// accessible via that property.
        /// </summary>
        /// <param name="port">The port.</param>
        void RemovePort(IPort port);
        /// <summary>
        /// Unregisters all ports.
        /// </summary>
        void ClearPorts();
        /// <summary>
        /// A PortSet containing the ports that this port owner owns.
        /// </summary>
        IPortSet Ports
        {
            get;
        }
    }

}
