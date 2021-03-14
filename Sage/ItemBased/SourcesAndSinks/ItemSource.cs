/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.SimCore;
using System;
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased.SinksAndSources
{

    /// <summary>
    /// Implemented by a method that is intended to generate objects.
    /// </summary>
    public delegate object ObjectSource();

    public class ItemSource : IPortOwner, IModelObject
    {

        private ObjectSource _objectSource;
        private IPulseSource _pulseSource;
        private object _latestEmission = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemSource"/> class.
        /// </summary>
        /// <param name="model">The model in which this object runs.</param>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        /// <param name="objectSource">The object source.</param>
        /// <param name="pulseSource">The pulse source.</param>
        /// <param name="persistentOutput">If true, then the most recent output value will be returned on any peek or pull.</param>
        public ItemSource(IModel model, string name, Guid guid, ObjectSource objectSource, IPulseSource pulseSource, bool persistentOutput = false)
        {
            InitializeIdentity(model, name, null, guid);

            if (persistentOutput)
            {
                _output = new SimpleOutputPort(model, "Source", Guid.NewGuid(), this, new DataProvisionHandler(PersistentOutput), new DataProvisionHandler(PersistentOutput));
            }
            else
            {
                _output = new SimpleOutputPort(model, "Source", Guid.NewGuid(), this, new DataProvisionHandler(VolatileOutput), new DataProvisionHandler(VolatileOutput));
            }
            // m_ports.AddPort(m_output); <-- Done in port's ctor.
            _objectSource = objectSource;
            _pulseSource = pulseSource;
            pulseSource.PulseEvent += new PulseEvent(OnPulse);

            IMOHelper.RegisterWithModel(this);

            model.Starting += new ModelEvent(delegate (IModel theModel)
            {
                _latestEmission = null;
            });
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
        /// Gets the output port for this source.
        /// </summary>
        /// <value>The output.</value>
		public IOutputPort Output
        {
            get
            {
                return _output;
            }
        }
        private readonly SimpleOutputPort _output;
        private void OnPulse()
        {
            _latestEmission = _objectSource();
            _output.OwnerPut(_latestEmission);
        }

        /// <summary>
        /// Gets or sets the object source, the factory method for creating items from this source.
        /// </summary>
        /// <value>The object source.</value>
        public ObjectSource ObjectSource
        {
            get
            {
                return _objectSource;
            }
            set
            {
                _objectSource = value;
            }
        }

        /// <summary>
        /// Gets or sets the pulse source, the ModelObject tjat provides the cadence for creating items from this source.
        /// </summary>
        /// <value>The pulse source.</value>
        public IPulseSource PulseSource
        {
            get
            {
                return _pulseSource;
            }
            set
            {
                if (_pulseSource != null)
                {
                    _pulseSource.PulseEvent -= new PulseEvent(OnPulse);
                }

                _pulseSource = value;
                _pulseSource.PulseEvent += new PulseEvent(OnPulse);
            }
        }


        private static object VolatileOutput(IOutputPort port, object selector)
        {
            return null;
        }
        private object PersistentOutput(IOutputPort port, object selector)
        {
            return _latestEmission;
        }

        #region IPortOwner Implementation
        /// <summary>
        /// The PortSet object to which this IPortOwner delegates.
        /// </summary>
        private readonly PortSet _ports = new PortSet();
        /// <summary>
        /// Registers a port with this IPortOwner
        /// </summary>
        /// <param name="port">The port that this IPortOwner will know by this key.</param>
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
                return GeneralPortChannelInfo.StdOutputOnly;
            }
        }

        /// <summary>
        /// Unregisters a port from this IPortOwner.
        /// </summary>
        /// <param name="port">The port.</param>
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
        /// A description of this ItemSource.
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