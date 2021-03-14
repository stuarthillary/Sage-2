/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.SimCore;
using System;
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased.SplittersAndJoiners
{

    public delegate bool BooleanDecider(object obj);

    /// <summary>
    /// The SimpleBranchBlock takes an object off of one input port, makes a choice from among its
    /// output ports, and sends the object to that port.
    /// </summary>
    public abstract class SimpleBranchBlock : ISplitter, IPortOwner, IModelObject
    {
        private PortSet _portSet;
        protected IInputPort input;
        public IInputPort Input
        {
            get
            {
                return input;
            }
        }
        protected IOutputPort[] outputs;
        public IOutputPort[] Outputs
        {
            get
            {
                return outputs;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleBranchBlock"/> class.
        /// </summary>
        /// <param name="model">The model in which this object runs.</param>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
		public SimpleBranchBlock(IModel model, string name, Guid guid)
        {
            InitializeIdentity(model, name, null, guid);
            _portSet = new PortSet();
            SetUpInputPort();
            SetUpOutputPorts();
            IMOHelper.RegisterWithModel(this);
        }

        /// <summary>
        /// Initialize the identity of this model object, once.
        /// </summary>
        /// <param name="model">The model this component runs in.</param>
        /// <param name="name">The name of this component.</param>
        /// <param name="description">The description for this component.</param>
        /// <param name="guid">The GUID of this component.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid)
        {
            IMOHelper.Initialize(ref _model, model, ref _name, name, ref _description, description, ref _guid, guid);
        }

        protected void SetUpInputPort()
        {
            input = new SimpleInputPort(_model, "In", Guid.NewGuid(), this, new DataArrivalHandler(OnDataArrived));
            // m_portSet.AddPort(m_input); <-- Done in port's ctor.
        }

        /// <summary>
        ///  Implemented by a method designed to respond to the arrival of data
        ///  on a port.
        /// </summary>
        private bool OnDataArrived(object data, IInputPort port)
        {
            SimpleOutputPort outport = (SimpleOutputPort)ChoosePort(data);
            if (outport == null)
                return false;
            return outport.OwnerPut(data);
        }

        protected abstract void SetUpOutputPorts();
        protected abstract IPort ChoosePort(object dataObject);

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
        /// <value></value>
		public IPortSet Ports
        {
            get
            {
                return _portSet;
            }
        }

        #endregion

        #region Sample Implementation of IModelObject
        private string _name = null;
        public string Name
        {
            get
            {
                return _name;
            }
        }
        private string _description = null;
        /// <summary>
        /// A description of this SimpleBranchBlock.
        /// </summary>
        public string Description
        {
            get
            {
                return _description ?? _name;
            }
        }
        private Guid _guid = Guid.Empty;
        public Guid Guid => _guid;
        private IModel _model;
        /// <summary>
        /// The model that owns this object, or from which this object gets time, etc. data.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model => _model;
        #endregion
    }
}
