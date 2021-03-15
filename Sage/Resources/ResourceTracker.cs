/* This source code licensed under the GNU Affero General Public License */

using _Debug = System.Diagnostics.Debug;
using System.Collections;
using Highpoint.Sage.SimCore;
// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Resources
{

    /// <summary>
    /// Implemented by anything that can filter ResourceEventRecords.
    /// </summary>
    /// <param name="candidate">The ResourceEventRecord for consideration.</param>
    /// <returns>true if the ResourceEventRecord is to be passed through the filter, false if it is to be filtered out.</returns>
	public delegate bool ResourceEventRecordFilter(ResourceEventRecord candidate);

    /// <summary>
    /// This class is the baseline implementation of <see cref="Highpoint.Sage.Resources.IResourceTracker"/>. It watches
    /// a specified resource over a model run, and creates &amp; collects <see cref="Highpoint.Sage.Resources.ResourceEventRecord"/>s on the activities of that resource.
    /// </summary>
	public class ResourceTracker : IResourceTracker {

        #region Private Fields
        private static readonly bool _diagnostics = Diagnostics.DiagnosticAids.Diagnostics("ResourceTracker");
        private readonly ArrayList _record;
        private readonly IResource _target;
        private readonly IModel _model;
        private ResourceEventRecordFilter _rerFilter;

        #endregion 

		/// <summary>
		/// Tracks utilization of a particular resource.
		/// </summary>
		/// <param name="model">The parent model to which the resource, and this tracker, will belong.</param>
		/// <param name="target">The resource that this tracker will track.</param>
		public ResourceTracker(IModel model, IResource target){
			_model = model;
			_target = target;
			_rerFilter = ResourceEventRecordFilters.AllEvents;
			_target.RequestEvent   += target_RequestEvent;
			_target.ReservedEvent  += target_ReservedEvent;
			_target.UnreservedEvent+= target_UnreservedEvent;
			_target.AcquiredEvent  += target_AcquiredEvent;
			_target.ReleasedEvent  += target_ReleasedEvent;
			_record = new ArrayList();
			if ( _diagnostics ) _Debug.WriteLine(_model.Executive.Now + " : Created a Resource Tracker focused on " + _target.Name + " (" + _target.Guid + ").");
		}

		/// <summary>
		/// The resource that this tracker is tracking.
		/// </summary>
		public IResource Resource => _target;

        /// <summary>
		/// Returns an enumerator across all ResourceEventRecords.
		/// </summary>
		/// <returns>An enumerator across all ResourceEventRecords.</returns>
		public IEnumerator GetEnumerator(){ return _record.GetEnumerator(); }

		/// <summary>
		/// Returns all event records that have been collected
		/// </summary>
		public ICollection EventRecords => ArrayList.ReadOnly(_record);

        /// <summary>
		/// The InitialAvailable(s) of all resources that are being tracked
		/// </summary>
		public double InitialAvailable => Resource.InitialAvailable;

        /// <summary>
		/// Clears all ResourceEventRecords.
		/// </summary>
		public void Clear(){ _record.Clear(); }

		/// <summary>
		/// Turns on tracking for this ResourceTracker. This defaults to 'true', and
		/// allEnabled must also be true, in order for a ResourceTracker to track.
		/// </summary>
		public bool Enabled { get; set; } = true;

        /// <summary>
		/// If false, all Trackers will be disabled. If true, then the individual tracker's setting governs.
		/// </summary>
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
		public static bool GlobalEnabled { get; set; } = true;

        /// <summary>
		/// The filter is given a look at each prospective Record, and allowed to decide whether it is to
		/// be logged or not. In conjunction with simply not adding a resource to the tracker, you can achieve
		/// fine-grained control of the contents of a resource activity log.
		/// </summary>
		public ResourceEventRecordFilter Filter { set{ _rerFilter = value; } }

        #region Private Members
        private void target_RequestEvent(IResourceRequest irr, IResource resource)
        {
            if (GlobalEnabled && Enabled)
            {
                LogEvent(resource, irr, ResourceAction.Request);
            }
        }

        private void target_ReservedEvent(IResourceRequest irr, IResource resource)
        {
            if (GlobalEnabled && Enabled)
            {
                LogEvent(resource, irr, ResourceAction.Reserved);
            }
        }

        private void target_UnreservedEvent(IResourceRequest irr, IResource resource)
        {
            if (GlobalEnabled && Enabled)
            {
                LogEvent(resource, irr, ResourceAction.Unreserved);
            }
        }

        private void target_AcquiredEvent(IResourceRequest irr, IResource resource)
        {
            if (GlobalEnabled && Enabled)
            {
                LogEvent(resource, irr, ResourceAction.Acquired);
            }
        }

        private void target_ReleasedEvent(IResourceRequest irr, IResource resource)
        {
            if (GlobalEnabled && Enabled)
            {
                LogEvent(resource, irr, ResourceAction.Released);
            }
        }

        private void LogEvent(IResource resource, IResourceRequest irr, ResourceAction action)
        {
            if (_diagnostics) _Debug.WriteLine(_model.Executive.Now + " : Resource Tracker " + _target.Name
                                   + " (" + _target.Guid + ") logged " + action
                                   + " with " + irr.QuantityDesired + ".");
            ResourceEventRecord rer = new ResourceEventRecord(_model.Executive.Now, resource, irr, action);
            if (_rerFilter == null || _rerFilter(rer))
            {
                _record.Add(rer);
                if (_diagnostics) _Debug.WriteLine("\tLogged.");
            }
            else
            {
                if (_diagnostics) _Debug.WriteLine("\tFiltered out.");
            }
        }

        #endregion 

	}

    
}