/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using System.Linq;
// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Resources
{
#if INCLUDE_WIP
    public class StaticResourceTracker : IResourceTracker
    {

    #region Private Fields

    #endregion

        public StaticResourceTracker(ICollection resourceEventRecords, double initialAvailable = 0){
			EventRecords = resourceEventRecords;
			InitialAvailable = initialAvailable;
		}

    #region IResourceTracker Members
		        public void Clear() { EventRecords = new ArrayList();} // Not sure why someone would want to do this on a static tracker.

		        public bool Enabled { get { return false; } set { throw new NotImplementedException(); } }

		        public ResourceEventRecordFilter Filter { set { throw new NotImplementedException(); } }

		        public ICollection EventRecords { get; private set; }

                /// <summary>
                /// The initial value(s) of all resources that are being tracked
                /// </summary>
                /// <value></value>
		        public double InitialAvailable { get; }

    #endregion

    #region IEnumerable Members

                /// <summary>
                /// Returns an enumerator that iterates through a collection.
                /// </summary>
                /// <returns>
                /// An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
                /// </returns>
		        public IEnumerator GetEnumerator() {
			        return EventRecords.GetEnumerator();
		        }

    #endregion

	}
#endif

    /// <summary>
    /// Class that consolidates a collection of IResourceTrackers
    /// </summary>
    public class ResourceTrackerAggregator : IResourceTracker
    {

#region Private Fields

        private readonly ArrayList _records;
        private readonly ArrayList _targets;

#endregion

		/// <summary>
		/// Standard constructor
		/// </summary>
		/// <param name="trackers">The trackers to consolidate</param>
		public ResourceTrackerAggregator(IEnumerable trackers){
			_records = new ArrayList();
			_targets = new ArrayList();

		    // ReSharper disable once LoopCanBePartlyConvertedToQuery (Much clearer this way.)
			foreach(IResourceTracker rt in trackers) {
				foreach(ResourceEventRecord rer in rt.EventRecords) {
					if(!_targets.Contains(rer.Resource)) _targets.Add(rer.Resource);
					_records.Add(rer);
				} // end foreach rer
			} // end foreach rt

			_records.Sort(ResourceEventRecord.BySerialNumber(false));
		} // end ResourceTrackerAggregator

#region IResourceTracker Members

		/// <summary>
		/// Clears all ResourceEventRecords.
		/// </summary>
		public void Clear() {
			throw new NotImplementedException("Cannot clear this tracker's record collection. Create a new one if you have a new aggregation to represent.");
		}
		/// <summary>
		/// Turns on tracking for this ResourceTracker. This defaults to 'true', and
		/// allEnabled must also be true, in order for a ResourceTracker to track.
		/// </summary>
		public bool Enabled {
			get { return false; }
			set { throw new NotImplementedException("Cannot enable this tracker to perform further tracking. It is an aggregated record collection only.");}
		}
		/// <summary>
		/// Allows for the setting of the active filter on the records
		/// </summary>
		public ResourceEventRecordFilter Filter {
			set { throw new NotImplementedException("Cannot change the filter on this tracker to perform further tracking. It is an aggregated record collection only.");}
		}
		/// <summary>
		/// Returns all event records that have been collected
		/// </summary>
		public ICollection EventRecords => ArrayList.ReadOnly(_records);

	    /// <summary>
		/// The InitialAvailable(s) of all resources that are being tracked
		/// </summary>
		public double InitialAvailable { 
			get
			{
			    return _targets.Cast<IResource>().Sum(rsc => rsc.InitialAvailable);
			}
		}

#endregion

#region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
        /// </returns>
		[Obsolete("Use EventRecords getter instead")]
		public IEnumerator GetEnumerator() {
			return _records.GetEnumerator();
		}


#endregion

	}
}