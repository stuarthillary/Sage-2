/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.SimCore
{
    internal class InvalidTransitionHandler : TransitionHandler
    {
        public new event PrepareTransitionEvent Prepare { add { Puke(); } remove { Puke(); } }
        public new event CommitTransitionEvent Commit { add { Puke(); } remove { Puke(); } }
        public new event RollbackTransitionEvent Rollback { add { Puke(); } remove { Puke(); } }
        public new bool IsValidTransition
        {
            get
            {
                return false;
            }
        }
        public new void AddPrepareEvent(PrepareTransitionEvent pte, double priority)
        {
            Puke();
        }
        public new void RemovePrepareEvent(PrepareTransitionEvent pte)
        {
            Puke();
        }
        public new void AddCommitEvent(CommitTransitionEvent cte, double priority)
        {
            Puke();
        }
        public new void RemoveCommitEvent(CommitTransitionEvent cte)
        {
            Puke();
        }
        public new void AddRollbackEvent(RollbackTransitionEvent rte, double priority)
        {
            Puke();
        }
        public new void RemoveRollbackEvent(RollbackTransitionEvent rte)
        {
            Puke();
        }

        private void Puke()
        {
            throw new ApplicationException("Attempt to interact with an event on an invalid transition.");
        }
    }

}
