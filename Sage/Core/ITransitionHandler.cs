/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// Implemented by a transition handler. A transition handler embodies
    /// the actions to be performed when the state machine is asked to make
    /// a transition from one state to another. The transition is performed
    /// via a two-phase protocol, first preparing to make the transition,
    /// and then if no handler that was involved in the preparation phase
    /// registered a failure reason, a commit operation is begun. Otherwise,
    /// if objections were registered, a rollback operation is begun. 
    /// </summary>
    public interface ITransitionHandler
    {
        /// <summary>
        /// This event is fired when the transition is beginning, and all
        /// handlers are given an opportunity to register failure reasons.
        /// This event permits registration for the preparation using the
        /// standard += and -= syntax.
        /// </summary>
        event PrepareTransitionEvent Prepare;
        /// <summary>
        /// If preparation is successful, this event is fired to signify
        /// commitment of the transition.
        /// This event permits registration for the commitment using the
        /// standard += and -= syntax.
        /// </summary>
        event CommitTransitionEvent Commit;
        /// <summary>
        /// If preparation is not successful, this event is fired to
        /// signify the failure of an attempted transition.
        /// This event permits registration for the rollback using the
        /// standard += and -= syntax.
        /// </summary>
        event RollbackTransitionEvent Rollback;
        /// <summary>
        /// Indicates whether this transition is permissible.
        /// </summary>
        bool IsValidTransition
        {
            get;
        }

        /// <summary>
        /// Adds a handler to the Prepare event with an explicitly-specified
        /// sequence number. The sequence begins with those handlers that 
        /// have a low sequence number.
        /// </summary>
        /// <param name="pte">The handler for the Prepare event.</param>
        /// <param name="sequence">The sequence number for the handler.</param>
        void AddPrepareEvent(PrepareTransitionEvent pte, double sequence);
        /// <summary>
        /// Removes a handler from the set of handlers that are registered
        /// for the prepare event.
        /// </summary>
        /// <param name="pte">The PrepareTransitionEvent handler to remove.</param>
        void RemovePrepareEvent(PrepareTransitionEvent pte);
        /// <summary>
        /// Adds a handler to the Commit event with an explicitly-specified
        /// sequence number. The sequence begins with those handlers that 
        /// have a low sequence number.
        /// </summary>
        /// <param name="cte">The handler for the CommitTransitionEvent</param>
        /// <param name="sequence">The sequence number for the handler.</param>
        void AddCommitEvent(CommitTransitionEvent cte, double sequence);
        /// <summary>
        /// Removes a handler from the set of handlers that are registered
        /// for the commit event.
        /// </summary>
        /// <param name="cte">The handler for the CommitTransitionEvent</param>
        void RemoveCommitEvent(CommitTransitionEvent cte);
        /// <summary>
        /// Adds a handler to the Prepare event with an explicitly-specified
        /// sequence number. The sequence begins with those handlers that 
        /// have a low sequence number.
        /// </summary>
        /// <param name="rte">The handler for the Rollback event.</param>
        /// <param name="sequence">The sequence number for the handler.</param>
        void AddRollbackEvent(RollbackTransitionEvent rte, double sequence);
        /// <summary>
        /// Removes a handler from the set of handlers that are registered
        /// for the rollback event.
        /// </summary>
        /// <param name="rte">The handler for the Rollback event.</param>
        void RemoveRollbackEvent(RollbackTransitionEvent rte);

    }

}
