/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using System;

namespace Highpoint.Sage.ItemBased.Ports
{
    /// <summary>
    /// A simple implementation of input port. A data arrival handler may be defined to
    /// react to data that has been pushed from its peer - if none is provided, then it 
    /// cannot accept pushed data, (i.e. can only pull data from its peer at the request
    /// of its owner.) 
    /// </summary>
    public class SimpleInputPort : GenericPort, IInputPort
    {
        /// <summary>
        /// Creates a simple input port with a specified owner and handler to be called
        /// when data arrives on the port. If the handler is null, then an internal handler
        /// is used that, in effect, refuses delivery of the data.
        /// It is the responsibility of the creator to add the port to the owner's PortSet.
        /// </summary>
        /// <param name="model">The model in which this port participates.</param>
        /// <param name="name">The name of the port. This is typically required to be unique within an owner.</param>
        /// <param name="guid">The GUID of the port - also known to the PortOwner as the port's Key.</param>
        /// <param name="owner">The IPortOwner that owns this port.</param>
        /// <param name="dah">The DataArrivalHandler that will respond to data arriving on
        /// this port having been pushed from its peer.</param>
        public SimpleInputPort(IModel model, string name, Guid guid, IPortOwner owner, DataArrivalHandler dah)
            : base(model, name, guid, owner)
        {
            if (dah != null)
            {
                _dataArrivalHandler = dah;
            }
            else
            {
                _dataArrivalHandler = new DataArrivalHandler(CantAcceptPushedData);
            }
        }

        /// <summary>
        /// This event is fired when new data is available to be taken from a port.
        /// </summary>
        public event PortEvent DataAvailable;

        private bool CantAcceptPushedData(object data, IInputPort ip)
        {
            return false;
        }
        private DataArrivalHandler _dataArrivalHandler;

        #region Implementation of IInputPort
        /// <summary>
        /// Called by this port's peer when it is pushing data to this port.
        /// </summary>
        /// <param name="newData">The data being pushed to the port from its peer.</param>
        /// <returns>true if this port is accepting the data, otherwise false.</returns>
        public bool Put(object newData)
        {
            if (HasBeenDetached)
            {
                DetachedPortInUse();
            }
            OnPresentingData(newData);
            bool b = _dataArrivalHandler(newData, this);
            if (b)
                OnAcceptingData(newData);
            else
                OnRejectingData(newData);
            return b;
        }

        /// <summary>
        /// Called by the peer output port to let the input port know that data is available
        /// on the output port, in case the input port wants to pull that data.
        /// </summary>
        public void NotifyDataAvailable()
        {
            if (HasBeenDetached)
            {
                DetachedPortInUse();
            }
            if (DataAvailable != null)
                DataAvailable(this);
        }

        /// <summary>
        /// This sets the DataArrivalHandler that this port will use, replacing the current
        /// one. This should be used only by objects under the control of, or owned by, the
        /// IPortOwner that owns this port.
        /// </summary>
        /// <value>The DataArrivalHandler.</value>
        public DataArrivalHandler PutHandler
        {
            get
            {
                return _dataArrivalHandler;
            }
            set
            {
                _dataArrivalHandler = value;
            }
        }

        #endregion

        /// <summary>
        /// Gets the default naming prefix for all ports of this type.
        /// </summary>
        /// <value>The port prefix.</value>
        protected override string PortPrefix
        {
            get
            {
                return "Input_";
            }
        }

        /// <summary>
        /// The port owner can use this API to look at, but not remove, what is on
        /// the upstream port.
        /// </summary>
        /// <param name="selector">An object that is used in the dataProvider to
        /// determine which of potentially more than one available data element is
        /// to be provided to the requestor.</param>
        /// <returns>A reference to the object, if any, that is on the upstream port.</returns>
        public object OwnerPeek(object selector)
        {
            if (HasBeenDetached)
            {
                DetachedPortInUse();
            }
            return Connector.Peek(selector);
        }
        /// <summary>
        /// The owner of an Input Port uses this to remove an object from the port.
        /// </summary>
        /// <param name="selector">An object that is used in the dataProvider to
        /// determine which of potentially more than one available data element is
        /// to be provided to the requestor.</param>
        /// <returns>The object that heretofore was on the input port.</returns>
        public object OwnerTake(object selector)
        {
            if (HasBeenDetached)
            {
                DetachedPortInUse();
            }
            object obj = Connector.Take(selector);
            if (obj != null)
            {
                OnPresentingData(obj);
                OnAcceptingData(obj);
            }
            return obj;
        }

        /// <summary>
        /// Detaches this input port's data arrival handler.
        /// </summary>
        public override void DetachHandlers()
        {
            _dataArrivalHandler = null;
        }
    }
}
