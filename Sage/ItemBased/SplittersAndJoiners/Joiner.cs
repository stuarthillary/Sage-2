/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.SimCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Highpoint.Sage.ItemBased.SplittersAndJoiners
{

    /// <summary>
    /// Receives an object on its input port, and sends it out one or more output ports, as defined
    /// in a derived class. If it gets a pull from any output port, it pulls from its one input port.
    /// Notification of data available proceeds according to a derived class' logic.
    /// </summary>
    public abstract class Joiner : IJoiner, IPortOwner
    {
        protected SimpleInputPort[] inputs;
        public IInputPort[] Inputs
        {
            get
            {
                return inputs;
            }
        }
        protected SimpleOutputPort output;
        public IOutputPort Output
        {
            get
            {
                return output;
            }
        }



        public Joiner(IModel model, string name, Guid guid, int nIns)
        {
            InitializeIdentity(model, name, null, guid);

            _ports = new PortSet();

            output = new SimpleOutputPort(model, "Output", Guid.NewGuid(), this, GetTakeHandler(), GetPeekHandler());
            // AddPort(m_output); <-- Done in SOP's ctor.

            inputs = new SimpleInputPort[nIns];
            for (int i = 0; i < nIns; i++)
            {
                inputs[i] = new SimpleInputPort(model, "Input" + i, Guid.NewGuid(), this, GetDataArrivalHandler(i));
                Inputs[i] = inputs[i];
                // AddPort(m_inputs[i]); <-- Done in SOP's ctor.
            }

            IMOHelper.RegisterWithModel(this);
        }

        public void AddInputPort()
        {

        }

        protected abstract DataArrivalHandler GetDataArrivalHandler(int i);
        protected abstract DataProvisionHandler GetPeekHandler();
        protected abstract DataProvisionHandler GetTakeHandler();

        #region IPortOwner Implementation
        /// <summary>
        /// The PortSet object to which this IPortOwner delegates.
        /// </summary>
        private readonly PortSet _ports = new PortSet();
        /// <summary>
        /// Registers a port with this IPortOwner
        /// </summary>
        /// <param name="port">The port that this IPortOwner will add.</param>
        public void AddPort(IPort port)
        {
            _ports.AddPort(port);
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
        /// Unregisters a port from this IPortOwner.
        /// </summary>
        /// <param name="port">The port that will be removed.</param>
		public void RemovePort(IPort port)
        {
            _ports.RemovePort(port);
        }
        /// <summary>
        /// Unregisters all ports that this IPortOwner knows to be its own.
        /// </summary>
        public void ClearPorts()
        {
            _ports.ClearPorts();
        }
        /// <summary>
        /// The public property that is the PortSet this IPortOwner owns.
        /// </summary>
        public IPortSet Ports
        {
            get
            {
                return _ports;
            }
        }
        #endregion

        #region Implementation of IModelObject
        private string _name = null;
        public string Name
        {
            [DebuggerStepThrough]
            get
            {
                return _name;
            }
        }
        private Guid _guid = Guid.Empty;
        public Guid Guid
        {
            [DebuggerStepThrough]
            get
            {
                return _guid;
            }
        }
        private IModel _model;
        public IModel Model
        {
            [DebuggerStepThrough]
            get
            {
                return _model;
            }
        }
        private string _description;
        /// <summary>
        /// The description for this object. Typically used for human-readable representations.
        /// </summary>
        /// <value>The object's description.</value>
		public string Description
        {
            [DebuggerStepThrough]
            get
            {
                return (_description ?? "No description for " + _name);
            }
        }

        /// <summary>
        /// Initializes the fields that feed the properties of this IModelObject identity.
        /// </summary>
        /// <param name="model">The IModelObject's new model value.</param>
        /// <param name="name">The IModelObject's new name value.</param>
        /// <param name="description">The IModelObject's new description value.</param>
        /// <param name="guid">The IModelObject's new GUID value.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid)
        {
            IMOHelper.Initialize(ref _model, model, ref _name, name, ref _description, description, ref _guid, guid);
        }
        #endregion

    }
}