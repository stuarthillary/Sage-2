/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.Scheduling
{
    /// <summary>
    /// Ensures that the dependent is always at a less-than-or-equal time to the independent.
    /// </summary>
    public class MilestoneRelationship_LTE : MilestoneRelationship
    {
        private TimeSpan _delta;
        public MilestoneRelationship_LTE(IMilestone dependent, IMilestone independent)
            : base(dependent, independent)
        {
            _delta = independent.DateTime - dependent.DateTime;
            AssessInitialCorrectnessForCtor();
        }

        /// <summary>
        /// Models a reaction to a movement of the independent milestone, and provides minimum and maximum acceptable
        /// DateTime values for the dependent milestone.
        /// </summary>
        /// <param name="independentNewValue">The independent new value.</param>
        /// <param name="minDateTime">The minimum acceptable DateTime value for the dependent milestone.</param>
        /// <param name="maxDateTime">The maximum acceptable DateTime value for the dependent milestone.</param>
        public override void Reaction(DateTime independentNewValue, out DateTime minDateTime, out DateTime maxDateTime)
        {
            minDateTime = DateTime.MinValue;
            maxDateTime = independentNewValue;
        }

        /// <summary>
        /// Determines whether this relationship is currently satisfied.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this instance is satisfied; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsSatisfied()
        {
            return (!Enabled || dependent.DateTime <= independent.DateTime);
        }

        /// <summary>
        /// Gets a relationship that is the reciprocal, if applicable, of this one. If there is no reciprocal,
        /// then this returns null.
        /// </summary>
        /// <value>The reciprocal.</value>
        public override MilestoneRelationship Reciprocal
        {
            get
            {
                return new MilestoneRelationship_GTE(Independent, Dependent);
            }
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override string ToString()
        {
            return Dependent.Name + " occurs before or when " + Independent.Name + " occurs.";
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"></see> to compare with the current <see cref="T:System.Object"></see>.</param>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            return base.Equals(obj) && _delta == ((MilestoneRelationship_LTE)obj)._delta;
        }

        /// <summary>
        /// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }
}


//_Debug.WriteLine("Independent : " + m_independent.Name + " @ " + m_independent.DateTime.ToString());
//_Debug.WriteLine("Dependent   : " + m_dependent.Name   + " @ " +   m_dependent.DateTime.ToString());
//_Debug.WriteLine("Delta       : " + m_delta.ToString());
//_Debug.WriteLine(this.ToString());
