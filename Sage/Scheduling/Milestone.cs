/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Scheduling
{
    /// <summary>
    /// Summary description for Milestone.
    /// </summary>
    public class Milestone : IMilestone, IObservable
    {

        public enum ChangeType
        {
            Set,
            Enabled
        }

        #region Private Fields
        private DateTime _dateTime;
        private readonly string _name = null;
        private Guid _guid = Guid.Empty;
        private readonly string _description = null;
        private List<MilestoneRelationship> _relationships;
        private Stack _activeStack;
        private readonly bool _isActive;
        private readonly MilestoneMovementManager _movementManager = null;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates and initializes a new simple instance of the <see cref="Milestone"/> class set to a specific date &amp; time.
        /// </summary>
        /// <param name="dateTime">The date &amp; time.</param>
        public Milestone(DateTime dateTime) : this("Milestone", Guid.NewGuid(), dateTime) { }

        /// <summary>
        /// Creates and initializes a new simple instance of the <see cref="Milestone"/> class set to a specific date &amp; time, and
        /// with a specified name and guid.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="guid">The GUID.</param>
        /// <param name="dateTime">The date time.</param>
		public Milestone(string name, Guid guid, DateTime dateTime) : this(name, guid, dateTime, true) { }

        /// <summary>
        /// Creates and initializes a new simple instance of the <see cref="Milestone"/> class set to a specific date &amp; time, and
        /// with a specified name and guid. This constructor also allows the newly created milestone to be initially active.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="guid">The GUID.</param>
        /// <param name="dateTime">The date time.</param>
        /// <param name="active">if set to <c>true</c> this milestone is initially active.</param>
        public Milestone(string name, Guid guid, DateTime dateTime, bool active)
        {
            _name = name;
            _guid = guid;
            _dateTime = dateTime;
            _activeStack = null;
            _relationships = null;
            _isActive = active;
        }
        #endregion

        #region Relationship Management
        /// <summary>
        /// Gets the relationships that involve this milestone.
        /// </summary>
        /// <value>The relationships.</value>
        public List<MilestoneRelationship> Relationships
        {
            get
            {
                if (_relationships == null)
                {
                    _relationships = new List<MilestoneRelationship>();
                }
                return _relationships;
            }
        }

        public void AddRelationship(MilestoneRelationship relationship)
        {
            #region Error Checking
            if (_movementManager != null)
            {
                throw new ApplicationException("Cannot add or remove relationships while a network change is being processed.");
            }
            if (relationship.Dependent != this && relationship.Independent != this)
            {
                throw new ApplicationException("Cannot add a relationship to this milestone that does not involve it.");
            }
            if (Relationships.Contains(relationship))
            {
                return;
            }
            #endregion
            Relationships.Add(relationship);
        }

        public void RemoveRelationship(MilestoneRelationship relationship)
        {
            #region Error Checking
            if (_movementManager != null)
            {
                throw new ApplicationException("Cannot add or remove relationships while a network change is being processed.");
            }
            #endregion
            Relationships.Remove(relationship);
        }
        #endregion

        #region Movement
        /// <summary>
        /// Moves the time of this milestone to the specified new DateTime.
        /// </summary>
        /// <param name="newDateTime">The new date time.</param>
        public void MoveTo(DateTime newDateTime)
        {
            Active = true;
            MilestoneMovementManager.Adjust(this, newDateTime);
        }

        /// <summary>
        /// Moves the time of this milestone by the amount of time specified.
        /// </summary>
        /// <param name="delta">The delta.</param>
		public void MoveBy(TimeSpan delta)
        {
            MilestoneMovementManager.Adjust(this, _dateTime + delta);
        }
        #endregion

        #region Active state management

        public Stack ActiveStack
        {
            get
            {
                if (_activeStack == null)
                {
                    _activeStack = new Stack();
                    _activeStack.Push(_isActive);
                }
                return _activeStack;
            }
        }

        public bool Active
        {
            get
            {
                return (bool)ActiveStack.Peek();
            }

            set
            {
                bool was = (bool)ActiveStack.Pop();
                ActiveStack.Push(value);
                if (was != value)
                    NotifyEnabledChanged();
            }
        }


        public void PushActiveSetting(bool newSetting)
        {
            bool was = (bool)ActiveStack.Peek();
            ActiveStack.Push(newSetting);
            if (was != newSetting)
                NotifyEnabledChanged();
        }

        public void PopActiveSetting()
        {
            bool was = (bool)ActiveStack.Pop();
            bool newSetting = (bool)ActiveStack.Peek();
            if (was != newSetting)
                NotifyEnabledChanged();

        }
        #endregion

        /// <summary>
        /// Gets the date &amp; time of this milestone.
        /// </summary>
        /// <value>The date time.</value>
        public DateTime DateTime
        {
            [DebuggerStepThrough]
            get
            {
                return _dateTime;
            }
        }

        #region IObservable Members and support methods.

        private void NotifyValueChanged(DateTime oldValue)
        {
            ChangeEvent?.Invoke(this, ChangeType.Set, oldValue);
        }

        private void NotifyEnabledChanged()
        {
            ChangeEvent?.Invoke(this, ChangeType.Enabled, _activeStack.Peek());
        }

        public event ObservableChangeHandler ChangeEvent;

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

        public override string ToString()
        {
            return (((_name == null || _name.Length == 0) ? ("Milestone : ") : (_name + " : ")) + _dateTime);
        }

        public class MilestoneMovementManager
        {
            private static object _lock = new object();
            public static void Adjust(Milestone prospectiveMover, DateTime newValue)
            {
                if (!prospectiveMover._dateTime.Equals(newValue))
                {
                    DateTime oldValue = prospectiveMover._dateTime;
                    prospectiveMover._dateTime = newValue;
                    prospectiveMover.NotifyValueChanged(oldValue);
                }
            }

            private static DateTime GetClosestDateTime(DateTime minDateTime, DateTime maxDateTime, DateTime proposedDateTime)
            {
                Debug.Assert(minDateTime <= maxDateTime, "GetClosestDateTime was passed a mnDateTime that was greater than the maxDateTime it was passed.");
                if (proposedDateTime < minDateTime)
                {
                    return minDateTime;
                }
                if (proposedDateTime > maxDateTime)
                {
                    return maxDateTime;
                }
                return proposedDateTime;
            }

        }

        /// <summary>
        /// The MilestoneMovementManager class is responsible for moving a requested milestone, and performing all inferred resultant movements.
        /// </summary>
		private class _MilestoneMovementManager
        {

            private static bool _debug = true;

            #region Private Fields
            private static readonly object _lock = new object();
            private static Hashtable _oldValues = new Hashtable();
            private static Stack _pushedDisablings = new Stack();
            private static Queue _changedMilestones = new Queue();
            #endregion

            public static void Adjust(Milestone prospectiveMover, DateTime newValue)
            {
                if (prospectiveMover._dateTime.Equals(newValue))
                    return;
                lock (_lock)
                {
                    if (_debug)
                        _Debug.WriteLine("Attempting to coordinate correct movement of " + prospectiveMover.Name + " from " + prospectiveMover.DateTime + " to " + newValue + ".");

                    // Change, and then enqueue the first milestone.
                    _oldValues.Add(prospectiveMover, prospectiveMover.DateTime);
                    _changedMilestones.Enqueue(prospectiveMover);

                    while (prospectiveMover._dateTime != newValue)
                    { // TODO: This is a SLEDGEHAMMER. Figure out why root changes don't always hold.
                        prospectiveMover._dateTime = newValue;
                        // Propagate the change downstream (including any resultant changes.)
                        Propagate();
                    }

                    // Finally, tell each changed milestone to fire it's change event.
                    foreach (Milestone changed in _oldValues.Keys)
                        changed.NotifyValueChanged((DateTime)_oldValues[changed]);

                    // And reset the data structures for the next use.
                    _oldValues.Clear();
                    while (_pushedDisablings.Count > 0)
                        ((MilestoneRelationship)(_pushedDisablings.Pop())).PopEnabled();
                    _changedMilestones.Clear();
                }
            }

            private static void Propagate()
            {
                while (_changedMilestones.Count > 0)
                {
                    Milestone ms = (Milestone)_changedMilestones.Dequeue();
                    if (_debug)
                        _Debug.WriteLine("\tPerforming propagation of change to " + ms.Name);

                    #region Create a Hashtable of Lists - key is target Milestone, list contains relationships to that ms.
                    Hashtable htol = new Hashtable();
                    foreach (MilestoneRelationship mr in ms.Relationships)
                    {
                        if (!mr.Enabled)
                            continue;              // Only enabled relationships can effect change.
                        if (mr.Dependent.Equals(ms))
                            continue;  // Only relationships where we are the independent can effect change.
                                       //if ( m_debug ) _Debug.WriteLine("\tConsidering " + mr.ToString());
                        if (!htol.Contains(mr.Dependent))
                            htol.Add(mr.Dependent, new ArrayList());
                        ((ArrayList)htol[mr.Dependent]).Add(mr);  // We now have outbounds, grouped by destination milestone.
                    }
                    #endregion

                    //if ( m_debug ) _Debug.WriteLine("\tPerforming change assessments for relationships that depend on " + ms.Name);

                    // For each 'other' milestone with which this milestone has a relationship, we will
                    // handle all of the relationships that this ms has with that one, as a group.
                    bool fullData = false;
                    foreach (Milestone target in htol.Keys)
                    {
                        if (_debug)
                        {
                            _Debug.WriteLine("\t\tReconciling all relationships between " + ms.Name + " and " + target.Name);
                            // E : RCV Liquid1.Xfer-In.Start and E : RCV Liquid1.Xfer-In.End

                        }
                        IList relationships = (ArrayList)htol[target];// Gives us a list of parallel relationships to the same downstream.

                        //						if ( ms.Name.Equals("B : RCV Liquid1.Xfer-In.Start") && target.Name.Equals("B : RCV Liquid1.Temp-Set.End") ) {
                        //							fullData = true;
                        //						}

                        if (fullData)
                            foreach (MilestoneRelationship mr2 in relationships)
                                _Debug.WriteLine(mr2.ToString());



                        DateTime minDateTime = DateTime.MinValue;
                        DateTime maxDateTime = DateTime.MaxValue;
                        foreach (MilestoneRelationship mr2 in relationships)
                        {
                            /*foreach ( MilestoneRelationship recip in mr2.Reciprocals) {
								recip.PushEnabled(false);
								m_pushedDisablings.Push(recip);
							}*/
                            if (fullData)
                                if (_debug)
                                    _Debug.WriteLine("\t\tAdjusting window to satisfy " + mr2);
                            DateTime thisMinDt, thisMaxDt;
                            mr2.Reaction(ms.DateTime, out thisMinDt, out thisMaxDt);       // Get the relationship's acceptable window.
                            minDateTime = DateTimeOperations.Max(minDateTime, thisMinDt); // Narrow the range from below.
                            maxDateTime = DateTimeOperations.Min(maxDateTime, thisMaxDt); // Narrow the range from above.
                            if (fullData)
                                if (_debug)
                                    _Debug.WriteLine("\t\t\tThe window is now from " + minDateTime + " to " + maxDateTime + ".");
                        }

                        //if ( m_debug ) _Debug.WriteLine("\t\tThe final window is from " + minDateTime + " to " + maxDateTime + ".");
                        if (minDateTime <= maxDateTime)
                        {
                            DateTime newDateTime = GetClosestDateTime(minDateTime, maxDateTime, target.DateTime);
                            if (!target.DateTime.Equals(newDateTime))
                            {
                                if (_debug)
                                    _Debug.WriteLine("\t\t\tWe will move " + target.Name + " from " + target.DateTime + " to " + newDateTime);
                                if (!_changedMilestones.Contains(target))
                                    _changedMilestones.Enqueue(target);
                                if (!_oldValues.Contains(target))
                                    _oldValues.Add(target, target._dateTime);
                                if (fullData)
                                    if (_debug)
                                        _Debug.WriteLine("\t\t\tThere are now " + _changedMilestones.Count + " milestones with changes to process.");
                                target._dateTime = newDateTime;
                            }
                            else
                            {
                                if (_debug)
                                    _Debug.WriteLine("\t\t\t" + target.Name + " stays put.");
                            }
                            //						} else {
                            //							if ( m_debug ) _Debug.WriteLine("\t\t\tThis is an unachievable window.");
                            //							throw new ApplicationException("Can't find a new datetime value for " + target.ToString());
                        }

                        fullData = false;
                    }
                }
            }

            private static DateTime GetClosestDateTime(DateTime minDateTime, DateTime maxDateTime, DateTime proposedDateTime)
            {
                Debug.Assert(minDateTime <= maxDateTime, "GetClosestDateTime was passed a mnDateTime that was greater than the maxDateTime it was passed.");
                if (proposedDateTime < minDateTime)
                {
                    return minDateTime;
                }
                if (proposedDateTime > maxDateTime)
                {
                    return maxDateTime;
                }
                return proposedDateTime;
            }

            #region (Unused) Cyclic Dependency Checkers
#if UNUSED
			private static void CheckForCyclicDependencies(Milestone prospectiveMover){
				Console.WriteLine("******************************************************************************");
				Stack callStack = new Stack();
				_CheckForCyclicDependencies(prospectiveMover, ref callStack);
			}

			private static void _CheckForCyclicDependencies(IMilestone prospectiveMover, ref Stack callStack){
				if ( !callStack.Contains(prospectiveMover) ) {
					callStack.Push(prospectiveMover);
					foreach ( MilestoneRelationship mr in prospectiveMover.Relationships ) {
						if ( mr.Enabled && mr.Independent.Equals(prospectiveMover)) {
							foreach ( MilestoneRelationship recip in mr.Reciprocals ) recip.PushEnabled(false);
							callStack.Push(mr);
							_CheckForCyclicDependencies(mr.Dependent,ref callStack);
							foreach ( MilestoneRelationship recip in mr.Reciprocals ) recip.PopEnabled();
						}
					}
				
				} else {
					object obj;
					while ( callStack.Count > 0 ) {
						obj = callStack.Pop();
						if ( obj is IMilestone ) {
							IMilestone ms = (IMilestone)obj;
							Console.WriteLine("Milestone : " + ms);
						} else if ( obj is MilestoneRelationship ) {
							MilestoneRelationship mr = (MilestoneRelationship)obj;
							Console.WriteLine("Relationship : " + mr);
						}
					}
					Console.WriteLine();
				}
			}
#endif
            #endregion
        }
    }
}
