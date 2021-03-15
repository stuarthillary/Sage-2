/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Diagnostics;
using Highpoint.Sage.Persistence;
using Highpoint.Sage.SimCore;
using System;
using System.Collections;
using System.Linq;
using _Debug = System.Diagnostics.Debug;
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

namespace Highpoint.Sage.Resources
{
    /// <summary>
    /// Resource Manager is a class that provides reservation, acquisition and
    /// release services on instances that implement IResource. All operations
    /// are accomplished synchronously, meaning that a call to acquire a resource
    /// will either throw an exception indicating that the resource request can
    /// never be fulfilled from this pool, or else (1) the call is blocked until
    /// the resource can be obtained from the pool, or (2) the request will return,
    /// unfulfilled. (1) or (2) happen based on the value of the blockAwaitingAcquisition
    /// parameter in the reserve or acquire APIs.
    /// </summary>
    public class ResourceManager : IResourceManager, IEnumerable, IXmlPersistable
    {

        #region Private Fields
        private static readonly bool diagnostics = DiagnosticAids.Diagnostics("Resources");
        private readonly RscWaiterList _waiters;
        private readonly ResourceRequestAbortEvent _onResourceRequestAborting;
        private IAccessRegulator _accessRegulator;
        private ArrayList _resources;
        private IModel _model;
        #endregion

        /// <summary>
        /// This event is fired when a resource is requested from this pool.
        /// </summary>
        public event ResourceStatusEvent ResourceRequested;

        /// <summary>
        /// This event is fired when a resource is acquired from this pool.
        /// </summary>
        public event ResourceStatusEvent ResourceAcquired;

        /// <summary>
        /// This event is fired when a resource is released back into this pool.
        /// </summary>
        public event ResourceStatusEvent ResourceReleased;

        /// <summary>
        /// This event is fired when a resource is added to the available resources in this pool.
        /// </summary>
        public event ResourceManagerEvent ResourceAdded;

        /// <summary>
        /// This event is fired when a resource is removed from the available resources in this pool.
        /// </summary>
        public event ResourceManagerEvent ResourceRemoved;

        /// <summary>
        /// Creates a new resource manager.
        /// </summary>
        /// <param name="model">The model to which this resource manager belongs. It can be null.</param>
        /// <param name="name">The name of this resource manager.</param>
        /// <param name="guid">The guid by which this resource manager will be known.</param>
        /// <param name="priorityEnabled">If true, this resource manager will handle prioritized resource requests.</param>
        public ResourceManager(IModel model, string name, Guid guid, bool priorityEnabled = false)
        {
            InitializeIdentity(model, name, null, guid);

            SupportsPrioritizedRequests = priorityEnabled;
            _onResourceRequestAborting = OnResourceRequestAborting;
            _waiters = new RscWaiterList(SupportsPrioritizedRequests);
            _resources = new ArrayList();

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

        /// <summary>
        /// Gets a value indicating whether this resource manager supports prioritized requests.
        /// </summary>
        /// <value><c>true</c> if this resource manager supports prioritized requests; otherwise, <c>false</c>.</value>
        public bool SupportsPrioritizedRequests
        {
            get;
        }

        /// <summary>
        /// Adds a resource to this resource manager.
        /// </summary>
        /// <param name="resource">The resource to be added.</param>
        public void Add(IResource resource)
        {
            if (diagnostics)
                _Debug.WriteLine("Resource manager " + Name + " added resource " + resource.Name + ". It now holds " + (_resources.Count + 1) + " resources.");
            _resources.Add(resource);
            resource.Manager = this;
            ResourceAdded?.Invoke(this, resource);
        }

        /// <summary>
        /// Removes a resource from this resource manager.
        /// </summary>
        /// <param name="resource">The resource to remove from this pool.</param>
        public void Remove(IResource resource)
        {
            if (diagnostics)
            {
                _Debug.WriteLine(Name + " removed resource " + resource);
                _Debug.WriteLine("Before removal, there are " + _resources.Count + " resources currently in  " + Name + ".");
                foreach (IResource rsc in _resources)
                    _Debug.WriteLine(rsc.Name + ", " + rsc.Guid + ", with hashcode " + rsc.GetHashCode() + "...");
                _Debug.WriteLine("You are asking to remove " + resource.Name + ", " + resource.Guid + ", with hashcode " + resource.GetHashCode() + "...");
                _Debug.WriteLine("The pool " + (_resources.Contains(resource) ? "contains" : "does not contain") + " the resource you are asking to remove.");
            }
            _resources.Remove(resource);
            if (diagnostics)
            {
                _Debug.WriteLine("After removal, there are " + _resources.Count + " resources left in  " + Name + ".");
                _Debug.WriteLine("The pool " + (_resources.Contains(resource) ? "contains" : "does not contain") + " the resource you are asking to remove.");
            }
            resource.Manager = null;
            if (ResourceRemoved != null)
            {
                if (diagnostics)
                    _Debug.WriteLine("There are listeners to the removal event.");
                ResourceRemoved(this, resource);
            }
        }

        /// <summary>
        /// Clears out the resources in this manager's pool.
        /// </summary>
        public void Clear()
        {
            if (diagnostics)
                _Debug.WriteLine(Name + " clearing its resource pool.");
            while (_resources.Count > 0)
            {
                Remove((IResource)_resources[0]);
            }
        }

        /// <summary>
        /// Retrieves an enumerator across all of the resources in this pool.
        /// </summary>
        /// <returns>An enumerator across all of the resources in this pool.</returns>
        public IEnumerator GetEnumerator()
        {
            return _resources.GetEnumerator();
        }

        /// <summary>
        /// Indexer that retrieves a resource from this pool by its Guid.
        /// </summary>
        public IResource this[Guid guid]
        {
            get
            {
                //TODO: Remove linear search, replace with Hashtable.
                return _resources.Cast<IResource>().FirstOrDefault(rsc => rsc.Guid.Equals(guid));
            }
        }

        /// <summary>
        /// Returns a read-only list of the resources in this pool.
        /// </summary>
        public IList Resources => ArrayList.ReadOnly(_resources);

        #region Implementation of IResourceManager
        /// <summary>
        /// Attempts to reserve a proscribed quantity of a particular resource in this resource pool. This
        /// removes the resource quantity from availability for further reservation &amp; acquisition.
        /// </summary>
        /// <param name="resourceRequest">The resource request under which the reservation is to take place.</param>
        /// <param name="blockAwaitingAcquisition">If true, blocks until resource is reserved.</param>
        /// <returns>true if the reservation was successful.</returns>
        public bool Reserve(IResourceRequest resourceRequest, bool blockAwaitingAcquisition)
        {
            if (diagnostics)
            {
                _Debug.WriteLine(Name + " servicing request to reserve (" + (blockAwaitingAcquisition ? "with" : "without") + " block) " + resourceRequest.QuantityDesired + " units of " + resourceRequest);
            }

            if (resourceRequest.RequiredResource != null)
            {
                _Debug.Assert(false, GetType().FullName + " does not support explicit targeting of resources.");
            }

            if (blockAwaitingAcquisition)
                return ReserveWithWait(resourceRequest);

            ReserveBestResource(resourceRequest);
            return (resourceRequest.ResourceObtained != null);

        }

        /// <summary>
        /// Unreserves a quantity of resource from this pool that was previously reserved under the provided
        /// resource request.
        /// </summary>
        /// <param name="resourceRequest">The resource request under which some resource was previously reserved.</param>
        public void Unreserve(IResourceRequest resourceRequest)
        {
            if (diagnostics)
            {
                _Debug.WriteLine(Name + " servicing request to unreserve " + resourceRequest.QuantityDesired + " units of " + resourceRequest);
            }
            if (resourceRequest.ResourceObtained != null)
            {
                resourceRequest.ResourceObtained.Unreserve(resourceRequest);
                resourceRequest.Status = RequestStatus.Free;
            }
            while (_waiters.Count > 0)
            {
                IDetachableEventController dec = (IDetachableEventController)_waiters[0];
                _waiters.RemoveAt(0);
                if (dec.IsWaiting())
                    dec.Resume(); // We might be releasing all resources as a part of an abort.
            }
        }

        /// <summary>
        /// Attempts to acquire a proscribed quantity of a resource in this resource pool. If the
        /// resource has already been reserved under this resourceRequest, it simply acquires that
        /// resource. If no resource has been reserved, then the best available resource will be
        /// reserved, and then acquired.
        /// </summary>
        /// <param name="resourceRequest">The resource request under which the reservation is to take 
        /// place, and describing the resources desired.</param>
        /// <param name="blockAwaitingAcquisition">If true, blocks until resource is acquired.</param>
        /// <returns>true if the acquisition was successful.</returns>
		public bool Acquire(IResourceRequest resourceRequest, bool blockAwaitingAcquisition)
        {
            if (diagnostics)
            {
                _Debug.WriteLine(Name + " servicing request to acquire (" + (blockAwaitingAcquisition ? "with" : "without") + " block) " + resourceRequest.QuantityDesired + " units of " + resourceRequest);
            }

            if (resourceRequest.RequiredResource != null)
            {
                _Debug.Assert(false, "Explicit targeting of resources not yet implemented.");
                // TODO: Explicit targeting of resources and keying of requests not yet implemented.
            }

            if (blockAwaitingAcquisition)
            {
                bool acquired = AcquireWithWait(resourceRequest);
                return acquired;
            }

            bool ableToReserve = ReserveBestResource(resourceRequest);

            if (ableToReserve)
            {
                lock (resourceRequest.ResourceObtained)
                {
                    IResource rsc = resourceRequest.ResourceObtained;
                    rsc.Acquire(resourceRequest);
                    resourceRequest.ResourceObtainedFrom = this;
                    ResourceAcquired?.Invoke(resourceRequest, resourceRequest.ResourceObtained);
                }
            }
            return ableToReserve;
        }

        /// <summary>
        /// Releases the resource held under this resource request back into the resource pool.
        /// </summary>
        /// <param name="resourceRequest">The resource request under which the resource has previously 
        /// been acquired.</param>
        public void Release(IResourceRequest resourceRequest)
        {
            if (resourceRequest.ResourceObtained != null)
            {
                if (diagnostics)
                {
                    string fromWhom = resourceRequest.ToString();
                    if (resourceRequest.ResourceObtained != null)
                        fromWhom = resourceRequest.ResourceObtained.Name;
                    _Debug.WriteLine(Name + " servicing request to release " + resourceRequest.QuantityDesired + " units of " + fromWhom);
                }
                IResource resourceReleased = resourceRequest.ResourceObtained;
                resourceRequest.ResourceObtained?.Release(resourceRequest);
                resourceRequest.Status = RequestStatus.Free;
                ResourceReleased?.Invoke(resourceRequest, resourceReleased);
                while (_waiters.Count > 0)
                {
                    IDetachableEventController dec = (IDetachableEventController)_waiters[0];
                    _waiters.RemoveAt(0);
                    if (dec.IsWaiting())
                        dec.Resume(); // We might be releasing all resources as a part of an abort.
                }
            }
        }

        /// <summary>
        /// The access regulator that governs which requestors may acquire which resources.
        /// </summary>
        public IAccessRegulator AccessRegulator
        {
            set
            {
                _accessRegulator = value;
            }
            get
            {
                return _accessRegulator;
            }
        }

        /// <summary>
        /// Reserves the resource and quantity specifed by the resource request, blocking
        /// until it can return successfully.
        /// </summary>
        /// <param name="request">The resource request that describes the desired resource and quantity.</param>
        /// <returns>Always true.</returns>
        protected bool ReserveWithWait(IResourceRequest request)
        {
            IDetachableEventController dec = Model.Executive.CurrentEventController;
            if (dec == null)
            {
                throw new ApplicationException("Someone tried to call ReserveWithWait() while not in a detachable event. This is not allowed.");
            }

            dec.SetAbortHandler(request.AbortHandler);
            request.ResourceRequestAborting += _onResourceRequestAborting;
            while (!Reserve(request, false))
            {
                _waiters.Add(request, dec);
                dec.Suspend();
                dec.ClearAbortHandler();
            }
            return true;
        }

        /// <summary>
		/// Acquires the resource and quantity specifed by the resource request, blocking
		/// until it can return successfully.
		/// </summary>
		/// <param name="request">The resource request that describes the desired resource and quantity.</param>
		/// <returns>Always true.</returns>
		protected bool AcquireWithWait(IResourceRequest request)
        {
            IDetachableEventController dec = Model.Executive.CurrentEventController;
            if (dec == null)
            {
                throw new ApplicationException("Someone tried to call AcquireWithWait() while not in a detachable event. This is not allowed.");
            }

            dec.SetAbortHandler(request.AbortHandler);
            request.ResourceRequestAborting += _onResourceRequestAborting;
            while (!Acquire(request, false))
            {
                _waiters.Add(request, dec);
                dec.Suspend();
                dec.ClearAbortHandler();
            }
            if (request.ResourceObtained != null)
            {
                ResourceAcquired?.Invoke(request, request.ResourceObtained);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Determines and reserves the 'best' resource in the pool for the specified resource request. The
        /// determination is based on the access regulator agreeing that the requestor may request it, and the
        /// scoring algorithm in the resourceRequest providing the best score for the resource.
        /// </summary>
        /// <param name="resourceRequest">The resource request that contains the description of the resource
        /// desired, and with the algorithm to apparaise the 'goodness' of each resource.</param>
        /// <returns>true if a resource was successfully reserved.</returns>
        protected virtual bool ReserveBestResource(IResourceRequest resourceRequest)
        {

            if (resourceRequest.Status == RequestStatus.Reserved)
            {
                return true;
            }

            ResourceRequested?.Invoke(resourceRequest, null);

            // If the resource request provides a selection strategy, we will use it.
            // otherwise, we will cycle through resources, executing the scoring strategy.
            if (resourceRequest.ResourceSelectionStrategy != null)
            {
                return resourceRequest.ResourceSelectionStrategy(_resources) != null;
            }

            double highestCapacity = double.MinValue;

            double bestScore = double.MinValue;
            IResource bestResource = null;

            foreach (IResource resource in _resources)
            {
                if (_accessRegulator == null || _accessRegulator.CanAcquire(resource, resourceRequest.Key))
                {
                    if (resource.Capacity > highestCapacity)
                        highestCapacity = resource.Capacity;
                    lock (resource)
                    {
                        if ((resource.Available + resource.PermissibleOverbook) < resourceRequest.QuantityDesired)
                            continue;
                        double thisScore = resourceRequest.GetScore(resource);
                        if (thisScore > bestScore)
                        {
                            bestResource?.Unreserve(resourceRequest);
                            if (resource.Reserve(resourceRequest))
                            {
                                bestScore = thisScore;
                                bestResource = resource;
                                // ReSharper disable once CompareOfFloatsByEqualityOperator (Explicitly set to this.)
                                if (bestScore == Double.MaxValue)
                                    break;
                            }
                        }
                    }
                }
            }

            //			if ( ( bestResource == null ) && highestCapacity < resourceRequest.QuantityDesired ) {
            //				throw new ResourcePoolInsufficientException(this,resourceRequest);
            //			}
            if (bestResource != null)
            {
                resourceRequest.ResourceObtainedFrom = this;
                _Debug.Assert(resourceRequest.Status == RequestStatus.Free);
                resourceRequest.Status = RequestStatus.Reserved;
                return true;
            }
            else
            {
                //				// Let's see if we can *ever* satisfy the request in this Resource Request...
                //				foreach ( IResource resource in m_resources ) {
                //					if ( resourceRequest.GetScore(resource) > double.MinValue ) return false;
                //				}
                //				throw new ApplicationException("Resource manager " + Name + " cannot satisfy resource request " + resourceRequest.ToString());
                return false;
            }
        }

        #endregion

        #region Implementation of IModelObject
        private string _name;
        /// <summary>
        /// The user-friendly name for this object.
        /// </summary>
        /// <value>The name.</value>
        public string Name => _name;

        private string _description;
        /// <summary>
        /// A description of this Resource Manager.
        /// </summary>
        public string Description => _description ?? _name;

        private Guid _guid = Guid.Empty;
        /// <summary>
        /// The Guid for this object. Typically required to be unique.
        /// </summary>
        /// <value>The unique identifier.</value>
        public Guid Guid => _guid;

        /// <summary>
        /// The model that owns this object, or from which this object gets time, etc. data.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model => _model;

        #endregion

        #region IXmlPersistable Members

        /// <summary>
        /// A default constructor, to be used for creating an empty object prior to reconstitution from a serializer.
        /// </summary>
        public ResourceManager()
        {
        }

        /// <summary>
        /// Serializes this object to the specified XmlSerializatonContext.
        /// </summary>
        /// <param name="xmlsc">The XmlSerializatonContext into which this object is to be stored.</param>
        public void SerializeTo(XmlSerializationContext xmlsc)
        {
            xmlsc.StoreObject("Name", _name);
            xmlsc.StoreObject("Guid", _guid);
            xmlsc.StoreObject("Resources", _resources);
        }

        /// <summary>
        /// Deserializes this object from the specified XmlSerializatonContext.
        /// </summary>
        /// <param name="xmlsc">The XmlSerializatonContext from which this object is to be reconstituted.</param>
        public void DeserializeFrom(XmlSerializationContext xmlsc)
        {
            _model = (Model)xmlsc.ContextEntities["Model"];
            _name = (string)xmlsc.LoadObject("Name");
            _guid = (Guid)xmlsc.LoadObject("Guid");
            _resources = (ArrayList)xmlsc.LoadObject("Resources");
            foreach (IResource resource in _resources)
                resource.Manager = this;

        }

        #endregion

        private void OnResourceRequestAborting(IResourceRequest request, IExecutive exec, IDetachableEventController idec)
        {
            _model.AddWarning(new TerminalResourceRequestAbortedWarning(exec, this, request, idec));
        }

        class RscWaiterList : ArrayList
        {
            private static readonly IComparer byPriority = new PriorityComparer();
            private readonly RequestPriorityChangeEvent _rreqPriorityChangeEvent;
            private readonly bool _supportsPriorities;
            private bool _dirty;
            private int _seqNum;

            public RscWaiterList(bool priorityEnabled)
            {
                _dirty = false;
                _supportsPriorities = priorityEnabled;
                _rreqPriorityChangeEvent = rreq_PriorityChangeEvent;
            }

            public void Add(IResourceRequest rreq, IDetachableEventController idec)
            {
                if (!_supportsPriorities)
                {
                    base.Add(idec);
                }
                else
                {
                    rreq.PriorityChangeEvent += _rreqPriorityChangeEvent;
                    _dirty = true; // TODO: Not necessarily
                    base.Add(new RscWaiterListEntry(rreq, idec, _seqNum++));
                }
            }

            public override object this[int index]
            {
                get
                {
                    if (NeedsSorting())
                        Sort(byPriority);
                    return !_supportsPriorities ? base[index] : (((RscWaiterListEntry)base[index]).Idec);
                }
                set
                {
                    throw new ApplicationException("Cannot use index setter on a RscWaiterList");
                }
            }


            public override int Add(object value)
            {
                if (!_supportsPriorities)
                    return Add(value);
                throw new ApplicationException("In a prioritized resource manager, use \"public void Add(IResourceRequest rreq,IDetachableEventController idec)\" to add entries to RscWaiterLists");
            }

            public override void AddRange(ICollection c)
            {
                if (!_supportsPriorities)
                    Add(c);
                throw new ApplicationException("In a prioritized resource manager, use \"public void Add(IResourceRequest rreq,IDetachableEventController idec)\" to add entries to RscWaiterLists");
            }


            public override void Remove(object obj)
            {
                if (!_supportsPriorities)
                {
                    base.Remove(obj);
                }
                else
                {
                    // This will fail - the object to be removed will be an IDEC, and the objects in the list are RscWaiterListEntry objects.
                    // Perhaps define an "Equals" override for RWLE's that make them 'equal' to their IDECs?
                    (((RscWaiterListEntry)obj).Request).PriorityChangeEvent -= _rreqPriorityChangeEvent;
                    if (Count == 0)
                        _seqNum = 0;
                    base.Remove(obj);
                }
            }

            public override IEnumerator GetEnumerator()
            {
                if (!_supportsPriorities)
                {
                    return base.GetEnumerator();
                }
                else
                {
                    if (NeedsSorting())
                    {
                        Sort(byPriority);
                        _dirty = false;
                    }
                    return new RscWaiterListEnumerator(base.GetEnumerator());
                }
            }

            public override IEnumerator GetEnumerator(int index, int count)
            {
                if (!_supportsPriorities)
                {
                    return base.GetEnumerator(index, count);
                }
                else
                {
                    return new RscWaiterListEnumerator(base.GetEnumerator(index, count));
                }
            }

            private void rreq_PriorityChangeEvent(IResourceRequest request, double oldPriority, double newPriority)
            {
                _dirty = true;
            }

            private bool NeedsSorting()
            {
                return _supportsPriorities && _dirty;
            }

            class RscWaiterListEnumerator : IEnumerator
            {

                private readonly IEnumerator _enumerator;

                public RscWaiterListEnumerator(IEnumerator enumerator)
                {
                    _enumerator = enumerator;
                }
                #region IEnumerator Members

                public void Reset()
                {
                    _enumerator.Reset();
                }

                public object Current => (((RscWaiterListEntry)_enumerator.Current).Idec);

                public bool MoveNext()
                {
                    return _enumerator.MoveNext();
                }

                #endregion

            }

            class PriorityComparer : IComparer
            {
                #region IComparer Members

                public int Compare(object x, object y)
                {
                    int priCom = -1 * Comparer.Default.Compare(((RscWaiterListEntry)x).Request.Priority, ((RscWaiterListEntry)y).Request.Priority);
                    if (priCom != 0)
                        return priCom;
                    return Comparer.Default.Compare(((RscWaiterListEntry)x).Sequence, ((RscWaiterListEntry)y).Sequence);
                }
                #endregion
            }

            struct RscWaiterListEntry
            {
                public RscWaiterListEntry(IResourceRequest request, IDetachableEventController idec, int seq)
                {
                    Request = request;
                    Idec = idec;
                    Sequence = seq;
                }
                public readonly IResourceRequest Request;
                public readonly IDetachableEventController Idec;
                public readonly int Sequence;
            }
        }
    }
}
