/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.ItemBased.Connectors;
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.SimCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Highpoint.Sage.ItemBased
{

    /// <summary>
    /// items show up on input ports. If it has one, the Nexus' IPortSelector is queried
    /// for a selection of output port, and if one is provided, the item is placed on that
    /// port. If it has no IPortSelector and the serviceObject implements IPortSelector, it is
    /// asked where it would like to go next. If it answers with a selected port, it is placed on
    /// that port. Otherwise, the nexus selects an output port at random.
    /// Note that the IPortSelector in this case must always select IOutputPorts.
    /// </summary>
    public class Nexus : IPortOwner, IModelObject
    {

        #region Private Fields
        private IPortSelector _portSelector;
        private readonly DataProvisionHandler _cantTakeOrPeekFromNexus;
        private readonly DataArrivalHandler _canAlwaysAcceptData;
        private int _inCount = 0;
        private int _outCount = 0;
        #endregion

        public Nexus(IModel model, string name, Guid guid) : this(model, name, guid, null) { }

        public Nexus(IModel model, string name, Guid guid, IPortSelector portSelector)
        {
            InitializeIdentity(model, name, null, guid);

            _ports = new PortSet();
            _portSelector = portSelector;
            _cantTakeOrPeekFromNexus = new DataProvisionHandler(CantTakeOrPeekFromNexus);
            _canAlwaysAcceptData = new DataArrivalHandler(OnDataArrived);

            IMOHelper.RegisterWithModel(this);
        }

        /// <summary>
        /// Gets or sets the port selector that will be used to determine where, if an object is pushed into the nexus,
        /// it will emerge.
        /// </summary>
        /// <value>The port selector.</value>
		public IPortSelector PortSelector
        {
            get
            {
                return _portSelector;
            }
            set
            {
                _portSelector = value;
            }
        }

        /// <summary>
        /// Connects the Nexus to the specified port If the specified port is an input port, creates an output port
        /// on the nexus and adds a connector to the specified port from that output port. The relationship does not
        /// allow taking or peeking from the nexus.
        /// </summary>
        /// <param name="port">The port.</param>
		public void Bind(IPort port)
        {
            IPort myNewPort = null;
            if (port is IInputPort)
            {
                myNewPort = new SimpleOutputPort(_model, "Output" + (_outCount++), Guid.NewGuid(), this, _cantTakeOrPeekFromNexus, _cantTakeOrPeekFromNexus);
            }
            else if (port is IOutputPort)
            {
                myNewPort = new SimpleInputPort(_model, "Input" + (_inCount++), Guid.NewGuid(), this, _canAlwaysAcceptData);
                myNewPort.PortDataAccepted += new PortDataEvent(OnPortDataAccepted);
            }
            else
            {
                throw new ApplicationException("Unknown port type " + port.GetType().Name + " encountered.");
            }
            // m_ports.AddPort(myNewPort); <-- Done in port's ctor.
            ConnectorFactory.Connect(port, myNewPort);
        }


        internal virtual IOutputPort SelectNextPort(object serviceObject)
        {
            // TODO: Make this an extensible set of strategies.
            IOutputPort nextPort = null;
            if (_portSelector != null)
                nextPort = (IOutputPort)_portSelector.SelectPort(_ports);
            if (nextPort == null)
            {
                if (serviceObject is IPortSelector)
                {
                    nextPort = (IOutputPort)((IPortSelector)serviceObject).SelectPort(_ports);
                }
            }
            if (nextPort == null)
                throw new NotImplementedException("No portSelector, and service object is not an IPortSelector.");
            return nextPort;
        }

        private static bool OnDataArrived(object data, IInputPort port)
        {
            return true;
        }

        private void OnPortDataAccepted(object serviceObject, IPort port)
        {
            if (serviceObject == null)
            {
                throw new ApplicationException("Nexus was unable to take object from input port.");
            }
            if (!((SimpleOutputPort)SelectNextPort(serviceObject)).OwnerPut(serviceObject))
            {
                throw new ApplicationException("Nexus unable to pass service object to a selected port.");
            }
        }

        private static object CantTakeOrPeekFromNexus(IOutputPort port, object selector)
        {
            return null;
        }

        #region IPortOwner Implementation
        /// <summary>
        /// The PortSet object to which this IPortOwner delegates.
        /// </summary>
        private PortSet _ports = new PortSet();
        /// <summary>
        /// Registers a port with this IPortOwner
        /// </summary>
        /// <param name="port">The port being added.</param>
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
        /// <param name="port">The port being unregistered.</param>
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
        public string Description => (_description ?? ("No description for " + _name));

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