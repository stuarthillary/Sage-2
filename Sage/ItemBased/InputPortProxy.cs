/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.ItemBased.Connectors;
using Highpoint.Sage.SimCore;
using System;
using System.Diagnostics;

namespace Highpoint.Sage.ItemBased.Ports
{
    /// <summary>
    /// Class InputPortProxy is a class that represents to an outer container
    /// the functionality of an input port on an internal port owner. This can be used
    /// to expose the externally-visible ports from a network of blocks that is
    /// being represented as one container-level block.    /// </summary>
    public class InputPortProxy : IInputPort
    {

        #region Private Fields
        private readonly IInputPort _ward;
        private readonly IPortOwner _owner;
        private IConnector _externalConnector;
        private readonly IConnector _internalConnector;
        private readonly IOutputPort _wardPartner;
        private readonly PortSet _portSet;
        private readonly IPortOwner _internalPortOwner;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="InputPortProxy"/> class.
        /// </summary>
        /// <param name="model">The model in which this <see cref="T:InputPortProxy"/> will run.</param>
        /// <param name="name">The name of the new <see cref="T:InputPortProxy"/>.</param>
        /// <param name="description">The description of the new <see cref="T:InputPortProxy"/>.</param>
        /// <param name="guid">The GUID of the new <see cref="T:InputPortProxy"/>.</param>
        /// <param name="owner">The owner of this proxy port.</param>
        /// <param name="ward">The ward - the internal port which this proxy port will represent.</param>
        public InputPortProxy(IModel model, string name, string description, Guid guid, IPortOwner owner, IInputPort ward)
        {
            IMOHelper.Initialize(ref _model, model, ref _name, name, ref _description, description, ref _guid, guid);

            _portSet = new PortSet();
            _internalPortOwner = new PortOwnerProxy(name + ".Internal");
            _wardPartner = new SimpleOutputPort(model, name + ".WardPartner", Guid.NewGuid(), _internalPortOwner, new DataProvisionHandler(takeHandler), new DataProvisionHandler(peekHandler));
            _ward = ward;
            _internalConnector = new BasicNonBufferedConnector(_wardPartner, _ward);
            _owner = owner;
            _externalConnector = null;

            IMOHelper.RegisterWithModel(this);
        }

        private object takeHandler(IOutputPort from, object selector)
        {
            if (_externalConnector != null)
            {
                return _externalConnector.Take(selector);
            }
            else
            {
                return false;
            }
        }

        private object peekHandler(IOutputPort from, object selector)
        {
            if (_externalConnector != null)
            {
                return _externalConnector.Peek(selector);
            }
            else
            {
                return false;
            }
        }

        #region IInputPort Members

        /// <summary>
        /// This method attempts to place the provided data object onto the port from
        /// upstream of its owner. It will succeed if the port is unoccupied, or if
        /// the port is occupied and the port permits overwrites.
        /// </summary>
        /// <param name="obj">the data object</param>
        /// <returns>True if successful. False if it fails.</returns>
        public bool Put(object obj)
        {
            if (_internalConnector != null)
            {
                return _internalConnector.Put(obj);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// This is called by a peer to let the input port know that there is data
        /// available at the peer, in case the input port wants to pull the data.
        /// </summary>
        public void NotifyDataAvailable()
        {
            if (_internalConnector != null)
            {
                _internalConnector.NotifyDataAvailable();
            }
        }

        /// <summary>
        /// This sets the DataArrivalHandler that this port will use, replacing the current
        /// one. This should be used only by objects under the control of, or owned by, the
        /// IPortOwner that owns this port.
        /// </summary>
        /// <value>The new dataArrivalHandler.</value>
        public DataArrivalHandler PutHandler
        {
            get
            {
                return _ward.PutHandler;
            }
            set
            {
                _ward.PutHandler = value;
            }
        }

        #endregion

        #region IPort Members

        /// <summary>
        /// This property represents the connector object that this port is associated with.
        /// </summary>
        /// <value></value>
        public IConnector Connector
        {
            get
            {
                return _externalConnector;
            }
            set
            {
                _externalConnector = value;
                if (value == null)
                {
                    _ward.Connector = null;
                }
                else
                {
                    _ward.Connector = _internalConnector;
                }
            }
        }

        /// <summary>
        /// This property contains the owner of the port.
        /// </summary>
        /// <value></value>
        public IPortOwner Owner
        {
            get
            {
                return _owner;
            }
        }

        /// <summary>
        /// Returns the key by which this port is known to its owner.
        /// </summary>
        /// <value></value>
        public Guid Key
        {
            get
            {
                return Guid;
            }
        }

        /// <summary>
        /// This property returns the port at the other end of the connector to which this
        /// port is connected, or null, if there is no connector, and/or no port on the
        /// other end of a connected connector.
        /// </summary>
        /// <value></value>
        public IPort Peer
        {
            get
            {
                return Connector.Upstream;
            }
        }

        /// <summary>
        /// Returns the default out-of-band data from this port. Out-of-band data
        /// is data that is not material that is to be transferred out of, or into,
        /// this port, but rather context, type, or other metadata to the transfer
        /// itself.
        /// </summary>
        /// <returns>
        /// The default out-of-band data from this port.
        /// </returns>
        public object GetOutOfBandData()
        {
            return _ward.GetOutOfBandData();
        }

        /// <summary>
        /// Returns out-of-band data from this port. Out-of-band data is data that is
        /// not material that is to be transferred out of, or into, this port, but
        /// rather context, type, or other metadata to the transfer itself.
        /// </summary>
        /// <param name="selector">The key of the sought metadata.</param>
        /// <returns>The desired out-of-band metadata.</returns>
        public object GetOutOfBandData(object selector)
        {
            return _ward.GetOutOfBandData(selector);
        }

        /// <summary>
        /// Gets and sets a value indicating whether this <see cref="IPort"/> is intrinsic. An intrinsic
        /// port is a hard-wired part of its owner. It is there when its owner is created, and
        /// cannot be removed.
        /// </summary>
        /// <value><c>true</c> if intrinsic; otherwise, <c>false</c>.</value>
        public bool Intrinsic
        {
            get
            {
                return _ward.Intrinsic;
            }
        }

        public void DetachHandlers()
        {
            IInputPort iip = _internalConnector.Downstream;
            IPortOwner ipo = iip.Owner;
            _internalConnector.Disconnect();
            ipo.RemovePort(iip);
            iip.DetachHandlers();
        }

        /// <summary>
        /// The port index represents its sequence, if any, with respect to the other ports.
        /// </summary>
        public int Index
        {
            get
            {
                return _ward.Index;
            }
            set
            {
                _ward.Index = value;
            }
        }

        #endregion

        #region IPortEvents Members

        public event PortDataEvent PortDataPresented
        {
            add
            {
                _ward.PortDataPresented += value;
            }
            remove
            {
                _ward.PortDataPresented -= value;
            }
        }

        public event PortDataEvent PortDataAccepted
        {
            add
            {
                _ward.PortDataAccepted += value;
            }
            remove
            {
                _ward.PortDataAccepted -= value;
            }
        }

        public event PortDataEvent PortDataRejected
        {
            add
            {
                _ward.PortDataRejected += value;
            }
            remove
            {
                _ward.PortDataRejected -= value;
            }
        }

        public event PortEvent BeforeConnectionMade
        {
            add
            {
                _ward.BeforeConnectionMade += value;
            }
            remove
            {
                _ward.BeforeConnectionMade -= value;
            }
        }

        public event PortEvent AfterConnectionMade
        {
            add
            {
                _ward.AfterConnectionMade += value;
            }
            remove
            {
                _ward.AfterConnectionMade -= value;
            }
        }

        public event PortEvent BeforeConnectionBroken
        {
            add
            {
                _ward.BeforeConnectionBroken += value;
            }
            remove
            {
                _ward.BeforeConnectionBroken -= value;
            }
        }

        public event PortEvent AfterConnectionBroken
        {
            add
            {
                _ward.AfterConnectionBroken += value;
            }
            remove
            {
                _ward.AfterConnectionBroken -= value;
            }
        }


        #endregion

        #region Implementation of IModelObject
        private string _name = null;
        private Guid _guid = Guid.Empty;
        private IModel _model;
        private string _description = null;

        /// <summary>
        /// The IModel to which this object belongs.
        /// </summary>
        /// <value>The object's Model.</value>
        public IModel Model
        {
            [DebuggerStepThrough]
            get
            {
                return _model;
            }
        }

        /// <summary>
        /// The name by which this object is known. Typically not required to be unique in a pan-model context.
        /// </summary>
        /// <value>The object's name.</value>
        public string Name
        {
            [DebuggerStepThrough]
            get
            {
                return _name;
            }
        }

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
        /// The Guid for this object. Typically required to be unique in a pan-model context.
        /// </summary>
        /// <value>The object's Guid.</value>
        public Guid Guid
        {
            [DebuggerStepThrough]
            get
            {
                return _guid;
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
