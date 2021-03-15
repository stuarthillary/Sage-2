/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.Scheduling
{
#if NOT_DEFINED
	/// <summary>
	/// A TimePeriodAspectSelector is useful for when one is working with a group of objects each 
	/// with time periods, but does not want to explicity select them - essentially, "give me the best
	/// indication of time period that you have", with a certain priority.<p/>
	/// For example, if we are working with a list of tasks that have planned and actual time period aspects,
	/// and we want the best time period data available for any given task, we would wrap the operation
	/// in a TimePeriodAspectSelector whose filterCriteria argument was new object[]{TaskAspectKey.Actual,TaskAspectKey.Planned};<p/>
	/// This would cause any time data requested to come from the TaskAspectKey.Actual aspect if it were
	/// defined there, and only from the TaskAspectKey.Planned if the desired data were defined by that one,
	/// and not by the preferred one.<p/>
	/// This is intended to be defined and used as follows:
	/// <code></code>
	/// </summary>
	public class TimePeriodAspectSelector : ITimePeriod {
		private IHasTimePeriodAspects m_ihti = null;
		private string m_name = null;
		private string m_description = null;
		private Guid m_guid = Guid.Empty;
		private object[] m_filterCriteria;
		/// <summary>
		/// Creates a TimePeriodAspectSelector with the specified array of filter criteria. The aspects
		/// whose keys are earlier in the array have priority over those later in the array, and any that
		/// are not in the array are not considered. The elements of the array must all be legitimate
		/// aspect keys.
		/// </summary>
		/// <param name="filterCriteria">An array of valid Aspect Keys for the targets being queried. An aspect 
		/// key is any object under which an ITimePeriod might have been stored in an object that implements
		/// IHasTimePeriodAspects.</param>
		public TimePeriodAspectSelector(object[] filterCriteria){
			m_ihti = null;
			m_filterCriteria = filterCriteria;
		}

		/// <summary>
		/// Sets the object whose time period aspects are being queried to a new value.
		/// </summary>
		/// <param name="ihti">The new object being queried for data from its time period aspects.</param>
		/// <returns>Itself, so that it can be used as <code>DateTime startTime = myTimePeriodAspectSelector.SetTarget(newTask).StartTime;</code></returns>
		public TimePeriodAspectSelector SetTarget(IHasTimePeriodAspects ihti){
			m_ihti = ihti;
			return this;
		}

		/// <summary>
		/// The milestone that represents the starting of this time period.
		/// </summary>
		public IMilestone StartMilestone { 
			get { 
				foreach ( object key in m_filterCriteria ) {
					ITimePeriod itro = m_ihti.GetTimePeriodAspect(key);
					if ( itro.HasStartTime ) return itro.StartMilestone;
				}
				return m_ihti.GetTimePeriodAspect(m_filterCriteria[0]).StartMilestone;
			} 
		}

		/// <summary>
		/// The milestone that represents the ending point of this time period.
		/// </summary>
		public IMilestone EndMilestone { 
			get { 
				foreach ( object key in m_filterCriteria ) {
					ITimePeriod itro = m_ihti.GetTimePeriodAspect(key);
					if ( itro.HasStartTime ) return itro.EndMilestone;
				}
				return m_ihti.GetTimePeriodAspect(m_filterCriteria[0]).EndMilestone;
			} 
		}

		/// <summary>
		/// Gets the best available defined start time of the time period.
		/// </summary>
		public DateTime StartTime { 
			get{
				foreach ( object key in m_filterCriteria ) {
					ITimePeriod itro = m_ihti.GetTimePeriodAspect(key);
					if ( itro.HasStartTime ) return itro.StartTime;
				}
				return m_ihti.GetTimePeriodAspect(m_filterCriteria[0]).StartTime;
			} 
		}
		/// <summary>
		/// Gets the best available defined end time of the time period.
		/// </summary>
		public DateTime EndTime { 
			get{
				foreach ( object key in m_filterCriteria ) {
					ITimePeriod itro = m_ihti.GetTimePeriodAspect(key);
					if ( itro.HasEndTime ) return itro.EndTime;
				}
				return m_ihti.GetTimePeriodAspect(m_filterCriteria[0]).EndTime;
			} 
		}
		/// <summary>
		/// Gets the best available defined duration of the time period.
		/// </summary>
		public TimeSpan Duration { 
			get{
				foreach ( object key in m_filterCriteria ) {
					ITimePeriod itro = m_ihti.GetTimePeriodAspect(key);
					if ( itro.HasDuration ) return itro.Duration;
				}
				return m_ihti.GetTimePeriodAspect(m_filterCriteria[0]).Duration;
			} 
		}

		/// <summary>
		/// True if any of the time period aspects has a determinate start time.
		/// </summary>
		public bool HasStartTime { 
			get{
				foreach ( object key in m_filterCriteria ) {
					if ( m_ihti.GetTimePeriodAspect(key).HasStartTime ) return true;
				}
				return false;
			} 
		}
		/// <summary>
		/// True if any of the time period aspects has a determinate end time.
		/// </summary>
		public bool HasEndTime { 
			get{
				foreach ( object key in m_filterCriteria ) {
					if ( m_ihti.GetTimePeriodAspect(key).HasEndTime ) return true;
				}
				return false;
			} 
		}
		/// <summary>
		/// True if any of the time period aspects has a determinate duration.
		/// </summary>
		public bool HasDuration {
			get{
				foreach ( object key in m_filterCriteria ) {
					if ( m_ihti.GetTimePeriodAspect(key).HasDuration ) return true;
				}
				return false;
			} 
		}

		public TimeAdjustmentMode AdjustmentMode {
			get { 
				foreach ( object key in m_filterCriteria ) {
					ITimePeriod itro = m_ihti.GetTimePeriodAspect(key);
					return itro.AdjustmentMode;
				}
				return TimeAdjustmentMode.None;
			}
		}

    #region IHasIdentity Members
		public string Name { get { return m_name; } }
		public Guid Guid => m_guid;
		public string Description { get { return m_description; } }
    #endregion

    #region ITimePeriod Members

		DateTime Highpoint.Sage.Scheduling.ITimePeriod.StartTime {
			get {
				foreach ( object key in m_filterCriteria ) {
					if ( m_ihti.GetTimePeriodAspect(key).HasStartTime ) return m_ihti.GetTimePeriodAspect(key).StartTime;
				}
				return DateTime.MinValue;
			}
			set {
				// TODO:  Add TimePeriodAspectSelector.Highpoint.Sage.Scheduling.ITimePeriod.StartTime setter implementation
			}
		}

		DateTime Highpoint.Sage.Scheduling.ITimePeriod.EndTime {
			get {
				foreach ( object key in m_filterCriteria ) {
					if ( m_ihti.GetTimePeriodAspect(key).HasStartTime ) return m_ihti.GetTimePeriodAspect(key).StartTime;
				}
				return DateTime.MinValue;
			}
			set {
				// TODO:  Add TimePeriodAspectSelector.Highpoint.Sage.Scheduling.ITimePeriod.EndTime setter implementation
			}
		}

		TimeSpan Highpoint.Sage.Scheduling.ITimePeriod.Duration {
			get {
				foreach ( object key in m_filterCriteria ) {
					if ( m_ihti.GetTimePeriodAspect(key).HasStartTime ) return m_ihti.GetTimePeriodAspect(key).Duration;
				}
				return TimeSpan.MaxValue;
			}
			set {
				// TODO:  Add TimePeriodAspectSelector.Highpoint.Sage.Scheduling.ITimePeriod.Duration setter implementation
			}
		}

		public void ClearStartTime() {
			// TODO:  Add TimePeriodAspectSelector.ClearStartTime implementation
		}

		public void ClearEndTime() {
			// TODO:  Add TimePeriodAspectSelector.ClearEndTime implementation
		}

		public void ClearDuration() {
			// TODO:  Add TimePeriodAspectSelector.ClearDuration implementation
		}

		Highpoint.Sage.Scheduling.TimeAdjustmentMode Highpoint.Sage.Scheduling.ITimePeriod.AdjustmentMode {
			get {
				foreach ( object key in m_filterCriteria ) {
					if ( m_ihti.GetTimePeriodAspect(key).HasStartTime ) return m_ihti.GetTimePeriodAspect(key).AdjustmentMode;
				}
				return TimeAdjustmentMode.None;
			}
			set {
				// TODO:  Add TimePeriodAspectSelector.Highpoint.Sage.Scheduling.ITimePeriod.AdjustmentMode setter implementation
			}
		}

		public void PushAdjustmentMode(Highpoint.Sage.Scheduling.TimeAdjustmentMode tam) {
			// TODO:  Add TimePeriodAspectSelector.PushAdjustmentMode implementation
		}

		public Highpoint.Sage.Scheduling.TimeAdjustmentMode PopAdjustmentMode() {
			// TODO:  Add TimePeriodAspectSelector.PopAdjustmentMode implementation
			return TimeAdjustmentMode.None;
		}

		public void AddRelationship(Highpoint.Sage.Scheduling.TimePeriod.Relationship relationship, ITimePeriod otherTimePeriod) {
			// TODO:  Add TimePeriodAspectSelector.AddRelationship implementation
		}

        public void RemoveRelationship(TimePeriod.Relationship relationship, ITimePeriod otherTimePeriod) {
            // TODO:  Add TimePeriodAspectSelector.AddRelationship implementation
        }

    #endregion

    #region IObservable Members
#pragma warning disable 67 // Ignore it if this event is not used. It's a framework, and this event may be for clients.
        public event Highpoint.Sage.Utility.ObservableChangeHandler ChangeEvent;
#pragma warning restore 67
    #endregion

    #region ITimePeriod Members

        private IHasIdentity m_subject;
        private object m_modifier;
        public void SetSubjectAndModifier(IHasIdentity subject, object modifier) {
            m_subject = subject;
            m_modifier = modifier;
        }

        public IHasIdentity Subject {
            get {
                return m_subject;
            }
        }

        public object Modifier {
            get {
                return m_modifier;
            }
        }

    #endregion
    }
#endif

    public class TimePeriodAdjustmentException : Exception {
		public TimePeriodAdjustmentException(string msg):base(msg){}
	}
}
