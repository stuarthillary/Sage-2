/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using System;
using System.Collections;
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

namespace Highpoint.Sage.Resources
{
    /// <summary>
    /// Summary description for Resource.
    /// </summary>
    public class SelfManagingResource : IResource, IResourceManager, IHasControllableCapacity
    {

        #region Private Fields
        private readonly Resource _baseResource;
        private readonly ResourceManager _baseResourceManager;
        #endregion

        /// <summary>
		/// Creates a new SelfManagingResource. A resource is created with a capacity, and is granted in portions
		/// of that capacity, or if atomic, all-or-nothing. The IResourceRequest will specify a desired
		/// amount. If the IResourceRequest specifies a desired quantity less than the resource's capacity,
		/// and the resource is atomic, the IResourceRequest will be granted the full capacity of the
		/// resource. A self-managing resource is a resource that is responsible for granting access to itself.
		/// </summary>
		/// <param name="model">The model to which the Resource will belong.</param>
		/// <param name="name">The name of the Resource.</param>
		/// <param name="guid">The guid of the Resource.</param>
		/// <param name="capacity">The capacity of the Resource. How much there is to be granted. This API infers that the resource,
		/// at creation has its full capacity available.</param>
		/// <param name="isAtomic">True if the Resource is atomic. Atomicity infers that the resource is granted all-or-nothing.</param>
		/// <param name="isDiscrete">True if the Resource is discrete. Discreteness infers that the resource is granted in unitary amounts.</param>
		/// <param name="isPersistent">True if the Resource is persistent. Atomicity infers that the resource, once granted, must be returned to the pool.</param>
		/// <param name="supportsPriorities">True if this resource is able to treat resource requests in a prioritized order.</param>
		public SelfManagingResource(IModel model, string name, Guid guid, double capacity, bool isAtomic, bool isDiscrete, bool isPersistent, bool supportsPriorities = false)
        {
            _baseResource = new Resource(model, name, guid, capacity, capacity, isAtomic, isDiscrete, isPersistent, this);
            model?.ModelObjects.Remove(guid);
            _baseResourceManager = new ResourceManager(model, name, guid, supportsPriorities);
            model?.ModelObjects.Remove(guid);
            _baseResourceManager.Add(_baseResource);
            if (model != null)
            {
                IModelWithResources resources = model as IModelWithResources;
                resources?.OnNewResourceCreated(this);
                model.ModelObjects.Add(guid, this);
            }
        }

        /// <summary>
		/// Creates a new SelfManagingResource. A resource is created with a capacity, and is granted in portions
		/// of that capacity, or if atomic, all-or-nothing. The IResourceRequest will specify a desired
		/// amount. If the IResourceRequest specifies a desired quantity less than the resource's capacity,
		/// and the resource is atomic, the IResourceRequest will be granted the full capacity of the
		/// resource. A self-managing resource is a resource that is responsible for granting access to itself.
		/// </summary>
		/// <param name="model">The model to which the Resource will belong.</param>
		/// <param name="name">The name of the Resource.</param>
		/// <param name="guid">The guid of the Resource.</param>
		/// <param name="capacity">The capacity of the Resource. How much there <b>can be</b> to be granted.</param>
		/// <param name="available">The availability of the Resource. How much there <b>is, at start,</b> to be granted.</param>
		/// <param name="isAtomic">True if the Resource is atomic. Atomicity infers that the resource is granted all-or-nothing.</param>
		/// <param name="isDiscrete">True if the Resource is discrete. Discreteness infers that the resource is granted in unitary amounts.</param>
		/// <param name="isPersistent">True if the Resource is persistent. Atomicity infers that the resource, once granted, must be returned to the pool.</param>
		/// <param name="supportsPriorities">True if this resource is able ot treat resource requests in a prioritized order.</param>
        public SelfManagingResource(IModel model, string name, Guid guid, double capacity, double available, bool isAtomic, bool isDiscrete, bool isPersistent, bool supportsPriorities = false)
        {
            _baseResource = new Resource(model, name, guid, capacity, available, isAtomic, isDiscrete, isPersistent, this);
            _baseResourceManager = new ResourceManager(model, name, guid, supportsPriorities) { _baseResource };
            IModelWithResources modelWithResources = model as IModelWithResources;
            modelWithResources?.OnNewResourceCreated(this);
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

            // m_baseResource should be initialized with this information.  Should this be an error?
        }

        /// <summary>
        /// We override the Equals operator so that a self-managing resource can declare
        /// equivalency to its underlying resource.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj.Equals(this) || _baseResource.Equals(obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            return _baseResource.GetHashCode();
        }

        #region Implementation of IModelObject
        /// <summary>
        /// The user-friendly name for this object.
        /// </summary>
        /// <value>The name.</value>
        public string Name => _baseResource.Name;

        /// <summary>
		/// A description of this Resource.
		/// </summary>
		public string Description => _baseResource.Description;

        /// <summary>
        /// The Guid for this object. Typically required to be unique.
        /// </summary>
        /// <value>The unique identifier.</value>
        public Guid Guid => _baseResource.Guid;

        /// <summary>
        /// The model that owns this object, or from which this object gets time, etc. data.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model => _baseResource.Model;

#pragma warning disable CS0067
        /// <summary>
        /// Fired when a resource is added to the pool.
        /// </summary>
        public event ResourceManagerEvent ResourceAdded;

        /// <summary>
        /// Fired when a resource is removed from the pool.
        /// </summary>
        public event ResourceManagerEvent ResourceRemoved;
#pragma warning restore CS0067

        #endregion

        #region Implementation of IResource

        /// <summary>
        /// Gets a value indicating whether this instance is discrete. A discrete resource is allocated in integral amounts, such as cartons or drums.
        /// </summary>
        /// <value><c>true</c> if this instance is discrete; otherwise, <c>false</c>.</value>
        public bool IsDiscrete => _baseResource.IsDiscrete;

        /// <summary>
        /// Gets a value indicating whether this instance is persistent. A persistent resource is returned to the pool after it is used.
        /// </summary>
        /// <value><c>true</c> if this instance is persistent; otherwise, <c>false</c>.</value>
        public bool IsPersistent => _baseResource.IsPersistent;

        /// <summary>
        /// Gets a value indicating whether this instance is atomic. And atomic resource is allocated all-or-none, such as a vehicle.
        /// </summary>
        /// <value><c>true</c> if this instance is atomic; otherwise, <c>false</c>.</value>
        public bool IsAtomic => _baseResource.IsAtomic;

        /// <summary>
        /// Unreserves the specified request. Returns it to availability.
        /// </summary>
        /// <param name="request">The request.</param>
        void IResource.Unreserve(IResourceRequest request)
        {
            _baseResource.Unreserve(request);
        }

        /// <summary>
        /// Releases the specified request. Returns it to availability and the resource pool.
        /// </summary>
        /// <param name="request">The request.</param>
        void IResource.Release(IResourceRequest request)
        {
            _baseResource.Release(request);
        }

        /// <summary>
        /// Reserves the specified request. Removes it from availability, but not from the pool. This is typically an intermediate state held during resource negotiation.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns><c>true</c> if the resource was successfully reserved, <c>false</c> otherwise.</returns>
        bool IResource.Reserve(IResourceRequest request)
        {
            return _baseResource.Reserve(request);
        }

        /// <summary>
        /// Acquires the specified request. Removes it from availability and from the resource pool.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns><c>true</c> if if the resource was successfully acquired, <c>false</c> otherwise.</returns>
        bool IResource.Acquire(IResourceRequest request)
        {
            return _baseResource.Acquire(request);
        }

        /// <summary>
        /// Resets this instance, returning it to its initial capacity and availability.
        /// </summary>
        public virtual void Reset()
        {
            _baseResource.Reset();
        }

        /// <summary>
        /// Occurs when this resource has been released.
        /// </summary>
        public event ResourceStatusEvent ReleasedEvent
        {
            add
            {
                _baseResource.ReleasedEvent += value;
            }
            remove
            {
                _baseResource.ReleasedEvent -= value;
            }
        }

        /// <summary>
        /// Occurs when this resource has been acquired.
        /// </summary>
        public event ResourceStatusEvent AcquiredEvent
        {
            add
            {
                _baseResource.AcquiredEvent += value;
            }
            remove
            {
                _baseResource.AcquiredEvent -= value;
            }
        }

        /// <summary>
        /// The quantity of this resource that will be available if the resource experiences a reset.
        /// </summary>
        /// <value>The initial available.</value>
        public double InitialAvailable => _baseResource.InitialAvailable;

        /// <summary>
        /// How much of this resource is currently available to service requests.
        /// </summary>
        /// <value>The available.</value>
        public double Available
        {
            get
            {
                return _baseResource.Available;
            }
            set
            {
                _baseResource.Available = value;
            }
        }

        /// <summary>
        /// Gets or sets the manager of the resource.
        /// </summary>
        /// <value>The manager.</value>
        public IResourceManager Manager
        {
            get
            {
                return _baseResource.Manager;
            }
            set
            {
                _baseResource.Manager = value;
            }
        }

        /// <summary>
        /// The capacity of this resource that will be in effect if the resource experiences a reset.
        /// </summary>
        /// <value>The initial capacity.</value>
        public double InitialCapacity => _baseResource.InitialCapacity;

        /// <summary>
        /// The current capacity of this resource - how much 'Available' can be, at its highest value.
        /// </summary>
        /// <value>The capacity.</value>
        public double Capacity
        {
            get
            {
                return _baseResource.Capacity;
            }
            set
            {
                _baseResource.Capacity = value;
            }
        }

        /// <summary>
        /// The amount by which it is permissible to overbook this resource.
        /// </summary>
        public double PermissibleOverbook
        {
            get
            {
                return _baseResource.PermissibleOverbook;
            }
            set
            {
                _baseResource.PermissibleOverbook = value;
            }
        }

        #endregion

        #region Implementation of IResourceManager
        /// <summary>
        /// Gets the resources owned by this Resource Manager.
        /// </summary>
        /// <value>The resources.</value>
        public IList Resources => _baseResourceManager.Resources;

        /// <summary>
        /// Fired when a resource request is received.
        /// </summary>
        public event ResourceStatusEvent ResourceRequested
        {
            add
            {
                _baseResourceManager.ResourceRequested += value;
            }
            remove
            {
                _baseResourceManager.ResourceRequested -= value;
            }
        }

        /// <summary>
        /// Attempts to reserve this resource using the provided IResourceRequest.
        /// </summary>
        /// <param name="resourceRequest">The IResourceRequest that wants this resource.</param>
        /// <param name="blockAwaitingAcquisition">If true, this call will not return until the resource has been acquired.</param>
        /// <returns>True if this resource was granted as a reservation to the IResourceRequest.</returns>
        public bool Reserve(IResourceRequest resourceRequest, bool blockAwaitingAcquisition)
        {
            return _baseResourceManager.Reserve(resourceRequest, blockAwaitingAcquisition);
        }

        /// <summary>
        /// Attempts to acquire this resource using the provided IResourceRequest.
        /// </summary>
        /// <param name="resourceRequest">The IResourceRequest that wants this resource.</param>
        /// <param name="blockAwaitingAcquisition">If true, this call will not return until the resource has been acquired.</param>
        /// <returns>True if this resource was granted to the IResourceRequest.</returns>
        public bool Acquire(IResourceRequest resourceRequest, bool blockAwaitingAcquisition)
        {
            return _baseResourceManager.Acquire(resourceRequest, blockAwaitingAcquisition);
        }

        /// <summary>
        /// Unreserves the resource through the provided resource request.
        /// </summary>
        /// <param name="request">The IResourceRequest through the reservation was originally obtained.</param>
        public void Unreserve(IResourceRequest request)
        {
            _baseResourceManager.Unreserve(request);
        }

        /// <summary>
        /// Releases the resource through the provided resource request.
        /// </summary>
        /// <param name="request">The IResourceRequest through the reservation was originally obtained.</param> 
        public void Release(IResourceRequest request)
        {
            _baseResourceManager.Release(request);
        }

        /// <summary>
        /// Gets or sets the access regulator, which is an object that can allow or deny
        /// individual ResourceRequests access to specified resources.
        /// </summary>
        /// <value>The access regulator.</value>
        public IAccessRegulator AccessRegulator
        {
            set
            {
                _baseResourceManager.AccessRegulator = value;
            }
            get
            {
                return _baseResourceManager.AccessRegulator;
            }
        }

        /// <summary>
        /// This event is fired when any acq/rls/rsv/unr request is issued to this equipment.
        /// </summary>
        public event ResourceStatusEvent RequestEvent
        {
            add
            {
                _baseResource.RequestEvent += value;
            }
            remove
            {
                _baseResource.RequestEvent -= value;
            }
        }

        /// <summary>
        /// This event is fired when this equipment is reserved.
        /// </summary>
        public event ResourceStatusEvent ReservedEvent
        {
            add
            {
                _baseResource.ReservedEvent += value;
            }
            remove
            {
                _baseResource.ReservedEvent -= value;
            }
        }

        /// <summary>
        /// This event is fired when this equipment is unreserved.
        /// </summary>
        public event ResourceStatusEvent UnreservedEvent
        {
            add
            {
                _baseResource.UnreservedEvent += value;
            }
            remove
            {
                _baseResource.UnreservedEvent -= value;
            }
        }

        /// <summary>
        /// This event is fired when this resource is acquired.
        /// </summary>
        public event ResourceStatusEvent ResourceAcquired
        {
            add
            {
                _baseResource.AcquiredEvent += value;
            }
            remove
            {
                _baseResource.AcquiredEvent -= value;
            }
        }

        /// <summary>
        /// This event is fired when this resource is released.
        /// </summary>
        public event ResourceStatusEvent ResourceReleased
        {
            add
            {
                _baseResource.ReleasedEvent += value;
            }
            remove
            {
                _baseResource.ReleasedEvent -= value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this resource manager supports prioritized requests.
        /// </summary>
        /// <value><c>true</c> if [supports prioritized requests]; otherwise, <c>false</c>.</value>
        /// <exception cref="NotImplementedException"></exception>
        public bool SupportsPrioritizedRequests
        {
            get
            {
                return _baseResourceManager.SupportsPrioritizedRequests;
            }
        }

        #endregion

        /// <summary>
		/// An Object that contains data about this self-managing resource. The default is a 
		/// null reference (Nothing in Visual Basic).
		/// Any Object derived type can be assigned to this property.
		/// </summary>
		public object Tag
        {
            get; set;
        }
    }
}
