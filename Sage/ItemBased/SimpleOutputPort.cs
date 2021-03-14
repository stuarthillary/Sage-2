/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using System;

namespace Highpoint.Sage.ItemBased.Ports
{
    /// <summary>
    /// A simple implementation of output port. A data provision handler may be defined to
    /// react to a data take request from its peer - if none is provided, then it 
    /// cannot accept a data take request, (i.e. it can only provide data as a push, driven by
    /// the port owner.) A similar handler, with the same conditions, is provided for handling
    /// a 'peek' request. If no data provision handler has been provided, either request
    /// will return null. 
    /// </summary>
    public class SimpleOutputPort : GenericPort, IOutputPort
    {
        private readonly DataProvisionHandler _nullSupplier;
        private DataProvisionHandler _takeHandler;
        private DataProvisionHandler _peekHandler;
        /// <summary>
        /// Creates a simple output port.
        /// It is the responsibility of the creator to add the port to the owner's PortSet.
        /// </summary>
        /// <param name="model">The model in which this port participates.</param>
        /// <param name="name">The name of the port. This is typically required to be unique within an owner.</param>
        /// <param name="guid">The GUID of the port - also known to the PortOwner as the port's Key.</param>
        /// <param name="owner">The IPortOwner that will own this port.</param>
        /// <param name="takeHandler">The delegate that will be called when a peer calls 'Take()'. Null is okay.</param>
        /// <param name="peekHandler">The delegate that will be called when a peer calls 'Peek()'. Null is okay.</param>
        public SimpleOutputPort(IModel model, string name, Guid guid, IPortOwner owner, DataProvisionHandler takeHandler, DataProvisionHandler peekHandler)
            : base(model, name, guid, owner)
        {
            _nullSupplier = new DataProvisionHandler(SupplyNullData);
            _takeHandler = takeHandler ?? _nullSupplier;
            _peekHandler = peekHandler ?? _nullSupplier;
        }

        private object SupplyNullData(IOutputPort op, object selector)
        {
            return null;
        }

        /// <summary>
        /// Gets the default naming prefix for all ports of this type.
        /// </summary>
        /// <value>The port prefix.</value>
        protected override string PortPrefix
        {
            get
            {
                return "Output_";
            }
        }

        #region Implementation of IOutputPort

        /// <summary>
        /// This method removes and returns the current contents of the port.
        /// </summary>
        /// <param name="selector">An object that is used in the dataProvider to
        /// determine which of potentially more than one available data element is
        /// to be provided to the requestor.</param>
        /// <returns>The current contents of the port.</returns>
        public object Take(object selector)
        {
            object data = _takeHandler(this, selector);
            if (data != null)
            {
                OnPresentingData(data);
                OnAcceptingData(data);
            }
            return data;
        }

        /// <summary>
        /// True if Peek can be expected to return meaningful data.
        /// </summary>
        public bool IsPeekable
        {
            get
            {
                return _peekHandler != _nullSupplier;
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
        /// <returns>
        /// The current contents of this port. Null if this port is not peekable.
        /// </returns>
        public object Peek(object selector)
        {
            return _peekHandler(this, selector);
        }

        /// <summary>
        /// This event is fired when new data is available to be taken from a port.
        /// </summary>
        public event PortEvent DataAvailable;

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
                return _takeHandler;
            }
            set
            {
                _takeHandler = value;
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
                return _peekHandler;
            }
            set
            {
                _peekHandler = value;
            }
        }

        #endregion

        /// <summary>
        /// Called by the port owner to put data on the port.
        /// </summary>
        /// <param name="newData">The object that is new data to be placed on the port.</param>
        /// <returns>True if the port was able to accept the data.</returns>
        public bool OwnerPut(object newData)
        {
            if (HasBeenDetached)
                DetachedPortInUse();
            OnPresentingData(newData); // Fires the PortDataPresented event. No return value.
            bool b = false;
            if (Connector != null)
            {
                // If we have a connector, present the data to it. Otherwise, just fire the rejection event.
                b = Connector.Put(newData);
            }
            if (b)
                OnAcceptingData(newData);
            else
                OnRejectingData(newData);
            return b;
        }

        /// <summary>
        /// This method is called when a Port Owner passively provides data objects - that is, it has
        /// a port on which it makes data available, but it expects others to pull from that port,
        /// rather than it pushing data to the port's peers. So, for example, a queue might call this
        /// method (a) when it is ready to discharge an object from the queue to an output port, or
        /// (b) immediately following an object being pulled from the output port, if there is another
        /// waiting right behind it.
        /// </summary>
        public void NotifyDataAvailable()
        {
            if (HasBeenDetached)
                DetachedPortInUse();
            if (DataAvailable != null)
                DataAvailable(this);
            if (Connector != null)
                Connector.NotifyDataAvailable();
        }

        /// <summary>
        /// Detaches this input port's data arrival handler.
        /// </summary>
        public override void DetachHandlers()
        {
            _takeHandler = null;
            _peekHandler = null;
            HasBeenDetached = true;
        }
    }
}
