/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.Mathematics;
using Highpoint.Sage.SimCore;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased.Servers
{

    public class MultiChannelDelayServer : IPortOwner, IServer
    {

        #region >>> Private Fields <<<
        private readonly SimpleInputPort _entryPort;
        private readonly SimpleOutputPort _exitPort;
        TimeSpanDistribution _timeSpanDistribution;
        private readonly ArrayList _inService;
        private readonly int _capacity;
        private int _pending;
        private readonly ExecEventReceiver _releaseObject;
        #endregion

        /// <summary>
        /// Creates a Server that accepts service objects on its input port, and holds them for a duration
        /// specified by a TimeSpanDistribution before emitting them from its output port. It currently is
        /// designed always to be "in service."<para/>
        /// </summary>
        /// <param name="model">The model in which this buffered server will operate.</param>
        /// <param name="name">The name given to this server.</param>
        /// <param name="guid">The guid that this server will be known by.</param>
        /// <param name="timeSpanDistribution">The TimeSpanDistribution that specifies how long each object is held.</param>
        /// <param name="capacity">The capacity of this server to hold service objects (i.e. how many it can hold)</param>
        public MultiChannelDelayServer(IModel model, string name, Guid guid, TimeSpanDistribution timeSpanDistribution, int capacity)
            : this(model, name, guid)
        {

            _timeSpanDistribution = timeSpanDistribution;
            _capacity = capacity;
        }

        private MultiChannelDelayServer(IModel model, string name, Guid guid)
        {

            InitializeIdentity(model, name, null, guid);

            _entryPort = new SimpleInputPort(model, "Input", Guid.NewGuid(), this, new DataArrivalHandler(OnDataArrived));
            _exitPort = new SimpleOutputPort(model, "Output", Guid.NewGuid(), this, null, null); // No take, no peek.

            // AddPort(m_entryPort); <-- Done in port's ctor.
            // AddPort(m_exitPort); <-- Done in port's ctor.

            _releaseObject = new ExecEventReceiver(ReleaseObject);

            _inService = new ArrayList();
            _pending = 0;

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

        public TimeSpanDistribution DelayDistribution
        {
            get
            {
                return _timeSpanDistribution;
            }
            set
            {
                _timeSpanDistribution = value;
            }
        }

        // Always accept a service object. (Simplification - might not.)
        private bool OnDataPresented(object patient, IInputPort port)
        {
            bool retval = false;
            lock (this)
            {
                if (_inService.Count < (_capacity - _pending))
                    _pending++;
                retval = true;
            }
            return retval;
        }

        // Take from the entry port, and place it on the queue's input.
        private bool OnDataArrived(object data, IInputPort port)
        {
            _inService.Add(data);
            _pending--;
            if (ServiceBeginning != null)
                ServiceBeginning(this, data);
            DateTime releaseTime = _model.Executive.Now + _timeSpanDistribution.GetNext();
            _model.Executive.RequestEvent(_releaseObject, releaseTime, 0.0, data);
            return true;
        }

        private void ReleaseObject(IExecutive exec, object userData)
        {
            _inService.Remove(userData);
            if (ServiceCompleted != null)
                ServiceCompleted(this, userData);
            _exitPort.OwnerPut(userData);
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
                return GeneralPortChannelInfo.StdInputAndOutput;
            }
        }

        /// <summary>
        /// Unregisters a port from this IPortOwner.
        /// </summary>
        /// <param name="port">The port to be removed from this MultiChannelDelayServer.</param>
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
        /// A description of this BufferedServer.
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

        #region IServer Members

        public IInputPort Input
        {
            get
            {
                return _entryPort;
            }
        }

        public IOutputPort Output
        {
            get
            {
                return _exitPort;
            }
        }

        /// <summary>
        /// From class docs - It currently is designed always to be "in service."
        /// </summary>
        /// <param name="dt"></param>
        public void PlaceInServiceAt(DateTime dt)
        {
            // From class docs - It currently is designed always to be "in service."
        }

        /// <summary>
        /// From class docs - It currently is designed always to be "in service."
        /// </summary>
        public void PlaceInService()
        {
            // From class docs - It currently is designed always to be "in service."
        }

        /// <summary>
        /// From class docs - It currently is designed always to be "in service."
        /// </summary>
        /// <param name="dt"></param>
        public void RemoveFromServiceAt(DateTime dt)
        {
            // From class docs - It currently is designed always to be "in service."
        }

        /// <summary>
        /// From class docs - It currently is designed always to be "in service."
        /// </summary>
        public void RemoveFromService()
        {
            // From class docs - It currently is designed always to be "in service."
        }

        /// <summary>
        /// This server has no periodicity, but rather a TimeSpanDistribution (since it
        /// services multiple objects at the same time.)
        /// </summary>
        public IPeriodicity Periodicity
        {
            get
            {
                return null;
            }
            set
            {
            }
        }

        public event ServiceEvent ServiceBeginning;

        public event ServiceEvent ServiceCompleted;

        #endregion
    }
}