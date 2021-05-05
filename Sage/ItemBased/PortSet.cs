/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Highpoint.Sage.ItemBased.Ports
{

    //TODO: We cast input & output ports as SimpleIn & SimpleOuts to get port control. This is not optimal. Need IInputPortOwnerController and IOutputPortOwnerController.

    /// <summary>
    /// This delegate receives the object passed in on a port and the set of choices to which it can be
    /// passed.
    /// </summary>
    public delegate IPort PortSelector(object data, IPortSet portSet);

    /// <summary>
    /// This is implemented by a method that will be paying attention to
    /// a port. PortData events include those occurring when data is 
    /// presented to a port, accepted by a port, or rejected by a port.
    /// </summary>
    public delegate void PortDataEvent(object data, IPort where);

    /// <summary>
    /// This is the signature of a listener to a port. PortEvents are
    /// fired when data becomes available on a port, when a port has just
    /// been pulled from or pushed to, or when someone has tried to pull
    /// from an empty port.
    /// </summary>
    public delegate void PortEvent(IPort port);

    /// <summary>
    ///  Implemented by a method designed to respond to the arrival of data
    ///  on a port.
    /// </summary>
    public delegate bool DataArrivalHandler(object data, IInputPort port);
    /// <summary>
    /// Implemented by a method designed to provide data on an external
    /// entity's requesting it from a port.
    /// </summary>
    public delegate object DataProvisionHandler(IOutputPort port, object selector);

    /// <summary>
    /// Contains and provides IPort objects based on keys. PortOwner objects (those
    /// which implement IPortOwner) will typically (though not necessarily) contain one
    /// of these.
    /// </summary>
    public class PortSet : IPortSet, IXmlPersistable
    {

        #region Private fields

        private IComparer<IPort> _sortOrderComparer = null;
        private List<IPort> _sortedPorts = null;

        private Hashtable _ports;
        private readonly ArrayList _presentedListeners;
        private readonly ArrayList _acceptedListeners;
        private readonly ArrayList _rejectedListeners;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="PortSet"/> class.
        /// </summary>
        /// <param name="useCaseInsensitiveKeys">if set to <c>true</c> the portSet will use case insensitive keys.</param>
        public PortSet(bool useCaseInsensitiveKeys)
        {
            if (useCaseInsensitiveKeys)
            {
                _ports = new Hashtable();
            }
            else
            {
                _ports = new Hashtable(StringComparer.CurrentCultureIgnoreCase);
            }
            _presentedListeners = new ArrayList();
            _acceptedListeners = new ArrayList();
            _rejectedListeners = new ArrayList();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PortSet"/> class with case-sensitive keys.
        /// </summary>
        public PortSet() : this(false) { }

        /// <summary>
        /// Adds a port to this object's port set.
        /// </summary>
        /// <param name="port">The port to be added to the portSet.</param>
        public void AddPort(IPort port)
        {

            if (_ports.ContainsValue(port))
            {
                return; // This PO already owns the port.
            }

            if (this[port.Name] == null)
            {
                int ndx = 0;
                if (port.Index == GenericPort.UnassignedIndex)
                {
                    foreach (IPort p in _ports.Values)
                        ndx = Math.Max(ndx, p.Index);
                    port.Index = ndx + 1;
                }
                _ports.Add(port.Guid, port);
                SortedPorts = null;
                foreach (PortDataEvent dce in _presentedListeners)
                    port.PortDataPresented += dce;
                foreach (PortDataEvent dce in _acceptedListeners)
                    port.PortDataAccepted += dce;
                foreach (PortDataEvent dce in _presentedListeners)
                    port.PortDataRejected += dce;
                if (_bcmListeners != null)
                    foreach (PortEvent pe in _bcmListeners)
                        port.BeforeConnectionMade += pe;
                if (_bcbListeners != null)
                    foreach (PortEvent pe in _bcbListeners)
                        port.BeforeConnectionBroken += pe;
                if (_acmListeners != null)
                    foreach (PortEvent pe in _acmListeners)
                        port.AfterConnectionMade += pe;
                if (_acbListeners != null)
                    foreach (PortEvent pe in _acbListeners)
                        port.AfterConnectionBroken += pe;

                if (PortAdded != null)
                {
                    PortAdded(port);
                }

            }
            else
            {
                string msg = string.Format("Caller attempting to add a second port with name {0} to {1}.", port.Name, port.Owner);
                throw new ApplicationException(msg);
            }
        }

        /// <summary>
        /// Removes a port from an object's portset. Any entity having references
        /// to the port may still use it, though this may be wrong from an application
        /// perspective.
        /// </summary>
        /// <param name="port">The port to be removed from the portSet.</param>
        public void RemovePort(IPort port)
        {

            if (!_ports.ContainsValue(port))
            {
                return; // This PO does not own the port.
            }

            _ports.Remove(port.Guid);
            SortedPorts = null;
            foreach (PortDataEvent dce in _presentedListeners)
                port.PortDataPresented -= dce;
            foreach (PortDataEvent dce in _acceptedListeners)
                port.PortDataAccepted -= dce;
            foreach (PortDataEvent dce in _presentedListeners)
                port.PortDataRejected -= dce;
            if (_bcmListeners != null)
                foreach (PortEvent pe in _bcmListeners)
                    port.BeforeConnectionMade -= pe;
            if (_bcbListeners != null)
                foreach (PortEvent pe in _bcbListeners)
                    port.BeforeConnectionBroken -= pe;
            if (_acmListeners != null)
                foreach (PortEvent pe in _acmListeners)
                    port.AfterConnectionMade -= pe;
            if (_acbListeners != null)
                foreach (PortEvent pe in _acbListeners)
                    port.AfterConnectionBroken -= pe;

            if (PortRemoved != null)
            {
                PortRemoved(port);
            }

        }

        /// <summary>
        /// Fired when a port has been added to this IPortSet.
        /// </summary>
        public event PortEvent PortAdded;

        /// <summary>
        /// Fired when a port has been removed from this IPortSet.
        /// </summary>
        public event PortEvent PortRemoved;


        /// <summary>
        /// Unregisters all ports.
        /// </summary>
        public void ClearPorts()
        {
            ArrayList ports = new ArrayList(_ports.Values);
            foreach (IPort port in ports)
            {
                RemovePort(port);
            }
        }

        /// <summary>
        /// This event is fired when data is presented to any input port in this
        /// PortSet from outside, or to any output port from inside.
        /// </summary>
        public event PortDataEvent PortDataPresented
        {
            add
            {
                _presentedListeners.Add(value);
                foreach (IPort port in _ports)
                    port.PortDataPresented += value;
            }
            remove
            {
                _presentedListeners.Remove(value);
                foreach (IPort port in _ports)
                    port.PortDataPresented -= value;
            }
        }

        /// <summary>
        /// This event is fired whenever any input port accepts data presented to it
        /// from outside or any output port accepts data presented to it from inside. 
        /// </summary>
        public event PortDataEvent PortDataAccepted
        {
            add
            {
                _acceptedListeners.Add(value);
                foreach (IPort port in _ports)
                    port.PortDataAccepted += value;
            }
            remove
            {
                _acceptedListeners.Remove(value);
                foreach (IPort port in _ports)
                    port.PortDataAccepted -= value;
            }
        }

        /// <summary>
        /// This event is fired whenever an input port rejects data that is presented
        /// to it from outside or an output port rejects data that is presented to it
        /// from inside.
        /// </summary>
        public event PortDataEvent PortDataRejected
        {
            add
            {
                _rejectedListeners.Add(value);
                foreach (IPort port in _ports)
                    port.PortDataRejected += value;
            }
            remove
            {
                _rejectedListeners.Remove(value);
                foreach (IPort port in _ports)
                    port.PortDataRejected -= value;
            }
        }

        #region Port Made/Broken Event Management
        private ArrayList _bcmListeners, _acmListeners, _bcbListeners, _acbListeners;
        /// <summary>
        /// This event fires immediately before the port's connector property becomes non-null.
        /// </summary>
        public event PortEvent BeforeConnectionMade
        {
            add
            {
                if (_bcmListeners == null)
                    _bcmListeners = new ArrayList();
                _bcmListeners.Add(value);
                foreach (IPort port in _ports)
                    port.BeforeConnectionMade += value;
            }
            remove
            {
                _bcmListeners.Remove(value);
                foreach (IPort port in _ports)
                    port.BeforeConnectionMade -= value;
            }
        }

        /// <summary>
        /// This event fires immediately after the port's connector property becomes non-null.
        /// </summary>
        public event PortEvent AfterConnectionMade
        {
            add
            {
                if (_acmListeners == null)
                    _acmListeners = new ArrayList();
                _acmListeners.Add(value);
                foreach (IPort port in _ports)
                    port.AfterConnectionMade += value;
            }
            remove
            {
                _acmListeners.Remove(value);
                foreach (IPort port in _ports)
                    port.AfterConnectionMade -= value;
            }
        }


        /// <summary>
        /// This event fires immediately before the port's connector property becomes null.
        /// </summary>
        public event PortEvent BeforeConnectionBroken
        {
            add
            {
                if (_bcbListeners == null)
                    _bcbListeners = new ArrayList();
                _bcbListeners.Add(value);
                foreach (IPort port in _ports)
                    port.BeforeConnectionBroken += value;
            }
            remove
            {
                _bcbListeners.Remove(value);
                foreach (IPort port in _ports)
                    port.BeforeConnectionBroken -= value;
            }
        }

        /// <summary>
        /// This event fires immediately after the port's connector property becomes null.
        /// </summary>
        public event PortEvent AfterConnectionBroken
        {
            add
            {
                if (_acbListeners == null)
                    _acbListeners = new ArrayList();
                _acbListeners.Add(value);
                foreach (IPort port in _ports)
                    port.AfterConnectionBroken += value;
            }
            remove
            {
                _acbListeners.Remove(value);
                foreach (IPort port in _ports)
                    port.AfterConnectionBroken -= value;
            }
        }
        #endregion

        /// <summary>
        /// Returns a collection of the keys that belong to ports known to this PortSet.
        /// </summary>
        public ICollection PortKeys
        {
            get
            {
                return _ports.Keys;
            }
        }

        /// <summary>
        /// Looks up the key associated with a particular port.
        /// </summary>
        /// <param name="port">The port for which we want the key.</param>
        /// <returns>The key for the provided port.</returns>
        [Obsolete("Ports use their Guids as the key.")]
        public Guid GetKey(IPort port)
        {
            return port.Guid;
        }

        /// <summary>
        /// Gets the count of all kinds of ports in this collection.
        /// </summary>
        /// <value>The count.</value>
        public int Count
        {
            [DebuggerStepThrough]
            get
            {
                return _ports.Count;
            }
        }

        /// <summary>
        /// Returns the port associated with the provided key.
        /// </summary>
        public IPort this[Guid key]
        {
            [DebuggerStepThrough]
            get
            {
                return (IPort)_ports[key];
            }
        }

        /// <summary>
        /// Gets the <see cref="Highpoint.Sage.ItemBased.Ports.IPort"/> with the specified index, i.
        /// </summary>
        /// <value>The <see cref="Highpoint.Sage.ItemBased.Ports.IPort"/>.</value>
        public IPort this[int i]
        {
            [DebuggerStepThrough]
            get
            {
                return SortedPorts[i];
            }
        }

        /// <summary>
        /// Returns the port associated with the provided name.
        /// </summary>
        public IPort this[string name]
        {
            get
            {
                foreach (IPort port in _ports.Values)
                {
                    if (port.Name.Equals(name, StringComparison.Ordinal))
                    {
                        return port;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Provides an enumerator over the IPort instances.
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator()
        {
            return SortedPorts.GetEnumerator();
        }

        /// <summary>
        /// Gets the output ports owned by this PortSet.
        /// </summary>
        /// <value>The output ports.</value>
        public ReadOnlyCollection<IOutputPort> Outputs
        {
            get
            {
                List<IOutputPort> outs = new List<IOutputPort>();
                SortedPorts.ForEach(delegate (IPort port)
                {
                    if (port is IOutputPort)
                        outs.Add((IOutputPort)port);
                });
                return outs.AsReadOnly();
            }
        }

        /// <summary>
        /// Gets the input ports owned by this PortSet.
        /// </summary>
        /// <value>The input ports.</value>
        public ReadOnlyCollection<IInputPort> Inputs
        {
            get
            {
                List<IInputPort> ins = new List<IInputPort>();
                SortedPorts.ForEach(delegate (IPort port)
                {
                    if (port is IInputPort)
                        ins.Add((IInputPort)port);
                });
                return ins.AsReadOnly();
            }
        }

        #region IXmlPersistable Members

        /// <summary>
        /// Stores this object to the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
        public void SerializeTo(XmlSerializationContext xmlsc)
        {
            xmlsc.StoreObject("Ports", _ports);
        }

        /// <summary>
        /// Reconstitutes this object from the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
        public void DeserializeFrom(XmlSerializationContext xmlsc)
        {
            _ports = (Hashtable)xmlsc.LoadObject("Ports");
        }

        #endregion

        /// <summary>
        /// Gets or sets the internal list of sorted ports.
        /// </summary>
        /// <value>The sorted ports.</value>
        private List<IPort> SortedPorts
        {
            get
            {
                if (_sortedPorts == null)
                {
                    _sortedPorts = new List<IPort>();
                    foreach (IPort port in _ports.Values)
                    {
                        _sortedPorts.Add(port);
                    }
                    if (_sortOrderComparer != null)
                    {
                        _sortedPorts.Sort(_sortOrderComparer);
                    }
                }
                return _sortedPorts;
            }
            set
            {
                _sortedPorts = value;
            }
        }

        /// <summary>
        /// Sorts the ports based on one element of their Out-of-band data sets.
        /// Following a return from this call, the ports will be in the order requested.
        /// The "T" parameter will usually be int, double or string, but it must
        /// represent the IComparable-implementing type of the data stored under the
        /// provided OOBDataKey.
        /// </summary>
        /// <param name="oobDataKey">The oob data key.</param>
        public void SetSortOrder<T>(object oobDataKey) where T : IComparable
        {
            _sortOrderComparer = new ByOobDataComparer<T>(oobDataKey);
        }

        class ByOobDataComparer<T> : IComparer<IPort> where T : IComparable
        {
            private object _oobDataKey;
            public ByOobDataComparer(object oobDataKey)
            {
                _oobDataKey = oobDataKey;
            }


            #region IComparer<IPort> Members

            public int Compare(IPort x, IPort y)
            {
                object obx = x.GetOutOfBandData(_oobDataKey);
                object oby = y.GetOutOfBandData(_oobDataKey);

                if (obx == null && oby == null)
                {
                    return 0;
                }

                IComparable icx = obx as IComparable;
                IComparable icy = oby as IComparable;

                if (icx == null && icy == null)
                {
                    string errMsg = string.Format("Attempt to sort port list on key {0} which is of type {1}, which does not implement IComparable and it must do so, in order to sort on it.",
                        _oobDataKey, (obx == null ? oby.GetType().FullName : obx.GetType().FullName));
                    throw new ApplicationException(errMsg);
                }



                if (icx == null)
                {
                    return -1;
                }

                if (icy == null)
                {
                    return 1;
                }

                return icx.CompareTo(icy);
            }

            #endregion
        }
    }
}
