/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using System;
using System.Collections;
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Highpoint.Sage.Resources
{

    /// <summary>
    /// This is a method that, given a list of objects implementing IResource,
    /// chooses the 'best' one. 
    /// </summary>
    public delegate IResource ResourceSelectionStrategy(IList candidates);

    /// <summary>
    /// This is the signature of the event that is fired when a Resource request is aborted.
    /// </summary>
    public delegate void ResourceRequestAbortEvent(IResourceRequest request, IExecutive exec, IDetachableEventController idec);

    /// <summary>
    /// This is the signature of the event that is fired when a Resource request changes its priority.
    /// </summary>
    public delegate void RequestPriorityChangeEvent(IResourceRequest request, double oldPriority, double newPriority);

    /// <summary>
    /// This is the signature of the callback that is invoked when a resource request, executed without a block and
    /// initially refused, is eventually deemed grantable, and as well, later, to notify the requester that its request
    /// has been granted.
    /// </summary>
    public delegate bool ResourceRequestCallback(IResourceRequest resourceRequest);
    /// <summary>
    /// IResourceRequest is an interface implemented by a class that is able
    /// to request a resource. This is typically an agent employed by the
    /// resource user itself. A resource request is submitted to a resource
    /// manager, whose job it is to mediate a process whereby the resource
    /// request selects, and is granted (or not) access to that resource. 
    /// </summary>
    public interface IResourceRequest : IComparable
    {

        /// <summary>
        /// Gets the score that describes the suitability of the resource to fulfill this resource request.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <returns>The score</returns>
        double GetScore(IResource resource);

        /// <summary>
        /// This property represents the quantity this request is to remove from the resource's
        /// 'Available' capacity.
        /// </summary>
        double QuantityDesired
        {
            get;
        }

        /// <summary>
        /// This is a key that will be used to see if the resource manager is allowed to
        /// grant a given resource to the requester. It is used in conjunction with resource earmarking.
        /// (See IAccessRegulator) 
        /// </summary>
        object Key
        {
            get;
        }

        /// <summary>
        /// An indication of the priority of this request. A larger number indicates a higher priority.
        /// </summary>
        double Priority
        {
            get; set;
        }

        /// <summary>
        /// An event that is fired if the priority of this request is changed.
        /// </summary>
        event RequestPriorityChangeEvent PriorityChangeEvent;

        /// <summary>
        /// If non-null, this infers a specific, needed resource.
        /// </summary>
        IResource RequiredResource
        {
            get; set;
        }

        /// <summary>
        /// This property represents the quantity this request actually removed from the resource's
        /// 'Available' capacity. It is filled in by the granting authority.
        /// </summary>
        double QuantityObtained
        {
            get; set;
        }

        /// <summary>
        /// This is a reference to the actual resource that was obtained.
        /// </summary>
        IResource ResourceObtained
        {
            get; set;
        }

        /// <summary>
        /// Gets the status of this resource request.
        /// </summary>
        /// <value>The status.</value>
        RequestStatus Status
        {
            get; set;
        }

        /// <summary>
        /// This is a reference to the resource manager that granted access to the resource.
        /// </summary>
        IResourceManager ResourceObtainedFrom
        {
            get; set;
        }

        /// <summary>
        /// This is a reference to the object requesting the resource.
        /// </summary>
        IHasIdentity Requester
        {
            get; set;
        }

        /// <summary>
        /// This is the resource selection strategy that is to be used by the resource
        /// manager to select the resource to be granted from the pool of available
        /// resources.
        /// </summary>
        ResourceSelectionStrategy ResourceSelectionStrategy
        {
            get;
        }

        /// <summary>
        /// Reserves a resource from the specified resource manager, or the provided default manager, if none is provided in this call.
        /// </summary>
        /// <param name="resourceManager">The resource manager from which the resource is desired. Can be null, if a default manager has been provided.</param>
        /// <param name="blockAwaitingReservation">If true, this call blocks until the resource is available.</param>
        /// <returns>true if the reservation was successful, false otherwise.</returns>
        bool Reserve(IResourceManager resourceManager, bool blockAwaitingReservation);

        /// <summary>
        /// Releases the resource previously obtained by this ResourceRequest.
        /// </summary>
        void Unreserve();

        /// <summary>
        /// Acquires a resource from the specified resource manager, or the provided default manager,
        /// if none is provided in this call. If the request has already successfully reserved a resource,
        /// then the reservation is revoked and the acquisition is honored in one atomic operation.
        /// </summary>
        /// <param name="resourceManager">The resource manager from which the resource is desired. Can be null, if a default manager has been provided.</param>
        /// <param name="blockAwaitingAcquisition">If true, this call blocks until the resource is available.</param>
        /// <returns>true if the acquisition was successful, false otherwise.</returns>
        bool Acquire(IResourceManager resourceManager, bool blockAwaitingAcquisition);

        /// <summary>
        /// Releases the resource previously obtained by this ResourceRequest.
        /// </summary>
        void Release();

        /// <summary>
        /// This method is called if the resource request is pending, and gets aborted, for
        /// example due to resource deadlocking. It can be null, in which case no deadlock
        /// detection is provided for the implementing type of ResourceRequest.
        /// </summary>
        DetachableEventAbortHandler AbortHandler
        {
            get;
        }

        /// <summary>
        /// Typically fires as a result of the RequestAbortHandler being called. In that method,
        /// it picks up the IResourceRequest identity, and is passed on through this event, which
        /// includes the IResourceRequest.
        /// </summary>
        event ResourceRequestAbortEvent ResourceRequestAborting;

        /// <summary>
        /// Creates a fresh replica of this resource request, without any of the in-progress data. This replica can
        /// be used to generate another, similar resource request that can acquire its own resource.
        /// </summary>
        ResourceRequestSource Replicate
        {
            get;
        }

        /// <summary>
        /// This is the resource manager from which a resource is obtained if none is provided in the reserve or
        /// acquire API calls.
        /// </summary>
        IResourceManager DefaultResourceManager
        {
            get; set;
        }

        /// <summary>
        /// This callback is called when a request, made with a do-not-block specification, that was initially
        /// refused, is finally deemed grantable, and provides the callee (presumably the original requester) 
        /// with an opportunity to say, "No, I don't want that any more", or perhaps to get ready for receipt
        /// of the resource in question.
        /// </summary>
        ResourceRequestCallback AsyncGrantConfirmationCallback
        {
            get; set;
        }

        /// <summary>
        /// Called after a resource request is granted asynchronously.
        /// </summary>
        ResourceRequestCallback AsyncGrantNotificationCallback
        {
            get; set;
        }

        /// <summary>
        /// Data maintained by this resource request on behalf of the requester.
        /// </summary>
        object UserData
        {
            set; get;
        }
    }
}
