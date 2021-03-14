/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using System;
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased.Ports
{
    /// <summary>
    /// Class PlaceholderPortOwner is a class to which the duties of PortOwner can be delegated.
    /// </summary>
    /// <seealso cref="Highpoint.Sage.ItemBased.Ports.IPortOwner" />
    /// <seealso cref="Highpoint.Sage.SimCore.IHasName" />
    internal class PortOwnerProxy : IPortOwner, IHasName
    {
        private readonly string _name;

        private readonly PortSet _portSet = new PortSet();

        public PortOwnerProxy(string name)
        {
            _name = name;
        }
        #region IPortOwner Members

        /// <summary>
        /// Adds a user-created port to this object's port set.
        /// </summary>
        /// <param name="port">The port to be added to the portSet.</param>
        public void AddPort(IPort port)
        {
            _portSet.AddPort(port);
        }

        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel.
        /// </summary>
        /// <param name="channel">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <returns>The newly-created port. Can return null if this is not supported.</returns>
        public IPort AddPort(string channel)
        {
            return null; /*Implement AddPort(string channel); */
        }

        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel.
        /// </summary>
        /// <param name="channelTypeName">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <param name="guid">The GUID to be assigned to the new port.</param>
        /// <returns>The newly-created port. Can return null if this is not supported.</returns>
        public IPort AddPort(string channelTypeName, Guid guid)
        {
            return null; /*Implement AddPort(string channel); */
        }

        /// <summary>
        /// Gets the names of supported port channels.
        /// </summary>
        /// <value>The supported channels.</value>
        public List<IPortChannelInfo> SupportedChannelInfo
        {
            get
            {
                return GeneralPortChannelInfo.StdInputAndOutput;
            }
        }

        /// <summary>
        /// Removes a port from an object's portset. Any entity having references
        /// to the port may still use it, though this may be wrong from an application
        /// perspective. Implementers are responsible to refuse removal of a port that
        /// is a hard property exposed (e.g. this.InputPort0), since it will remain
        /// accessible via that property.
        /// </summary>
        /// <param name="port">The port.</param>
        public void RemovePort(IPort port)
        {
            _portSet.RemovePort(port);
        }

        /// <summary>
        /// Unregisters all ports.
        /// </summary>
        public void ClearPorts()
        {
            _portSet.ClearPorts();
        }

        /// <summary>
        /// A PortSet containing the ports that this port owner owns.
        /// </summary>
        /// <value>The ports.</value>
        public IPortSet Ports
        {
            get
            {
                return _portSet;
            }
        }

        #endregion

        #region IHasName Members

        /// <summary>
        /// The user-friendly name for this object.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get
            {
                return _name;
            }
        }

        #endregion
    }
}
