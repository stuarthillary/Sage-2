/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Highpoint.Sage.Scheduling
{
    public class TimePeriodEnvelope : ITimePeriod
    {

        #region Private Fields
        private readonly List<ITimePeriod> _childTimePeriods;
        private readonly Milestone _startMilestone;
        private readonly Milestone _endMilestone;
        private readonly string _name;
        private Guid _guid;
        private readonly string _description = "";
        private static readonly string _default_Name = "TimePeriodEnvelope";
        private ISupportsCorrelation _subject;
        private object _modifier;
        #endregion

        #region Constructors
        public TimePeriodEnvelope() : this(_default_Name, Guid.NewGuid(), true) { }
        public TimePeriodEnvelope(string name, Guid guid) : this(name, guid, true) { }
        public TimePeriodEnvelope(string name, Guid guid, bool supportsReactiveAdjustment)
        {
            _name = name;
            _guid = guid;
            _childTimePeriods = new List<ITimePeriod>();
            _startMilestone = new Milestone(name + ".Start", guid, DateTime.MinValue, false);
            _endMilestone = new Milestone(name + ".End", guid, DateTime.MaxValue, false);

            // These two are always present, and always active, therefore we do not put them in the
            // arraylist of internal (i.e. clearable) relationships. A TimePeriod can NEVER end before it starts.
            if (supportsReactiveAdjustment)
            {
                new MilestoneRelationship_LTE(StartMilestone, EndMilestone);
                new MilestoneRelationship_GTE(EndMilestone, StartMilestone);
            }
        }
        #endregion

        #region Add & Remove Time Periods
        public void AddTimePeriod(ITimePeriod childTimePeriod)
        {
            _childTimePeriods.Add(childTimePeriod);
            childTimePeriod.StartMilestone.ChangeEvent += new ObservableChangeHandler(Milestone_ChangeEvent);
            childTimePeriod.EndMilestone.ChangeEvent += new ObservableChangeHandler(Milestone_ChangeEvent);
            Update();
        }

        public void AddTimePeriods(IEnumerable<ITimePeriod> childTimePeriods)
        {
            foreach (ITimePeriod childTimePeriod in childTimePeriods)
            {
                _childTimePeriods.Add(childTimePeriod);
                childTimePeriod.StartMilestone.ChangeEvent += new ObservableChangeHandler(Milestone_ChangeEvent);
                childTimePeriod.EndMilestone.ChangeEvent += new ObservableChangeHandler(Milestone_ChangeEvent);
            }
            Update();
        }

        public void RemoveTimePeriods(IEnumerable<ITimePeriod> childTimePeriods)
        {
            foreach (ITimePeriod childTimePeriod in childTimePeriods)
            {
                _childTimePeriods.Remove(childTimePeriod);
                childTimePeriod.StartMilestone.ChangeEvent -= new ObservableChangeHandler(Milestone_ChangeEvent);
                childTimePeriod.EndMilestone.ChangeEvent -= new ObservableChangeHandler(Milestone_ChangeEvent);
            }
            Update();
        }

        public void RemoveTimePeriod(ITimePeriod childTimePeriod)
        {
            _childTimePeriods.Remove(childTimePeriod);
            childTimePeriod.StartMilestone.ChangeEvent -= new ObservableChangeHandler(Milestone_ChangeEvent);
            childTimePeriod.EndMilestone.ChangeEvent -= new ObservableChangeHandler(Milestone_ChangeEvent);
            Update();
        }

        #endregion

        private void Update()
        {

            DateTime earliest = DateTime.MaxValue;
            _childTimePeriods.ForEach(delegate (ITimePeriod tp)
            {
                earliest = DateTimeOperations.Min(earliest, tp.StartMilestone.DateTime);
            });
            DateTime latest = DateTime.MinValue;
            _childTimePeriods.ForEach(delegate (ITimePeriod tp)
            {
                latest = DateTimeOperations.Max(latest, tp.EndMilestone.DateTime);
            });

            if (!earliest.Equals(DateTime.MaxValue))
            {
                _startMilestone.MoveTo(earliest);
            }
            if (!latest.Equals(DateTime.MinValue))
            {
                _endMilestone.MoveTo(latest);
            }

            // We will deal with this later. For now, we only support as-is time periods and the
            // only milestones that move are those in TimePeriodEnvelopes - they are set only
            // when a child is added to, or removed from, an envelope.
            /*
			bool hadStartTime = false;
			DateTime earliest = DateTime.MaxValue;
			bool hadEndTime = false;
			DateTime latest = DateTime.MinValue;

			foreach ( ITimePeriod childTimePeriod in m_childTimePeriods ) {
				if ( childTimePeriod.HasStartTime ) {
					hadStartTime = true;
					earliest = DateTimeOperations.Min(earliest,childTimePeriod.StartTime);
                }
				if ( childTimePeriod.HasEndTime ) {
					hadEndTime = true;
					latest = DateTimeOperations.Max(earliest,childTimePeriod.EndTime);
				}
			}

			bool msStartActive = StartMilestone.Active;
			DateTime msStartDateTime = StartMilestone.DateTime;
			bool msEndActive = EndMilestone.Active;
			DateTime msEndDateTime = EndMilestone.DateTime;
			try {
                if (hadStartTime) {
                    StartMilestone.Active = true;
                    StartMilestone.MoveTo(earliest);
                }
                if (hadEndTime) {
                    EndMilestone.Active = hadEndTime;
                    EndMilestone.MoveTo(latest);
                }
			} catch ( MilestoneAdjustmentException mae ) {
				StartMilestone.PushActiveSetting(false);
				EndMilestone.PushActiveSetting(false);
				StartMilestone.MoveTo(msStartDateTime);
				StartMilestone.Active = msStartActive;
				EndMilestone.MoveTo(msEndDateTime);
				EndMilestone.Active = msEndActive;
				StartMilestone.PopActiveSetting();
				EndMilestone.PopActiveSetting();
				throw mae;
			} */

        }

        #region Milestone & Time getters
        /// <summary>
        /// Gets the start time of the time period.
        /// </summary>
        public DateTime StartTime
        {
            get
            {
                return _startMilestone.DateTime;
            }
        }
        /// <summary>
        /// Gets the end time of the time period.
        /// </summary>
        public DateTime EndTime
        {
            get
            {
                return _endMilestone.DateTime;
            }
        }
        /// <summary>
        /// Gets the duration of the time period.
        /// </summary>
        public TimeSpan Duration
        {
            get
            {
                return _endMilestone.DateTime - _startMilestone.DateTime;
            }
        }

        /// <summary>
        /// True if the time period has a determinate start time.
        /// </summary>
        public bool HasStartTime
        {
            get
            {
                return _startMilestone.Active;
            }
        }
        /// <summary>
        /// True if the time period has a determinate end time.
        /// </summary>
        public bool HasEndTime
        {
            get
            {
                return _endMilestone.Active;
            }
        }
        /// <summary>
        /// True if the time period has a determinate duration.
        /// </summary>
        public bool HasDuration
        {
            get
            {
                return _startMilestone.Active && _endMilestone.Active;
            }
        }

        /// <summary>
        /// The milestone that represents the starting of this time period.
        /// </summary>
        public IMilestone StartMilestone
        {
            get
            {
                return _startMilestone;
            }
        }

        /// <summary>
        /// The milestone that represents the ending point of this time period.
        /// </summary>
        public IMilestone EndMilestone
        {
            get
            {
                return _endMilestone;
            }
        }
        #endregion

        #region IHasIdentity Members
        public string Name
        {
            get
            {
                return _name;
            }
        }
        public Guid Guid => _guid;
        public string Description
        {
            get
            {
                return _description;
            }
        }
        #endregion

        /// <summary>
        /// Determines what inferences are to be made about the other two settings when one
        /// of the settings (start, duration, finish times) is changed.
        /// </summary>
        public TimeAdjustmentMode AdjustmentMode
        {
            get
            {
                return TimeAdjustmentMode.InferDuration;
            }
        }

        private void Milestone_ChangeEvent(object whoChanged, object whatChanged, object howChanged)
        {
            Update();
        }

        #region ITimePeriod Members

        DateTime ITimePeriodBase.StartTime
        {
            get
            {
                return _startMilestone.DateTime;
            }
            set
            {
                throw new ApplicationException("TimePeriodEnvelope is Read-only.");
            }
        }

        DateTime ITimePeriodBase.EndTime
        {
            get
            {
                return EndMilestone.DateTime;
            }
            set
            {
                throw new ApplicationException("TimePeriodEnvelope is Read-only.");
            }
        }

        TimeSpan ITimePeriodBase.Duration
        {
            [DebuggerStepThrough]
            get
            {
                return _endMilestone.DateTime - _startMilestone.DateTime;
            }
            set
            {
                throw new ApplicationException("TimePeriodEnvelope is Read-only.");
            }
        }

        public void ClearStartTime()
        {
            throw new ApplicationException("TimePeriodEnvelope is Read-only.");
        }

        public void ClearEndTime()
        {
            throw new ApplicationException("TimePeriodEnvelope is Read-only.");
        }

        public void ClearDuration()
        {
            throw new ApplicationException("TimePeriodEnvelope is Read-only.");
        }

        TimeAdjustmentMode ITimePeriod.AdjustmentMode
        {
            get
            {
                return TimeAdjustmentMode.None;
            }
            set
            {
                throw new ApplicationException("TimePeriodEnvelope is Read-only.");
            }
        }

        public void PushAdjustmentMode(TimeAdjustmentMode tam)
        {
            throw new ApplicationException("TimePeriodEnvelope is Read-only.");
        }

        public TimeAdjustmentMode PopAdjustmentMode()
        {
            throw new ApplicationException("TimePeriodEnvelope is Read-only.");
        }

        public void AddRelationship(TimePeriod.Relationship relationship, ITimePeriod otherTimePeriod)
        {
            throw new ApplicationException("TimePeriodEnvelope is Read-only.");
        }

        public void RemoveRelationship(TimePeriod.Relationship relationship, ITimePeriod otherTimePeriod)
        {
            throw new ApplicationException("TimePeriodEnvelope is Read-only.");
        }

        public ISupportsCorrelation Subject
        {
            get
            {
                return _subject;
            }
            set
            {
                _subject = value;
            }
        }

        public object Modifier
        {
            get
            {
                return _modifier;
            }
            set
            {
                _modifier = value;
            }
        }

        #endregion

        /// <summary>
        /// Returns an iterator that traverses the descendant payloads breadth first.
        /// </summary>
        /// <value>The descendant payloads iterator.</value>
        public IEnumerable<ITimePeriod> BreadthFirstEnumerable => new Enumerable<ITimePeriod>(GetBreadthFirstEnumerator());

        private IEnumerator<ITimePeriod> GetBreadthFirstEnumerator()
        {
            Queue<ITimePeriod> todo = new Queue<ITimePeriod>();
            todo.Enqueue(this);
            while (todo.Count > 0)
            {
                ITimePeriod tp = todo.Dequeue();
                if (tp is TimePeriodEnvelope)
                {
                    ((TimePeriodEnvelope)tp).Children.ForEach(delegate (ITimePeriod kid)
                    {
                        todo.Enqueue(kid);
                    });
                }
                yield return tp;
            }
        }

        /// <summary>
        /// Returns an iterator that traverses the descendant payloads depth first.
        /// </summary>
        /// <value>The descendant payloads iterator.</value>
        public IEnumerable<ITimePeriod> DepthFirstEnumerable
        {
            get
            {
                return new Enumerable<ITimePeriod>(GetDepthFirstEnumerator());
            }
        }

        private IEnumerator<ITimePeriod> GetDepthFirstEnumerator()
        {
            yield return this;
            foreach (ITimePeriod kid in Children)
            {
                if (kid is TimePeriodEnvelope)
                {
                    IEnumerator<ITimePeriod> tpe = ((TimePeriodEnvelope)kid).GetDepthFirstEnumerator();
                    while (tpe.MoveNext())
                    {
                        yield return tpe.Current;
                    }
                }
                else
                {
                    yield return kid;
                }
            }
        }

        /// <summary>
        /// Gets the list of children. Do not modify this.
        /// </summary>
        /// <value>The children.</value>
        public List<ITimePeriod> Children
        {
            get
            {
                return _childTimePeriods;
            }
        }

        public override string ToString()
        {
            return string.Format("{0}{1} [{2}->{3}->{4}]",
                _subject == null ? "" : _subject.Name,
                _modifier == null ? "" : "(" + _modifier + ")",
                StartMilestone.Active ? StartMilestone.DateTime.ToString() : "??/??/???? ??:??:?? ?M",
                ((StartMilestone.Active && EndMilestone.Active) ? Duration.ToString() : "--:--:--"),
                EndMilestone.Active ? EndMilestone.DateTime.ToString() : "??/??/???? ??:??:?? ?M");
        }

        #region IObservable Members

#pragma warning disable 67 // Ignore it if this event is not used. It's a framework, and this event may be for clients.
        public event ObservableChangeHandler ChangeEvent;
#pragma warning restore 67
        #endregion

        public static string ToString(ITimePeriod root, TimePeriodSorter sortChildrenBy)
        {
            StringBuilder sb = new StringBuilder();
            ToString(ref sb, root, sortChildrenBy, 0);
            return sb.ToString();
        }

        private static void ToString(ref StringBuilder sb, ITimePeriod itp, TimePeriodSorter sortChildrenBy, int depth)
        {
            string indent = StringOperations.Spaces(depth * 4);
            if (itp is TimePeriodEnvelope)
            {
                TimePeriodEnvelope tpe = (TimePeriodEnvelope)itp;
                sb.Append(string.Format("{0}<TimePeriodEnvelope subject=\"{1}\"modifier=\"{2}\" start=\"{3}\" end=\"{4}\">\r\n",
                    indent,
                    tpe.Subject.Name,
                    tpe.Modifier,
                    tpe.StartTime,
                    tpe.EndTime));

                tpe._childTimePeriods.Sort(TimePeriodSorter.ByIncreasingStartTime);
                foreach (ITimePeriod child in tpe._childTimePeriods)
                {
                    ToString(ref sb, child, sortChildrenBy, depth + 1);
                }
                sb.Append(string.Format("{0}</TimePeriodEnvelope>\r\n", indent));
            }
            else if (itp is TimePeriod)
            {
                TimePeriod tp = (TimePeriod)itp;
                sb.Append(string.Format("{0}<TimePeriodEnvelope subject=\"{1}\"modifier=\"{2}\" start=\"{3}\" end=\"{4}\" duration=\"{5}\">\r\n",
                    indent,
                    tp.Subject.Name,
                    tp.Modifier,
                    tp.StartTime,
                    tp.Duration,
                    tp.EndTime));
            }
        }
    }

    /// <summary>
    /// Class Enumerable provides a wrapper for an IEnumerable of T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="System.Collections.Generic.IEnumerable{T}" />
    class Enumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerator<T> _enumerator;
        public Enumerable(IEnumerator<T> enumerator)
        {
            _enumerator = enumerator;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _enumerator;
        }
    }
}
