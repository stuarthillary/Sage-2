/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using System;
using System.Collections;
using System.Linq;
// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Resources
{
    /// <summary>
    /// A MultiResourceTracker is a resource tracker (gathers copies of resource event records) that
    /// can monitor multiple resources during a simulation.
    /// </summary>
    public class MultiResourceTracker : IResourceTracker
    {

        #region Private Members
        private readonly ArrayList _record;
        private readonly ArrayList _targets;
        private readonly IModel _model;
        private bool _enabled = true;
        private static bool _allEnabled = true;
        private ResourceEventRecordFilter _rerFilter;

        #endregion

        /// <summary>
        /// Tracks utilization of a particular resource.
        /// </summary>
        /// <param name="model">The parent model to which the resource, and this tracker, will belong.</param>
        public MultiResourceTracker(IModel model)
        {
            _model = model;
            _record = new ArrayList();
            _targets = new ArrayList();
            _rerFilter = ResourceEventRecordFilters.AllEvents;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="T:MultiResourceTracker"/> class.
        /// </summary>
        /// <param name="trackers">The trackers that are aggregated by this <see cref="T:MultiResourceTracker"/>.</param>
		public MultiResourceTracker(IResourceTracker[] trackers)
        {
            _model = null;
            _targets = new ArrayList(trackers);
            _rerFilter = ResourceEventRecordFilters.AllEvents;
        }

        /// <summary>
        /// Adds the specified resources to those being monitored by this tracker.
        /// </summary>
        /// <param name="targets">The IResource entities that are to be tracked.</param>
        public void AddTargets(params IResource[] targets)
        {
            foreach (IResource target in targets)
            {
                AddTarget(target);
            }
        }

        /// <summary>
        /// Adds the specified resource to those being monitored by this tracker.
        /// </summary>
        /// <param name="target"></param>
        public void AddTarget(IResource target)
        {
            _targets.Add(target);
            target.RequestEvent += target_RequestEvent;
            target.ReservedEvent += target_ReservedEvent;
            target.UnreservedEvent += target_UnreservedEvent;
            target.AcquiredEvent += target_AcquiredEvent;
            target.ReleasedEvent += target_ReleasedEvent;
        }

        /// <summary>
        /// Returns an enumerator across all ResourceEventRecords.
        /// </summary>
        /// <returns>An enumerator across all ResourceEventRecords.</returns>
        [Obsolete("Use EventRecords getter instead")]
        public IEnumerator GetEnumerator()
        {
            return _record.GetEnumerator();
        }

        /// <summary>
        /// Returns all event records that have been collected
        /// </summary>
        public ICollection EventRecords => ArrayList.ReadOnly(_record);

        /// <summary>
        /// The sum of the InitialAvailable(s) of all resources that are being tracked
        /// </summary>
        public double InitialAvailable
        {
            get
            {
                return _targets.Cast<IResource>().Sum(rsc => rsc.InitialAvailable);
            }
        }

        /// <summary>
        /// Clears all ResourceEventRecords.
        /// </summary>
        public void Clear()
        {
            _record.Clear();
        }

        /// <summary>
        /// Turns on tracking for this ResourceTracker. This defaults to 'true', and
        /// allEnabled must also be true, in order for a ResourceTracker to track.
        /// </summary>
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
            }
        }

        /// <summary>
        /// If false, all Trackers will be disabled. If true, then the individual tracker's setting governs.
        /// </summary>
        public static bool GlobalEnabled
        {
            get
            {
                return _allEnabled;
            }
            set
            {
                _allEnabled = value;
            }
        }

        /// <summary>
        /// The filter is given a look at each prospective Record, and allowed to decide whether it is to
        /// be logged or not. In conjunction with simply not adding a resource to the tracker, you can achieve
        /// fine-grained control of the contents of a resource activity log.
        /// </summary>
        public ResourceEventRecordFilter Filter
        {
            set
            {
                _rerFilter = value;
            }
        }

        /// <summary>
        /// Loads a collection of resource event records, and then sorts them using the provided comparer.
        /// </summary>
        /// <param name="bulkRecords">The collection of resource records to be added to this collection.</param>
        /// <param name="clearAllFirst">If true, this tracker's ResourceEventRecord internal collection is cleared out before the new records are added.</param>
        /// <param name="sortCriteria">An IComparer that can compare ResourceEventRecord objects. See ResourceEventRecord.By...() methods.</param>
        public void BulkLoad(ICollection bulkRecords, bool clearAllFirst, IComparer sortCriteria)
        {
            if (clearAllFirst)
                _record.Clear();
            _record.AddRange(bulkRecords);
            if (sortCriteria != null)
                _record.Sort(sortCriteria);
        }

        /// <summary>
        /// Loads a collection of resource event records, and then sorts them by serial number in ascending order.
        /// </summary>
        /// <param name="bulkRecords">The collection of resource records to be added to this collection.</param>
        /// <param name="clearAllFirst">If true, this tracker's ResourceEventRecord internal collection is cleared out before the new records are added.</param>
        public void BulkLoad(ICollection bulkRecords, bool clearAllFirst)
        {
            BulkLoad(bulkRecords, clearAllFirst, ResourceEventRecord.BySerialNumber(false));
        }

        /// <summary>
        /// Loads a collection of resource event records, and then sorts them by serial number in ascending order.
        /// </summary>
        /// <param name="bulkRecords">The collection of resource records to be added to this collection.</param>
        public void BulkLoad(ICollection bulkRecords)
        {
            BulkLoad(bulkRecords, true, ResourceEventRecord.BySerialNumber(false));
        }

        #region Private Members
        private void target_RequestEvent(IResourceRequest irr, IResource resource)
        {
            if (_allEnabled && _enabled)
            {
                LogEvent(resource, irr, ResourceAction.Request);
            }
        }

        private void target_ReservedEvent(IResourceRequest irr, IResource resource)
        {
            if (_allEnabled && _enabled)
            {
                LogEvent(resource, irr, ResourceAction.Reserved);
            }
        }

        private void target_UnreservedEvent(IResourceRequest irr, IResource resource)
        {
            if (_allEnabled && _enabled)
            {
                LogEvent(resource, irr, ResourceAction.Unreserved);
            }
        }

        private void target_AcquiredEvent(IResourceRequest irr, IResource resource)
        {
            if (_allEnabled && _enabled)
            {
                LogEvent(resource, irr, ResourceAction.Acquired);
            }
        }

        private void target_ReleasedEvent(IResourceRequest irr, IResource resource)
        {
            if (_allEnabled && _enabled)
            {
                LogEvent(resource, irr, ResourceAction.Released);
            }
        }

        private void LogEvent(IResource resource, IResourceRequest irr, ResourceAction action)
        {
            ResourceEventRecord rer = new ResourceEventRecord(_model.Executive.Now, resource, irr, action);
            if (_rerFilter != null && _rerFilter(rer))
                _record.Add(rer);
        }

        #endregion

    }
}