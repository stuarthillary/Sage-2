/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Highpoint.Sage.Scheduling
{

    public delegate void TimePeriodChange(ITimePeriod who, TimePeriod.ChangeType howChanged);

    public class TimePeriod : ITimePeriod
    {

        public enum ChangeType { StartTime, Duration, EndTime, All };
        public enum Relationship
        {
            StartsBeforeStartOf,
            StartsOnStartOf,
            StartsAfterStartOf,
            StartsBeforeEndOf,
            StartsOnEndOf,
            StartsAfterEndOf,
            EndsBeforeStartOf,
            EndsOnStartOf,
            EndsAfterStartOf,
            EndsBeforeEndOf,
            EndsOnEndOf,
            EndsAfterEndOf
        }

        #region Private Fields
        private static readonly bool _diagnostics = Diagnostics.DiagnosticAids.Diagnostics("TimePeriod");
        private readonly bool _supportsReactiveAdjustment = true;
        private readonly Milestone _startMilestone;
        private readonly Milestone _endMilestone;
        private TimeSpan _duration;
        private bool _hasDuration;
        private TimeAdjustmentMode _adjustmentMode;
        private Stack _adjustmentModeStack;
        private ArrayList _adjustmentModeRelationships;
        private readonly string _name;
        private Guid _guid;
        private readonly string _description = "";
        private static readonly string _default_Name = "TimePeriod";
        private MilestoneRelationship_Strut _inferenceRelationship;
        private enum MsRel
        {
            Before, On, After
        }
        private ISupportsCorrelation m_subject;
        private object m_modifier;
        #endregion

        #region Constructors
        public TimePeriod(DateTime startTime, DateTime endTime, TimeAdjustmentMode adjustmentMode)
            : this(_default_Name, Guid.NewGuid(), startTime, endTime, adjustmentMode, true) { }

        public TimePeriod(DateTime startTime, DateTime endTime, TimeAdjustmentMode adjustmentMode, bool supportsReactiveAdjustment)
            : this(_default_Name, Guid.NewGuid(), startTime, endTime, adjustmentMode, supportsReactiveAdjustment) { }

        public TimePeriod(string name, Guid guid, DateTime startTime, DateTime endTime, TimeAdjustmentMode adjustmentMode)
            : this(name, guid, startTime, endTime, adjustmentMode, true) { }

        public TimePeriod(string name, Guid guid, DateTime startTime, DateTime endTime, TimeAdjustmentMode adjustmentMode, bool supportsReactiveAdjustment)
        {
            _name = name;
            _guid = guid;
            _startMilestone = new Milestone(_name + ".Start", Guid.NewGuid(), startTime);
            _endMilestone = new Milestone(_name + ".End", Guid.NewGuid(), endTime);
            _duration = endTime - startTime;
            _hasDuration = true;
            _supportsReactiveAdjustment = supportsReactiveAdjustment;
            Init();
            AdjustmentMode = adjustmentMode;
        }

        public TimePeriod(DateTime startTime, TimeSpan duration, TimeAdjustmentMode adjustmentMode)
            : this(_default_Name, Guid.NewGuid(), startTime, duration, adjustmentMode) { }

        public TimePeriod(string name, Guid guid, DateTime startTime, TimeSpan duration, TimeAdjustmentMode adjustmentMode)
        {
            _name = name;
            _guid = guid;
            _startMilestone = new Milestone(_name + ".Start", Guid.NewGuid(), startTime);
            _endMilestone = new Milestone(_name + ".End", Guid.NewGuid(), startTime + duration);
            _duration = duration;
            _hasDuration = true;
            Init();
            AdjustmentMode = adjustmentMode;
        }

        public TimePeriod(TimeSpan duration, DateTime endTime, TimeAdjustmentMode adjustmentMode)
            : this(_default_Name, Guid.NewGuid(), duration, endTime, adjustmentMode) { }
        public TimePeriod(string name, Guid guid, TimeSpan duration, DateTime endTime, TimeAdjustmentMode adjustmentMode)
        {
            _name = name;
            _guid = guid;
            _startMilestone = new Milestone(_name + ".Start", Guid.NewGuid(), endTime - duration);
            _endMilestone = new Milestone(_name + ".End", Guid.NewGuid(), endTime);
            _duration = duration;
            _hasDuration = true;
            Init();
            AdjustmentMode = adjustmentMode;
        }


        public TimePeriod(TimeAdjustmentMode adjustmentMode)
            : this(_default_Name, Guid.NewGuid(), adjustmentMode) { }
        public TimePeriod(string name, Guid guid, TimeAdjustmentMode adjustmentMode)
        {
            _name = name;
            _guid = guid;
            _startMilestone = new Milestone(_name + ".Start", Guid.NewGuid(), DateTime.MinValue, false);
            _endMilestone = new Milestone(_name + ".End", Guid.NewGuid(), DateTime.MaxValue, false);
            _duration = TimeSpan.MaxValue;
            _hasDuration = false;
            Init();
            AdjustmentMode = adjustmentMode;
        }

        private void Init()
        {
            if (_supportsReactiveAdjustment)
            {
                _adjustmentModeStack = new Stack();
                _adjustmentModeRelationships = new ArrayList();

                // These two are always present, and always active, therefore we do not put them in the
                // arraylist of internal (i.e. clearable) relationships. A TimePeriod can NEVER end before it starts.
                MilestoneRelationship mr1 = new MilestoneRelationship_LTE(StartMilestone, EndMilestone);
                MilestoneRelationship mr2 = new MilestoneRelationship_GTE(EndMilestone, StartMilestone);
                mr1.AddReciprocal(mr2);
                mr2.AddReciprocal(mr1);
            }
        }
        #endregion

        #region Adjustment Mode Management
        /// <summary>
        /// This property determines how the triad of start, duration &amp; finish are kept up-to-date
        /// as individual properties are set and changed.
        /// </summary>
        public TimeAdjustmentMode AdjustmentMode
        {
            get
            {
                return _adjustmentMode;
            }
            set
            {
                if (_supportsReactiveAdjustment)
                {
                    // Clear existing relationships.
                    foreach (MilestoneRelationship mr in _adjustmentModeRelationships)
                    {
                        mr.Detach();
                    }

                    _inferenceRelationship = null;
                    MilestoneRelationship relationship;
                    switch (value)
                    {
                        case TimeAdjustmentMode.None:
                            break;

                        case TimeAdjustmentMode.FixedStart:
                            relationship = new MilestoneRelationship_Pin(null, StartMilestone);
                            _adjustmentModeRelationships.Add(relationship);
                            break;

                        case TimeAdjustmentMode.FixedDuration:
                            MilestoneRelationship fwd = new MilestoneRelationship_Strut(StartMilestone, EndMilestone);
                            _adjustmentModeRelationships.Add(fwd);
                            MilestoneRelationship rev = new MilestoneRelationship_Strut(EndMilestone, StartMilestone);
                            _adjustmentModeRelationships.Add(rev);
                            fwd.AddReciprocal(rev);
                            rev.AddReciprocal(fwd);
                            break;

                        case TimeAdjustmentMode.FixedEnd:
                            relationship = new MilestoneRelationship_Pin(null, EndMilestone);
                            _adjustmentModeRelationships.Add(relationship);

                            break;
                        case TimeAdjustmentMode.InferStartTime:
                            _inferenceRelationship = new MilestoneRelationship_Strut(StartMilestone, EndMilestone);
                            relationship = _inferenceRelationship;
                            _adjustmentModeRelationships.Add(relationship);
                            break;

                        case TimeAdjustmentMode.InferDuration:
                            break;

                        case TimeAdjustmentMode.InferEndTime:
                            _inferenceRelationship = new MilestoneRelationship_Strut(EndMilestone, StartMilestone);
                            relationship = _inferenceRelationship;
                            _adjustmentModeRelationships.Add(relationship);
                            break;

                        case TimeAdjustmentMode.Locked:
                            relationship = new MilestoneRelationship_Pin(null, StartMilestone);
                            _adjustmentModeRelationships.Add(relationship);
                            relationship = new MilestoneRelationship_Pin(null, EndMilestone);
                            _adjustmentModeRelationships.Add(relationship);
                            break;
                    }
                }
                _adjustmentMode = value;

                //				_Debug.WriteLine("In " + this.Name + " mode was just set to " + m_adjustmentMode.ToString() +".");
                //				_Debug.WriteLine("Adjustment mode Relationships are ...");
                //				foreach ( MilestoneRelationship mr in m_adjustmentModeRelationships ) _Debug.WriteLine("\t" + mr.ToString());
            }
        }

        /// <summary>
        /// Pushes the current time period adjustment mode onto a stack, substituting a provided mode. This
        /// must be paired with a corresponding Pop operation.
        /// </summary>
        /// <param name="tam">The time period adjustment mode that is to temporarily take the place of the current one.</param>
        public void PushAdjustmentMode(TimeAdjustmentMode tam)
        {
            _adjustmentModeStack.Push(_adjustmentMode);
            AdjustmentMode = tam;
        }

        /// <summary>
        /// Pops the previous time period adjustment mode off a stack, and sets this Time Period's adjustment mode to that value.
        /// </summary>
        /// <returns>The newly-popped time period adjustment mode.</returns>
        public TimeAdjustmentMode PopAdjustmentMode()
        {
            AdjustmentMode = (TimeAdjustmentMode)_adjustmentModeStack.Pop();
            return AdjustmentMode;
        }
        #endregion

        private void GetRelationshipParameters(Relationship relationship, ITimePeriod otherTimePeriod, out IMilestone a, out MsRel m, out IMilestone b)
        {
            switch (relationship)
            {
                case Relationship.StartsBeforeStartOf:
                    a = StartMilestone;
                    m = MsRel.Before;
                    b = otherTimePeriod.StartMilestone;
                    break;
                case Relationship.StartsOnStartOf:
                    a = StartMilestone;
                    m = MsRel.On;
                    b = otherTimePeriod.StartMilestone;
                    break;
                case Relationship.StartsAfterStartOf:
                    a = StartMilestone;
                    m = MsRel.After;
                    b = otherTimePeriod.StartMilestone;
                    break;
                case Relationship.StartsBeforeEndOf:
                    a = StartMilestone;
                    m = MsRel.Before;
                    b = otherTimePeriod.EndMilestone;
                    break;
                case Relationship.StartsOnEndOf:
                    a = StartMilestone;
                    m = MsRel.On;
                    b = otherTimePeriod.EndMilestone;
                    break;
                case Relationship.StartsAfterEndOf:
                    a = StartMilestone;
                    m = MsRel.After;
                    b = otherTimePeriod.EndMilestone;
                    break;
                case Relationship.EndsBeforeStartOf:
                    a = EndMilestone;
                    m = MsRel.Before;
                    b = otherTimePeriod.StartMilestone;
                    break;
                case Relationship.EndsOnStartOf:
                    a = EndMilestone;
                    m = MsRel.On;
                    b = otherTimePeriod.StartMilestone;
                    break;
                case Relationship.EndsAfterStartOf:
                    a = EndMilestone;
                    m = MsRel.After;
                    b = otherTimePeriod.StartMilestone;
                    break;
                case Relationship.EndsBeforeEndOf:
                    a = EndMilestone;
                    m = MsRel.Before;
                    b = otherTimePeriod.EndMilestone;
                    break;
                case Relationship.EndsOnEndOf:
                    a = EndMilestone;
                    m = MsRel.On;
                    b = otherTimePeriod.EndMilestone;
                    break;
                case Relationship.EndsAfterEndOf:
                    a = EndMilestone;
                    m = MsRel.After;
                    b = otherTimePeriod.EndMilestone;
                    break;
                default:
                    a = null;
                    b = null;
                    m = MsRel.On;
                    throw new ApplicationException("Error - unrecognized TimePeriod relationship " + relationship + " referenced in " + Name);
            }
        }

        public void AddRelationship(Relationship relationship, ITimePeriod otherTimePeriod)
        {

            if (_supportsReactiveAdjustment)
            {

                IMilestone a, b;
                MsRel m;
                GetRelationshipParameters(relationship, otherTimePeriod, out a, out m, out b);

                MilestoneRelationship mr1, mr2;
                switch (m)
                {
                    case MsRel.Before:
                        mr1 = new MilestoneRelationship_LTE(a, b);
                        mr2 = new MilestoneRelationship_GTE(b, a);
                        //a.AddRelationship(mr);
                        break;
                    case MsRel.On:
                        mr1 = new MilestoneRelationship_Strut(a, b);
                        mr2 = new MilestoneRelationship_Strut(b, a);
                        //a.AddRelationship(mr);
                        break;
                    case MsRel.After:
                        mr1 = new MilestoneRelationship_GTE(a, b);
                        mr2 = new MilestoneRelationship_LTE(b, a);
                        //a.AddRelationship(mr);
                        break;
                    default:
                        throw new ApplicationException("Error - unrecognized MS_REL " + m + " referenced in " + Name);
                }
                mr2.AddReciprocal(mr1);
                mr1.AddReciprocal(mr2);
                //			_Debug.WriteLine("External relationship added to " + this.Name + " : \"" + mr1.ToString() + "\".");
                //			_Debug.WriteLine("\t+recip relationship added to " + this.Name + " : \"" + mr2.ToString() + "\".");

            }
            else
            {
                throw new ApplicationException("Trying to add relationships to a TimePeriod that does not support reactive adjustment.");
            }
        }

        public void RemoveRelationship(Relationship relationship, ITimePeriod otherTimePeriod)
        {
            //IMilestone a, b;
            //MS_REL m;
            //GetRelationshipParameters(relationship, otherTimePeriod, out a, out m, out b);

            //MilestoneRelationship mr1, mr2;
            //switch (m) {
            //    case MS_REL.Before:
            //        foreach (MilestoneRelationship mr in a.Relationships) {
            //            //if ( 
            //        }
            //        mr1 = new MilestoneRelationship_LTE(a, b);
            //        mr2 = mr1.Reciprocal;
            //        //a.AddRelationship(mr);
            //        break;
            //    case MS_REL.On:
            //        mr1 = new MilestoneRelationship_Strut(a, b);
            //        mr2 = mr1.Reciprocal;
            //        //a.AddRelationship(mr);
            //        break;
            //    case MS_REL.After:
            //        mr1 = new MilestoneRelationship_GTE(a, b);
            //        mr2 = mr1.Reciprocal;
            //        //a.AddRelationship(mr);
            //        break;
            //    default:
            //        throw new ApplicationException("Error - unrecognized MS_REL " + m.ToString() + " referenced in " + this.Name);
            //}
            //mr2.Reciprocals.Remove(mr1);
            //mr1.Reciprocals.Remove(mr2);
            throw new NotImplementedException();
        }

        #region Milestones
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

        #region StartTime, Duration and EndTime
        /// <summary>
        /// Reads and writes the Start Time. When writing the start time, leaves the
        /// end time fixed, and adjusts duration.
        /// </summary>
        public virtual DateTime StartTime
        {
            get
            {
                return _startMilestone.DateTime;
            }
            set
            {
                DateTime was = StartMilestone.DateTime;
                if (value.Equals(was) && StartMilestone.Active)
                    return;
                try
                {
                    //_Debug.WriteLine("Trying to move start of " + this.Name + " to " + value.ToString());
                    StartMilestone.MoveTo(value);
                    if (ChangeEvent != null)
                        ChangeEvent(this, ChangeType.StartTime, null);
                }
                catch (MilestoneAdjustmentException mae)
                {
                    StartMilestone.PushActiveSetting(false);
                    StartMilestone.MoveTo(was);
                    StartMilestone.PopActiveSetting();
                    throw mae;
                }
            }
        }

        /// <summary>
        /// Reads and writes the End Time. When writing the end time, leaves the
        /// start time fixed, and adjusts duration.
        /// </summary>
        public virtual DateTime EndTime
        {
            get
            {
                return EndMilestone.DateTime;
            }
            set
            {
                DateTime was = EndMilestone.DateTime;
                if (value.Equals(was) && EndMilestone.Active)
                    return;
                try
                {
                    EndMilestone.MoveTo(value);
                    if (ChangeEvent != null)
                        ChangeEvent(this, ChangeType.EndTime, null);
                }
                catch (MilestoneAdjustmentException mae)
                {
                    EndMilestone.PushActiveSetting(false);
                    EndMilestone.MoveTo(was);
                    EndMilestone.PopActiveSetting();
                    throw mae;
                }
            }
        }

        /// <summary>
        /// Gets the duration of the time period.
        /// </summary>
        public virtual TimeSpan Duration
        {
            get
            {
                if (_startMilestone != null && _endMilestone != null && _startMilestone.Active && _endMilestone.Active)
                {
                    return (_endMilestone.DateTime - _startMilestone.DateTime);
                }
                else
                {
                    return _duration;
                }
            }
            set
            {
                DateTime startWas = StartMilestone.DateTime;
                DateTime endWas = EndMilestone.DateTime;
                _hasDuration = true;
                if (value.Equals(endWas - startWas))
                    return;
                try
                {

                    switch (_adjustmentMode)
                    {
                        case TimeAdjustmentMode.None:
                            _duration = value;
                            break;

                        case TimeAdjustmentMode.FixedStart:
                            EndMilestone.MoveTo(StartMilestone.DateTime + value);
                            break;

                        case TimeAdjustmentMode.InferEndTime:
                            _inferenceRelationship.Delta = value;
                            break;

                        case TimeAdjustmentMode.FixedDuration:
                        case TimeAdjustmentMode.InferDuration:
                        case TimeAdjustmentMode.Locked:
                            throw new TimePeriodAdjustmentException("Cannot adjust duration when Time Period is set to " + _adjustmentMode + " adjustment mode.");
                        //break;

                        case TimeAdjustmentMode.FixedEnd:
                            StartMilestone.MoveTo(EndMilestone.DateTime - value);
                            break;

                        case TimeAdjustmentMode.InferStartTime:
                            _inferenceRelationship.Delta = -value;
                            break;

                        default:
                            throw new ApplicationException("Unrecognized TimeAdjustmentMode specified - " + _adjustmentMode + ".");
                    }

                    if (ChangeEvent != null)
                        ChangeEvent(this, ChangeType.Duration, null);

                }
                catch (MilestoneAdjustmentException mae)
                {
                    StartMilestone.PushActiveSetting(false);
                    EndMilestone.PushActiveSetting(false);
                    StartMilestone.MoveTo(startWas);
                    EndMilestone.MoveTo(endWas);
                    StartMilestone.PopActiveSetting();
                    EndMilestone.PopActiveSetting();
                    throw mae;
                }
            }
        }


        /// <summary>
        /// True if the time period has a determinate start time.
        /// </summary>
        public bool HasStartTime
        {
            get
            {
                return StartMilestone.Active;
            }
        }
        /// <summary>
        /// True if the time period has a determinate end time.
        /// </summary>
        public bool HasEndTime
        {
            get
            {
                return EndMilestone.Active;
            }
        }
        /// <summary>
        /// True if the time period has a determinate duration.
        /// </summary>
        public bool HasDuration
        {
            get
            {
                return _hasDuration;
            }
        }

        /// <summary>
        /// Sets the start time to an indeterminate time.
        /// </summary>
        public void ClearStartTime()
        {
            StartMilestone.Active = false;
            StartMilestone.MoveTo(DateTime.MaxValue);
            if (ChangeEvent != null)
                ChangeEvent(this, ChangeType.StartTime, null);
        }
        /// <summary>
        /// Sets the end time to an indeterminate time.
        /// </summary>
        public void ClearEndTime()
        {
            EndMilestone.Active = false;
            EndMilestone.MoveTo(DateTime.MaxValue);
            if (ChangeEvent != null)
                ChangeEvent(this, ChangeType.EndTime, null);
        }

        /// <summary>
        /// Sets the duration to an indeterminate timespan.
        /// </summary>
        public void ClearDuration()
        {
            _hasDuration = false;
            _duration = TimeSpan.MaxValue;
            if (ChangeEvent != null)
                ChangeEvent(this, ChangeType.Duration, null);
        }
        #endregion

        public event ObservableChangeHandler ChangeEvent;

        public override string ToString()
        {
            return string.Format("{0}{1} [{2}->{3}->{4}]",
                m_subject == null ? "" : m_subject.Name,
                m_modifier == null ? "" : "(" + m_modifier + ")",
                StartMilestone.Active ? StartMilestone.DateTime.ToString() : "??/??/???? ??:??:?? ?M",
                ((StartMilestone.Active && EndMilestone.Active) ? Duration.ToString() : "--:--:--"),
                EndMilestone.Active ? EndMilestone.DateTime.ToString() : "??/??/???? ??:??:?? ?M");
        }

        public static TimePeriod operator +(TimePeriod a, TimePeriod b)
        {

            DateTime startTime = DateTime.MaxValue;
            bool hasStartTime = a.HasStartTime || b.HasStartTime;
            if (hasStartTime)
            {
                if (a.HasStartTime)
                    startTime = DateTimeOperations.Min(startTime, a.StartTime);
                if (b.HasStartTime)
                    startTime = DateTimeOperations.Min(startTime, b.StartTime);
            }

            DateTime endTime = DateTime.MaxValue;
            bool hasEndTime = a.HasEndTime || b.HasEndTime;
            if (hasEndTime)
            {
                if (a.HasEndTime)
                    endTime = DateTimeOperations.Max(endTime, a.EndTime);
                if (b.HasEndTime)
                    endTime = DateTimeOperations.Max(endTime, b.EndTime);
            }

            // We will infer duration.
            bool hasDuration = hasStartTime && hasEndTime;

            // If both adjustment modes match, we will use that mode. Otherwise, we default to "None".
            TimeAdjustmentMode tam = TimeAdjustmentMode.None;
            if (a.AdjustmentMode.Equals(b.AdjustmentMode))
                tam = a.AdjustmentMode;

            TimePeriod tp = new TimePeriod(TimeAdjustmentMode.None);

            if (hasStartTime)
                tp.StartTime = startTime;
            if (hasEndTime)
                tp.EndTime = endTime;
            if (hasDuration)
                tp.Duration = endTime - startTime;

            tp.AdjustmentMode = tam;

            return tp;
        }

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

        #region ITimePeriod Members

        public ISupportsCorrelation Subject
        {
            [DebuggerStepThrough]
            get
            {
                return m_subject;
            }
            [DebuggerStepThrough]
            set
            {
                m_subject = value;
            }
        }

        public object Modifier
        {
            [DebuggerStepThrough]
            get
            {
                return m_modifier;
            }
            [DebuggerStepThrough]
            set
            {
                m_modifier = value;
            }
        }
        #endregion

        internal IEnumerator<ITimePeriod> GetDepthFirstEnumerator()
        {
            yield return this;
        }
        internal IEnumerator<ITimePeriod> GetBreadthFirstEnumerator()
        {
            yield return this;
        }
    }


}
