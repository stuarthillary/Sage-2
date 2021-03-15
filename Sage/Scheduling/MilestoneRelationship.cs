/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;

namespace Highpoint.Sage.Scheduling
{

    /// <summary>
    /// This is an abstract class from which all MilestoneRelationships are derived.<b></b>
    /// A MilestoneRelationship represents a relationship between a dependent milestone
    /// such as "Oven Heatup Finishes" and an independent one such as "Bake Cookies."
    /// In this case, the relationship would be a MilestoneRelationship_GTE(heatupDone,startBaking);<b></b>
    /// meaning that if the heatupDone milestone is changed, then the startBaking milestone will
    /// also be adjusted, if the change resulted in startBaking occurring before heatupDone.
    /// </summary>
    public abstract class MilestoneRelationship
    {

        #region Private Fields
        private readonly Stack _enabled;
        /// <summary>
        /// The dependent milestone affected by this milestone.
        /// </summary>
        protected IMilestone dependent;
        /// <summary>
        /// The independent milestone monitored by this milestone.
        /// </summary>
        protected IMilestone independent;
        /// <summary>
        /// A list of the reciprocal relationships to this relationship.
        /// </summary>
        protected ArrayList reciprocals = empty_List;
        private static readonly ArrayList empty_List = ArrayList.ReadOnly(new ArrayList());
        #endregion

        /// <summary>
        /// Imposes changes on the dependent milestone, if the independent one changes.
        /// </summary>
        /// <param name="independent">The one that might be changed to kick off this rule.</param>
        /// <param name="dependent">The one upon which a resulting change is imposed by this rule.</param>
        public MilestoneRelationship(IMilestone dependent, IMilestone independent)
        {
            this.independent = independent;
            this.dependent = dependent;
            _enabled = new Stack();
            _enabled.Push(true);
            if (this.independent != null)
                this.independent.AddRelationship(this);
            if (this.dependent != null)
                this.dependent.AddRelationship(this);
        }

        /// <summary>
        /// Detaches this relationship from the two milestones.
        /// </summary>
        public void Detach()
        {
            if (independent != null)
                independent.RemoveRelationship(this);
            if (dependent != null)
                dependent.RemoveRelationship(this);
        }

        /// <summary>
        /// Gets the dependent milestone.
        /// </summary>
        /// <value>The dependent milestone.</value>
        public IMilestone Dependent
        {
            get
            {
                return dependent;
            }
        }

        /// <summary>
        /// Gets the independent milestone.
        /// </summary>
        /// <value>The independent milestone.</value>
        public IMilestone Independent
        {
            get
            {
                return independent;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="MilestoneRelationship"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        public bool Enabled
        {
            get
            {
                return (bool)_enabled.Peek();
            }
            set
            {
                PopEnabled();
                PushEnabled(value);
            }
        }

        /// <summary>
        /// Models a reaction to a movement of the independent milestone, and provides minimum and maximum acceptable
        /// DateTime values for the dependent milestone.
        /// </summary>
        /// <param name="independentNewValue">The independent new value.</param>
        /// <param name="minDateTime">The minimum acceptable DateTime value for the dependent milestone.</param>
        /// <param name="maxDateTime">The maximum acceptable DateTime value for the dependent milestone.</param>
        public abstract void Reaction(DateTime independentNewValue, out DateTime minDateTime, out DateTime maxDateTime);

        /// <summary>
        /// Pushes the enabled state of this relationship.
        /// </summary>
        /// <param name="newValue">if set to <c>true</c> [new value].</param>
        public void PushEnabled(bool newValue)
        {
            _enabled.Push(newValue);
        }

        /// <summary>
        /// Pops the enabled state of this relationship.
        /// </summary>
        public void PopEnabled()
        {
            _enabled.Pop();
        }

        #region Reciprocal Management
        /// <summary>
        /// Gets a relationship that is the reciprocal, if applicable, of this one. If there is no reciprocal,
        /// then this returns null.
        /// </summary>
        /// <value>The reciprocal.</value>
        public abstract MilestoneRelationship Reciprocal
        {
            get;
        }

        /// <summary>
        /// A reciprocal is a secondary relationship that should not fire if this one fired.
        /// A good example is a strut where one strut pins A to 5 mins after B, and another pins B to
        /// 5 mins before A.
        /// </summary>
        /// <param name="reciprocal">The reciprocal relationship.</param>
        public void AddReciprocal(MilestoneRelationship reciprocal)
        {
            if (reciprocals == empty_List)
            {
                reciprocals = new ArrayList();
            }
            reciprocals.Add(reciprocal);
        }

        /// <summary>
        /// Removes the specified reciprocal relationship from this relationship.
        /// </summary>
        /// <param name="reciprocal">The reciprocal.</param>
        public void RemoveReciprocal(MilestoneRelationship reciprocal)
        {
            if (reciprocals.Contains(reciprocal))
                reciprocals.Remove(reciprocal);
        }

        /// <summary>
        /// Clears the reciprocal relationships.
        /// </summary>
        public void ClearReciprocal()
        {
            if (reciprocals != empty_List)
                reciprocals.Clear();
        }

        /// <summary>
        /// Gets the reciprocal relationships.
        /// </summary>
        /// <value>The reciprocals.</value>
        public IList Reciprocals
        {
            get
            {
                return ArrayList.ReadOnly(reciprocals);
            }
        }
        #endregion

        #region Correctness Checking
        /// <summary>
        /// Assesses the initial satisfaction of this relationship for ctor.
        /// </summary>
        protected void AssessInitialCorrectnessForCtor()
        {
            if (!IsSatisfied())
            {
                Detach();
                string msg = "Relationship " + ToString() + ", applied to "
                  + dependent.Name + "(" + dependent.DateTime + "), and "
                  + independent.Name + "(" + independent.DateTime + ") is not initially satisfied.";
                throw new ApplicationException(msg);
            }
        }

        /// <summary>
        /// Determines whether this relationship is currently satisfied.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this instance is satisfied; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool IsSatisfied();
        #endregion

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"></see> to compare with the current <see cref="T:System.Object"></see>.</param>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            MilestoneRelationship mr = obj as MilestoneRelationship;
            if (mr == null)
            {
                return false;
            }
            else if (GetType() != mr.GetType())
            {
                return false;
            }
            else if (Object.Equals(Dependent, mr.Dependent) && Object.Equals(Independent, mr.Independent))
            {
                return true;
            }
            else
            {
                return base.Equals(obj);
            }
        }

        /// <summary>
        /// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override int GetHashCode()
        {
            return (Dependent == null ? 0 : Dependent.GetHashCode()) ^ (Independent == null ? 0 : Independent.GetHashCode());
        }
    }
}


//_Debug.WriteLine("Independent : " + m_independent.Name + " @ " + m_independent.DateTime.ToString());
//_Debug.WriteLine("Dependent   : " + m_dependent.Name   + " @ " +   m_dependent.DateTime.ToString());
//_Debug.WriteLine("Delta       : " + m_delta.ToString());
//_Debug.WriteLine(this.ToString());
