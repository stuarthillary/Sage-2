/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.Scheduling
{
    public class MilestoneAdjustmentException : Exception
    {
        private MilestoneRelationship _relationship;
        public MilestoneAdjustmentException(string msg, MilestoneRelationship relationship) : base(msg)
        {
            _relationship = relationship;
        }

        public MilestoneRelationship Relationship
        {
            get
            {
                return _relationship;
            }
        }
    }
}
