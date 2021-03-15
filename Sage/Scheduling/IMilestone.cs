/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using System;
using System.Collections.Generic;

namespace Highpoint.Sage.Scheduling
{
    public interface IMilestone : IObservable, IHasIdentity
    {
        /// <summary>
        /// Adds one to the set of relationships that this milestone has with other milestones.
        /// </summary>
        /// <param name="relationship">The new relationship that involves this milestone.</param>
		void AddRelationship(MilestoneRelationship relationship);

        /// <summary>
        /// Removes one from the set of relationships that this milestone has with other milestones..
        /// </summary>
        /// <param name="relationship">The relationship to be removed.</param>
		void RemoveRelationship(MilestoneRelationship relationship);

        /// <summary>
        /// Moves the time of this milestone to the specified new DateTime.
        /// </summary>
        /// <param name="newDateTime">The new date time.</param>
		void MoveTo(DateTime newDateTime);

        /// <summary>
        /// Moves the time of this milestone by the amount of time specified.
        /// </summary>
        /// <param name="delta">The delta.</param>
		void MoveBy(TimeSpan delta);

        /// <summary>
        /// Gets the date &amp; time of this milestone.
        /// </summary>
        /// <value>The date time.</value>
		DateTime DateTime
        {
            get;
        }

        /// <summary>
        /// Gets the relationships that involve this milestone.
        /// </summary>
        /// <value>The relationships.</value>
		List<MilestoneRelationship> Relationships
        {
            get;
        }

        bool Active
        {
            get; set;
        }
        void PushActiveSetting(bool newSetting);
        void PopActiveSetting();


    }
}
