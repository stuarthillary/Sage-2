/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.SimCore;
using System;
using System.Collections.Generic;


namespace Highpoint.Sage.ItemBased.SinksAndSources
{

    /// <summary>
    /// Implemented by a method that is intended to consume objects.
    /// </summary>
    public delegate void ObjectSink(object theObject);

    public class ItemSink : IPortOwner, IModelObject
    {

        public event ObjectSink ObjectSunk;
        private readonly SimpleInputPort _input;
        public IInputPort Input;

        public ItemSink(IModel model, string name, Guid guid)
        {
            InitializeIdentity(model, name, null, guid);

            _input = new SimpleInputPort(model, "Input", Guid.NewGuid(), this, new DataArrivalHandler(CanAcceptPushedData));
            _ports.AddPort(_input);
            Input = _input;
            _input.PortDataAccepted += new PortDataEvent(input_PortDataAccepted);

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

        /// <summary>
        /// Determines whether this instance can accept pushed data on its input port[s].
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="port">The port.</param>
        /// <returns>
        /// 	<c>true</c> if this instance can accept pushed data; otherwise, <c>false</c>.
        /// </returns>
		private bool CanAcceptPushedData(object data, IInputPort port)
        {
            return true;
        }


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
                return GeneralPortChannelInfo.StdInputOnly;
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
            get
            {
                return _name;
            }
        }
        private string _description = null;
        /// <summary>
        /// A description of this ItemSink.
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

        private void input_PortDataAccepted(object data, IPort where)
        {
            if (ObjectSunk != null)
                ObjectSunk(data);
        }
    }
}