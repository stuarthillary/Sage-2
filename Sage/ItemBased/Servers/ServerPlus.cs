/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.SimCore;
using System;
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased.Servers
{
    /// <summary>
    /// A 'server plus' is a server that can decide whether it can provide service based on some
    /// outside criteria, then do something (i.e. setup) before starting service, and something
    /// else (i.e. teardown) before completing service.
    /// </summary>
    public class ServerPlus : IServer
    {
        public IInputPort Input
        {
            get
            {
                return _input;
            }
        }
        public IOutputPort Output
        {
            get
            {
                return _output;
            }
        }

        #region >>> Private Fields <<<
        private readonly SimpleInputPort _input;
        private readonly SimpleOutputPort _output;
        private readonly bool _supportsServerObjects;
        private bool _inService = false;
        private bool _pending = false;
        private IPeriodicity _periodicity;
        #endregion

        /// <summary>
        /// Creates a new instance of the <see cref="T:ServerPlus"/> class.
        /// </summary>
        /// <param name="model">The model in which this object runs.</param>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        /// <param name="periodicity">The periodicity.</param>
        public ServerPlus(IModel model, string name, Guid guid, IPeriodicity periodicity)
        {
            InitializeIdentity(model, name, null, guid);

            _input = new SimpleInputPort(model, "Input", Guid.NewGuid(), this, new DataArrivalHandler(AcceptServiceObject));
            _output = new SimpleOutputPort(model, "Output", Guid.NewGuid(), this, null, null);

            _periodicity = periodicity;

            string sso = _model.ModelConfig.GetSimpleParameter("SupportsServerObjects");
            _supportsServerObjects = (sso == null) ? false : bool.Parse(sso);

            OnCanWeProcessServiceObject = new ServiceRequestEvent(CanWeProcessServiceObjectHandler);
            OnPreCommencementSetup = new ServiceEvent(PreCommencementSetupHandler);
            OnPreCompletionTeardown = new ServiceEvent(PreCompletionTeardownHandler);

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


        public bool SupportsServerObjects
        {
            get
            {
                return _supportsServerObjects;
            }
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
            _input.DataAvailable += new PortEvent(OnServiceObjectAvailable);
            TryToPullServiceObject();
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
            _input.DataAvailable -= new PortEvent(OnServiceObjectAvailable);
            _inService = false;
        }

        private void RemoveFromService(IExecutive exec, object userData)
        {
            RemoveFromService();
        }
        #endregion

        private void OnServiceObjectAvailable(IPort inputPort)
        {
            if (_inService)
                TryToPullServiceObject();
        }

        #region >>> Override this stuff for extended functionality <<<
        protected virtual bool RequiresAsyncEvents
        {
            get
            {
                return false;
            }
        }
        public ServiceRequestEvent OnCanWeProcessServiceObject;
        protected virtual bool CanWeProcessServiceObjectHandler(IServer server, object obj)
        {
            return true;
        }

        public ServiceEvent OnPreCommencementSetup;
        protected virtual void PreCommencementSetupHandler(IServer server, object obj)
        {
        }

        public ServiceEvent OnPreCompletionTeardown;
        protected virtual void PreCompletionTeardownHandler(IServer server, object obj)
        {
        }
        #endregion

        protected void TryToPullServiceObject()
        {
            IExecutive exec = Model.Executive;
            if (RequiresAsyncEvents && exec.CurrentEventController == null)
            {
                Model.Executive.RequestEvent(new ExecEventReceiver(tryToPullServiceObject), exec.Now, 0.0, null, ExecEventType.Detachable);
            }
            else
            {
                tryToPullServiceObject(exec, null);
            }
        }

        private void tryToPullServiceObject(IExecutive exec, object obj)
        {
            if (_input.Connector == null)
                return;
            object serviceObject = _input.OwnerPeek(null);
            if (serviceObject == null)
                return;
            lock (this)
            {
                if (_pending)
                    return;
                if (RequiresAsyncEvents)
                    _pending = true;
            }
            if (OnCanWeProcessServiceObject(this, serviceObject))
            {
                serviceObject = _input.OwnerTake(null);
                OnPreCommencementSetup(this, serviceObject);
                _pending = false;
                Process(serviceObject);
            }
        }

        private bool AcceptServiceObject(object nextServiceObject, IInputPort ip)
        {
            if (_inService)
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

            if (ServiceBeginning != null)
                ServiceBeginning(this, serviceObject);

            if (_supportsServerObjects)
            {
                IServiceObject iso = serviceObject as IServiceObject;
                if (iso != null)
                    iso.OnServiceBeginning(this);
            }

            DateTime when = _model.Executive.Now + _periodicity.GetNext();
            _model.Executive.RequestEvent(new ExecEventReceiver(CompleteProcessing), when, 0.0, serviceObject);
        }


        private void CompleteProcessing(IExecutive exec, object serviceObject)
        {

            OnPreCompletionTeardown(this, serviceObject);

            if (_supportsServerObjects)
            {
                IServiceObject iso = serviceObject as IServiceObject;
                if (iso != null)
                    iso.OnServiceCompleting(this);
            }
            if (ServiceCompleted != null)
                ServiceCompleted(this, serviceObject);
            _output.OwnerPut(serviceObject);
            if (_inService)
                TryToPullServiceObject();
        }

        #region >>> Service Events <<<
        /// <summary>
        /// Fires when the server begins servicing an object.
        /// </summary>
        public event ServiceEvent ServiceBeginning;
        /// <summary>
        /// Fires when the server completes servicing an object.
        /// </summary>
        public event ServiceEvent ServiceCompleted;
        #endregion

        #region IPortOwner Implementation
        /// <summary>
        /// The PortSet object to which this IPortOwner delegates.
        /// </summary>
        private readonly PortSet _ports = new PortSet();
        /// <summary>
        /// Registers a port with this IPortOwner
        /// </summary>
        /// <param name="port">The port that this IPortOwner will add. It is known by the Guid and name of the port.</param>
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
        /// <param name="port">The port that is to be removed.</param>
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
        /// A description of this ServerPlus.
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