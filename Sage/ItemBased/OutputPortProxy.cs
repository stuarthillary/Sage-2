/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.ItemBased.Connectors;
using Highpoint.Sage.SimCore;
using System;
using System.Diagnostics;

namespace Highpoint.Sage.ItemBased.Ports
{
    /// <summary>
    /// Class OutputPortProxy is a class that represents to an outer container
    /// the functionality of a port on an internal port owner. This can be used
    /// to expose the externally-visible ports from a network of blocks that is
    /// being represented as one container-level block. 
    /// </summary>
    /// <seealso cref="Highpoint.Sage.ItemBased.Ports.IOutputPort" />
    public class OutputPortProxy : IOutputPort
    {

        #region Private Fields
 
        private readonly IOutputPort _ward;
        private readonly IPortOwner _owner;
        private IConnector _externalConnector;
        private readonly IConnector _internalConnector;
        private readonly IInputPort _wardPartner;
        private readonly PortSet _portSet;
        private readonly IPortOwner _internalPortOwner;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputPortProxy" /> class.
        /// </summary>
        /// <param name="model">The model in which this <see cref="T:OutputPortProxy" /> will run.</param>
        /// <param name="name">The name of the new <see cref="T:OutputPortProxy" />.</param>
        /// <param name="description">The description of the new <see cref="T:OutputPortProxy" />.</param>
        /// <param name="guid">The GUID of the new <see cref="T:OutputPortProxy" />.</param>
        /// <param name="owner">The owner of this proxy port.</param>
        /// <param name="ward">The ward - the internal port which this proxy port will represent.</param>
        public OutputPortProxy(IModel model, string name, string description, Guid guid, IPortOwner owner, IOutputPort ward)
        {
            IMOHelper.Initialize(ref _model, model, ref _name, name, ref _description, description, ref _guid, guid);

            _portSet = new PortSet();
            _internalPortOwner = new PortOwnerProxy(name + ".PortOwner");
            _wardPartner = new SimpleInputPort(model, name + ".WardPartner", Guid.NewGuid(), _internalPortOwner, new DataArrivalHandler(dataArrivalHandler));
            _ward = ward;
            _internalConnector = new BasicNonBufferedConnector(_ward, _wardPartner);
            _owner = owner;
            _externalConnector = null;

            IMOHelper.RegisterWithModel(this);
        }

        /// <summary>
        /// Datas the arrival handler.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="inputPort">The input port.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool dataArrivalHandler(object data, IInputPort inputPort)
        {
            if (_externalConnector != null)
            {
                return _externalConnector.Put(data);
            }
            else
            {
                return false;
            }
        }

        #region IOutputPort Members

        /// <summary>
        /// This method removes and returns the current contents of the port.
        /// </summary>
        /// <param name="selector">An object that is used in the dataProvider to
        /// determine which of potentially more than one available data element is
        /// to be provided to the requestor.</param>
        /// <returns>The current contents of the port.</returns>
        public object Take(object selector)
        {
            return _ward.Take(selector);
        }

        /// <summary>
        /// True if Peek can be expected to return meaningful data.
        /// </summary>
        /// <value><c>true</c> if this instance is peekable; otherwise, <c>false</c>.</value>
        public bool IsPeekable
        {
            get
            {
                return _ward.IsPeekable;
            }
        }

        /// <summary>
        /// Nonconsumptively returns the contents of this port. A subsequent Take
        /// may or may not produce the same object, if, for example, the stuff
        /// produced from this port is time-sensitive.
        /// </summary>
        /// <param name="selector">An object that is used in the dataProvider to
        /// determine which of potentially more than one available data element is
        /// to be provided to the requestor.</param>
        /// <returns>The current contents of this port. Null if this port is not peekable.</returns>
        public object Peek(object selector)
        {
            return _ward.Peek(selector);
        }

        /// <summary>
        /// This event fires when data has been made available on this port.
        /// </summary>
        public event PortEvent DataAvailable
        {
            add
            {
                _ward.DataAvailable += value;
            }
            remove
            {
                _ward.DataAvailable -= value;
            }
        }

        /// <summary>
        /// This sets the DataProvisionHandler that this port will use to handle requests
        /// to take data from this port, replacing the current one. This should be used
        /// only by objects under the control of, or owned by, the IPortOwner that owns
        /// this port.
        /// </summary>
        /// <value>The take handler.</value>
        public DataProvisionHandler TakeHandler
        {
            get
            {
                return _ward.TakeHandler;
            }
            set
            {
                _ward.TakeHandler = value;
            }
        }

        /// <summary>
        /// This sets the DataProvisionHandler that this port will use to handle requests
        /// to peek at data on this port, replacing the current one. This should be used
        /// only by objects under the control of, or owned by, the IPortOwner that owns
        /// this port.
        /// </summary>
        /// <value>The peek handler.</value>
        public DataProvisionHandler PeekHandler
        {
            get
            {
                return _ward.PeekHandler;
            }
            set
            {
                _ward.PeekHandler = value;
            }
        }

        #endregion

        #region IPort Members

        /// <summary>
        /// This property represents the connector object that this port is associated with.
        /// </summary>
        /// <value>The connector.</value>
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
        /// <value>The owner.</value>
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
        /// <value>The key.</value>
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
        /// <value>The peer.</value>
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
        /// <returns>The default out-of-band data from this port.</returns>
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
        /// Gets and sets a value indicating whether this <see cref="IPort" /> is intrinsic. An intrinsic
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

        /// <summary>
        /// Detaches this output port's data peek and take handler.
        /// </summary>
        public void DetachHandlers()
        {
            IOutputPort iop = _internalConnector.Upstream;
            IPortOwner ipo = iop.Owner;
            _internalConnector.Disconnect();
            ipo.RemovePort(iop);
            iop.DetachHandlers();
        }

        /// <summary>
        /// The port index represents its sequence, if any, with respect to the other ports.
        /// </summary>
        /// <value>The index.</value>
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

        /// <summary>
        /// This event fires when data is presented on a port. For an input port, this
        /// implies presentation by an outsider, and for an output port, it implies
        /// presentation by the port owner.
        /// </summary>
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

        /// <summary>
        /// This event fires when data is accepted by a port. For an input port, this
        /// implies acceptance by the port owner, and for an output port, it implies
        /// acceptance by an outsider.
        /// </summary>
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

        /// <summary>
        /// This event fires when data is rejected by a port. For an input port, this
        /// implies rejection by the port owner, and for an output port, it implies
        /// rejection by an outsider.
        /// </summary>
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

        /// <summary>
        /// This event fires immediately before the port's connector property becomes non-null.
        /// </summary>
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

        /// <summary>
        /// This event fires immediately after the port's connector property becomes non-null.
        /// </summary>
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

        /// <summary>
        /// This event fires immediately before the port's connector property becomes null.
        /// </summary>
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

        /// <summary>
        /// This event fires immediately after the port's connector property becomes null.
        /// </summary>
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
        public string Description => (_description ?? ("No description for " + _name));

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
