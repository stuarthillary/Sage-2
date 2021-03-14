/* This source code licensed under the GNU Affero General Public License */

using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased.Ports
{
    /// <summary>
    /// Class PortManagementFacade provides one object from which the managers for all of a group of ports owned by one owner
    /// can be obtained. It is a convenience class.
    /// </summary>
    public class PortManagementFacade
    {

        private readonly Dictionary<IInputPort, InputPortManager> _inputPortManagers;
        private readonly Dictionary<IOutputPort, OutputPortManager> _outputPortManagers;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortManagementFacade"/> class with the ports owned by the specified port owner.
        /// </summary>
        /// <param name="portOwner">The port owner.</param>
        public PortManagementFacade(IPortOwner portOwner)
        {
            _inputPortManagers = new Dictionary<IInputPort, InputPortManager>();
            _outputPortManagers = new Dictionary<IOutputPort, OutputPortManager>();
            foreach (IInputPort iip in portOwner.Ports.Inputs)
            {
                _inputPortManagers.Add(iip, new InputPortManager((SimpleInputPort)iip));
            }
            foreach (IOutputPort iop in portOwner.Ports.Outputs)
            {
                _outputPortManagers.Add(iop, new OutputPortManager((SimpleOutputPort)iop));
            }
        }

        /// <summary>
        /// Obtains the manager for a specified input port.
        /// </summary>
        /// <param name="iip">The input port.</param>
        /// <returns>InputPortManager.</returns>
        public InputPortManager ManagerFor(IInputPort iip)
        {
            return _inputPortManagers[iip];
        }
        /// <summary>
        /// Obtains the manager for a specified output port.
        /// </summary>
        /// <param name="iop">The output port.</param>
        /// <returns>OutputPortManager.</returns>
        public OutputPortManager ManagerFor(IOutputPort iop)
        {
            return _outputPortManagers[iop];
        }
    }
}
