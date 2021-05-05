/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.ItemBased.Connectors;
using Highpoint.Sage.SimCore;
using System;
using System.Collections;
using System.Diagnostics;

namespace Highpoint.Sage.ItemBased.Ports
{
    /// <summary>
    /// Base class implementation for ports.
    /// </summary>
    public abstract class GenericPort : IPort
    {

        #region Private Fields
        private readonly IPortOwner _owner;
        private IConnector _connector;
        private int _makeBreakListeners = 0;
        private bool _intrinsic = false;
        private object _defaultOutOfBandData;
        private Hashtable _outOfBandData;
        private int _portIndex = UnassignedIndex;
        #endregion

        protected bool HasBeenDetached = false;

        /// <summary>
        /// Creates a port with a given owner. It is the responsibility of the creator to add the port to the
        /// owner's PortSet.
        /// </summary>
        /// <param name="model">The model in which the port exists.</param>
        /// <param name="name">The name of the port.</param>
        /// <param name="guid">The GUIDof the port.</param>
        /// <param name="owner">The IPortOwner that will own this port.</param>
        public GenericPort(IModel model, string name, Guid guid, IPortOwner owner)
        {
            if (string.IsNullOrEmpty(name) && owner != null)
            {
                name = GetNextName(owner);
            }
            InitializeIdentity(model, name, null, guid);

            _owner = owner;
            // The following was removed 20070322 when as a result of AddPort API additions, it was seen that
            // an AddPort resulted in creation of a Port, and subequent callback into another AddPort API. This
            // was duplicitous and ambiguous. Henceforth, 
            //if (m_owner != null) {
            //    m_owner.AddPort(this);
            //}
            if (_owner != null && _owner.Ports[guid] == null)
            {
                owner.AddPort(this);
            }

            IMOHelper.RegisterWithModel(this);
        }

        /// <summary>
        /// Detaches any data arrival, peek, push, etc. handlers.
        /// </summary>
        public abstract void DetachHandlers();

        /// <summary>
        /// Gets the next port name for the specified portOwner. If has (Input_0, Input_3 and Input_9) next is Input_10.
        /// </summary>
        /// <param name="owner">The prospective new IPortOwner for the port in question.</param>
        /// <returns></returns>
        private string GetNextName(IPortOwner owner)
        {
            int i = 0;
            foreach (IPort port in owner.Ports)
            {
                if (port.Name.StartsWith(PortPrefix, StringComparison.Ordinal))
                {
                    int tmp = 0;
                    if (int.TryParse(port.Name.Substring(PortPrefix.Length), out tmp))
                    {
                        i = Math.Max(i, tmp);
                    }
                }
            }
            return PortPrefix + i;
        }

        /// <summary>
        /// Gets the default naming prefix for all ports of this type.
        /// </summary>
        /// <value>The port prefix.</value>
        protected abstract string PortPrefix
        {
            get;
        }

        /// <summary>
        /// The port index represents its sequence, if any, with respect to the other ports.
        /// </summary>
        public int Index
        {
            get
            {
                return _portIndex;
            }
            set
            {
                _portIndex = value;
            }
        }

        /// <summary>
        /// The connector, if any, to which this port is attached. If there is already a connector,
        /// then the setter is allowed to set Connector to &lt;null&gt;. Thereafter, the setter will
        /// be permitted to set the connector to a new value. This is to prevent accidentally
        /// overwriting a connection in code.
        /// </summary>
        public IConnector Connector
        {
            [DebuggerStepThrough]
            get
            {
                return _connector;
            }
            set
            {
                // We want to prevent overwriting an existing connection.
                if (_connector != null && value != null)
                {
                    if (_connector.Equals(value))
                        return;
                    #region Create an informative text message.
                    string myKey = Key.ToString();
                    string myOwner = PossibleIHasIdentityAsString(_owner);
                    string peerKey;
                    string peerOwner;
                    string newPeerKey;
                    string newPeerOwner;
                    if (this is IInputPort)
                    {
                        peerKey = Connector.Upstream == null ? "<null>" : Connector.Upstream.Key.ToString();
                        peerOwner = Connector.Upstream == null ? "<null>" : PossibleIHasIdentityAsString(Connector.Upstream.Owner);
                        newPeerKey = value.Upstream == null ? "<null>" : value.Upstream.Key.ToString();
                        newPeerOwner = value.Upstream == null ? "<null>" : PossibleIHasIdentityAsString(value.Upstream.Owner);
                    }
                    else
                    {
                        peerKey = Connector.Downstream == null ? "<null>" : Connector.Downstream.Key.ToString();
                        peerOwner = Connector.Downstream == null ? "<null>" : PossibleIHasIdentityAsString(Connector.Downstream.Owner);
                        newPeerKey = value.Downstream == null ? "<null>" : value.Downstream.Key.ToString();
                        newPeerOwner = value.Downstream == null ? "<null>" : PossibleIHasIdentityAsString(value.Downstream.Owner);
                    }

                    string errMsg;
                    try
                    {
                        errMsg = string.Format("Trying to add a connector to a port {0} on {1}, where that port is already connected "
                            + "to another port {2} on {3}. The connector being added is connected on the other end, to {4} on {5}",
                            /* my guid/port key */ myKey,
                            myOwner,
                            /* his guid/port key */ peerKey,
                            peerOwner,
                            /* new other's guid/port key */ newPeerKey,
                            newPeerOwner);
                    }
                    catch (NullReferenceException nre)
                    {
                        errMsg = "Trying to add a connector to a port that already has one.\r\n" + nre.Message;
                    }
                    #endregion
                    throw new ApplicationException(errMsg);
                }

                if (_makeBreakListeners > 0)
                {
                    if (value == null && _beforeConnectionBroken != null)
                        _beforeConnectionBroken(this);
                    if (value != null && _beforeConnectionMade != null)
                        _beforeConnectionMade(this);
                    _connector = value;
                    if (value == null && _afterConnectionBroken != null)
                        _afterConnectionBroken(this);
                    if (value != null && _afterConnectionMade != null)
                        _afterConnectionMade(this);
                }
                else
                {
                    _connector = value;
                }
            }
        }

        #region Port Made/Broken Event Management
        private event PortEvent _beforeConnectionMade;
        /// <summary>
        /// This event fires immediately before the port's connector property becomes non-null.
        /// </summary>
        public event PortEvent BeforeConnectionMade
        {
            add
            {
                _makeBreakListeners++;
                _beforeConnectionMade += value;
            }
            remove
            {
                _makeBreakListeners--;
                _beforeConnectionMade -= value;
            }
        }

        private event PortEvent _afterConnectionMade;
        /// <summary>
        /// This event fires immediately after the port's connector property becomes non-null.
        /// </summary>
        public event PortEvent AfterConnectionMade
        {
            add
            {
                _makeBreakListeners++;
                _afterConnectionMade += value;
            }
            remove
            {
                _makeBreakListeners--;
                _afterConnectionMade -= value;
            }
        }


        private event PortEvent _beforeConnectionBroken;
        /// <summary>
        /// This event fires immediately before the port's connector property becomes null.
        /// </summary>
        public event PortEvent BeforeConnectionBroken
        {
            add
            {
                _makeBreakListeners++;
                _beforeConnectionBroken += value;
            }
            remove
            {
                _makeBreakListeners--;
                _beforeConnectionBroken -= value;
            }
        }

        private event PortEvent _afterConnectionBroken;
        /// <summary>
        /// This event fires immediately after the port's connector property becomes null.
        /// </summary>
        public event PortEvent AfterConnectionBroken
        {
            add
            {
                _makeBreakListeners++;
                _afterConnectionBroken += value;
            }
            remove
            {
                _makeBreakListeners--;
                _afterConnectionBroken -= value;
            }
        }
        #endregion

        /// <summary>
        /// This port's owner.
        /// </summary>
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
        /// This event fires when data is presented on a port. For an input port, this
        /// implies presentation by an outsider, and for an output port, it implies 
        /// presentation by the port owner.
        /// </summary>
        public event PortDataEvent PortDataPresented;

        /// <summary>
        /// This event fires when data is accepted by a port. For an input port, this
        /// implies acceptance by an outsider, and for an output port, it implies 
        /// acceptance by the port owner.
        /// </summary>
        public event PortDataEvent PortDataAccepted;

        /// <summary>
        /// This event fires when data is rejected by a port. For an input port, this
        /// implies rejection by an outsider, and for an output port, it implies 
        /// rejection by the port owner.
        /// </summary>
        public event PortDataEvent PortDataRejected;

        /// <summary>
        /// Handler for arrival of data. For an output port, this will be the PortOwner
        /// presenting data to the port, for an input port, it will be the IPort's peer
        /// presenting data through the connector.
        /// </summary>
        /// <param name="data">The data being transmitted.</param>
        protected void OnPresentingData(object data)
        {
            if (PortDataPresented != null)
                PortDataPresented(data, this);
        }

        /// <summary>
        /// Handler for the acceptance of data. For an output port, this will be the port
        /// accepting data from the port owner, and for an input port, it will be the port's peer
        /// accepting data offered through the connector by this port.
        /// </summary>
        /// <param name="data">The data being transmitted.</param>
        protected void OnAcceptingData(object data)
        {
            if (PortDataAccepted != null)
                PortDataAccepted(data, this);
        }

        /// <summary>
        /// Handler for the acceptance of data. For an output port, this will be the port
        /// accepting data from the port owner, and for an input port, it will be the port's peer
        /// accepting data offered through the connector by this port.
        /// </summary>
        /// <param name="data">The data being transmitted.</param>
        protected void OnRejectingData(object data)
        {
            if (PortDataRejected != null)
                PortDataRejected(data, this);
        }

        /// <summary>
        /// Returns the default out-of-band data from this port. Out-of-band data
        /// is data that is not material that is to be transferred out of, or into,
        /// this port, but rather context, type, or other metadata to the transfer
        /// itself.
        /// </summary>
        /// <returns>The default out-of-band data from this port.</returns>
        public object GetOutOfBandData()
        {
            return _defaultOutOfBandData;
        }

        /// <summary>
        /// Returns out-of-band data from this port. Out-of-band data is data that is
        /// not material that is to be transferred out of, or into, this port, but
        /// rather context, type, or other metadata to the transfer itself.
        /// </summary>
        /// <param name="key">The key of the sought metadata.</param>
        /// <returns></returns>
        public object GetOutOfBandData(object key)
        {
            if (_outOfBandData == null)
                return null;
            if (!_outOfBandData.ContainsKey(key))
            {
                return null;
            }
            return _outOfBandData[key];
        }

        /// <summary>
        /// Sets the default out-of-band data.
        /// </summary>
        /// <param name="defaultOobData">The default out-of-band data.</param>
        public void SetDefaultOutOfBandData(object defaultOobData)
        {
            _defaultOutOfBandData = defaultOobData;
        }

        /// <summary>
        /// Sets an out-of-band data item based on its key.
        /// </summary>
        /// <param name="key">The key through which the out-of-band data is to be returned.</param>
        /// <param name="outOfBandData">The out-of-band data associated with the above key.</param>
        public void SetOutOfBandData(object key, object outOfBandData)
        {
            if (_outOfBandData == null)
                _outOfBandData = new Hashtable();
            if (_outOfBandData.Contains(key))
            {
                _outOfBandData[key] = outOfBandData;
            }
            else
            {
                _outOfBandData.Add(key, outOfBandData);
            }
        }

        /// <summary>
        /// Returns the peer of this port. A port's peer is the port
        /// that is at the other end of the connector to which this
        /// port is attached, or null if there is no attached conenctor
        /// or if there is no port on the other end.
        /// </summary>
        public IPort Peer
        {
            get
            {
                if (_connector == null)
                    return null;
                if (_connector.Upstream == null)
                    return null;
                if (_connector.Upstream.Equals(this))
                    return _connector.Downstream;
                return _connector.Upstream;
            }
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
                return _intrinsic;
            }
            set
            {
                _intrinsic = value;
            }
        }

        /// <summary>
        /// When a port index is this value upon being added to a PortSet, that PortSet will assign a sequential index value.
        /// </summary>
        public static int UnassignedIndex = -1;

        #region Implementation of IModelObject
        private IModel _model;
        private string _name = null;
        private Guid _guid = Guid.Empty;
        private string _description = null;
        /// <summary>
        /// The user-friendly name for this object.
        /// </summary>
        /// <value></value>
        public string Name
        {
            [DebuggerStepThrough]
            get
            {
                return _name;
            }
        }
        /// <summary>
        /// The Guid for this object. Typically required to be unique.
        /// </summary>
        /// <value></value>
        public Guid Guid
        {
            [DebuggerStepThrough]
            get
            {
                return _guid;
            }
        }
        /// <summary>
        /// The model that owns this object, or from which this object gets time, etc. data.
        /// </summary>
        /// <value></value>
        public IModel Model
        {
            [DebuggerStepThrough]
            get
            {
                return _model;
            }
        }
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

        protected void DetachedPortInUse()
        {
            string errMsg = string.Format("The port {0}, owned by {1} is being used by someone, but it has been detached from its owner.",
                Name, PossibleIHasIdentityAsString(Owner));
            throw new ApplicationException(errMsg);
        }

        private string PossibleIHasIdentityAsString(object obj)
        {
            if (obj == null)
            {
                return "<null>";
            }
            else if (obj is IHasIdentity)
            {
                return ((IHasIdentity)_owner).Name;
            }
            else
            {
                return obj.GetType().ToString();
            }
        }
    }
}
