/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.SimCore;
using System;
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased.Servers
{
    /// <summary>
    /// A SimpleServer is a single-channeled server that accepts one object from its input port,
    /// waits a specified timespan, and then presents that object to its output port. It does not
    /// permit its own outputs to be refused.
    /// <p></p>
    /// When a server becomes idle, it attempts to pull from its input port. If it is successful,
    /// it becomes busy for a timespan, determined by a timespan distribution after which the
    /// object is presented to its output port. Once the object at its output port is taken, the
    /// server becomes idle.
    /// If an object is presented on its input port and it is busy, it rejects the presentation
    /// by returning false. If it is not busy when the presentation is made, then it accepts 
    /// the new arrival, and commences working on it for a timespan. When the timespan expires,
    /// the object is placed on its output port. 
    /// </summary>
    public class SimpleServer : IServer
    {
        public IInputPort Input
        {
            get
            {
                return m_input;
            }
        }
        private SimpleInputPort m_input;
        public IOutputPort Output
        {
            get
            {
                return _output;
            }
        }
        private readonly SimpleOutputPort _output;

        private readonly bool _supportsServerObjects;

        private DateTime _startedService;
        private bool _available = false;
        private bool _inService = false;
        private IPeriodicity _periodicity;
        public SimpleServer(IModel model, string name, Guid guid, IPeriodicity periodicity)
        {
            InitializeIdentity(model, name, null, guid);

            m_input = new SimpleInputPort(model, "Input", Guid.NewGuid(), this, new DataArrivalHandler(AcceptServiceObject));
            _output = new SimpleOutputPort(model, "Output", Guid.NewGuid(), this, null, null);
            // AddPort(m_input); <-- Done in port's ctor.
            // AddPort(m_output); <-- Done in port's ctor.
            _periodicity = periodicity;
            m_input.DataAvailable += new PortEvent(OnServiceObjectAvailable);
            string sso = _model.ModelConfig.GetSimpleParameter("SupportsServerObjects");
            _supportsServerObjects = (sso == null) ? false : bool.Parse(sso);

            IMOHelper.RegisterWithModel(this);
        }

        #region >>> Place In Service <<<
        /// <summary>
        /// Waits until a specified time, then places the server in service. Can be done directly in code
        /// through the PlaceInService() API and an executive event with handler. 
        /// </summary>
        /// <param name="dt">The DateTime at which the server will be placed in service.</param>
        public void PlaceInServiceAt(DateTime dt)
        {
            _model.Executive.RequestEvent(new ExecEventReceiver(PlaceInService), dt, 0.0, null);
        }

        /// <summary>
        /// Places the server in service immediately. The server will try immediately to
        /// pull and service a service object from its input port.
        /// </summary>
        /// <param name="exec">The executive controlling the timebase in which this server is
        /// to operate. Typically, model.Executive.</param>
        /// <param name="userData"></param>
        private void PlaceInService(IExecutive exec, object userData)
        {
            PlaceInService();
        }

        /// <summary>
        /// Places the server in service immediately. The server will try immediately to
        /// pull and service a service object from its input port.
        /// </summary>
        public void PlaceInService()
        {
            _inService = true;
            _available = true;
            m_input.DataAvailable += new PortEvent(OnServiceObjectAvailable);
            TryToCommenceService();
        }
        #endregion

        #region >>> Remove From Service <<<
        /// <summary>
        /// Removes the server from service at a specified time. The server will complete
        /// servicing its current service item, and then accept no more items.
        /// </summary>
        /// <param name="dt">The DateTime at which this server is to be removed from service.</param>
        public void RemoveFromServiceAt(DateTime dt)
        {
            _model.Executive.RequestEvent(new ExecEventReceiver(RemoveFromService), dt, 0.0, null);
        }

        /// <summary>
        /// Removes this server from service immediately. The server will complete
        /// servicing its current service item, and then accept no more items.
        /// </summary>
        public void RemoveFromService()
        {
            _inService = false;
        }

        private void RemoveFromService(IExecutive exec, object userData)
        {
            RemoveFromService();
        }
        #endregion

        /// <summary>
        /// The periodicity of the server.
        /// </summary>
        /// <value></value>
        public IPeriodicity Periodicity
        {
            get
            {
                return _periodicity;
            }
            set
            {
                _periodicity = value;
            }
        }

        /// <summary>
        /// This method is called either when an in-process service completes, or when a new
        /// service object shows up at the entry point of an idle server.
        /// </summary>
        /// <returns>true if the service event may proceed. If an implementer returns false,
        /// it is up to that implementer to ensure that in some way, it initiates re-attempt
        /// at a later time, or this server will freeze.</returns>
        protected virtual bool PrepareToServe()
        {
            return true;
        }

        private void OnServiceObjectAvailable(IPort inputPort)
        {
            if (_inService && _available)
                TryToCommenceService();
        }

        private void TryToCommenceService()
        {
            if (m_input.Connector == null)
                return;
            object nextServiceObject = m_input.OwnerTake(null);
            if (nextServiceObject != null)
                Process(nextServiceObject);
        }

        private bool AcceptServiceObject(object nextServiceObject, IInputPort ip)
        {
            if (_inService && _available)
            {
                Process(nextServiceObject);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Process(object serviceObject)
        {
            IServiceObject iso = serviceObject as IServiceObject;

            if (ServiceBeginning != null)
                ServiceBeginning(this, serviceObject);
            if (iso != null)
                iso.OnServiceBeginning(this);
            _available = false;
            _startedService = _model.Executive.Now;
            DateTime when = _model.Executive.Now + _periodicity.GetNext();
            _model.Executive.RequestEvent(new ExecEventReceiver(CompleteProcessing), when, 0.0, serviceObject);
        }

        private void CompleteProcessing(IExecutive exec, object serviceObject)
        {
            IServiceObject iso = serviceObject as IServiceObject;
            if (iso != null)
                iso.OnServiceCompleting(this);
            if (ServiceCompleted != null)
                ServiceCompleted(this, serviceObject);
            _output.OwnerPut(serviceObject);
            _available = true;
            if (_inService)
                TryToCommenceService();
        }

        /// <summary>
        /// Fires when the server begins servicing an object.
        /// </summary>
        public event ServiceEvent ServiceBeginning;
        /// <summary>
        /// Fires when the server completes servicing an object.
        /// </summary>
        public event ServiceEvent ServiceCompleted;

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
        /// A description of this SimpleServer.
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

        #endregion

    }
}