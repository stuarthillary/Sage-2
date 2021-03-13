/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Graphs.PFC.Execution;
using Highpoint.Sage.Graphs.PFC.Expressions;

namespace Highpoint.Sage.Graphs.PFC
{
    /// <summary>
    /// Implemented by an object that is an SfcTransition.
    /// </summary>
    public interface IPfcTransitionNode : IPfcNode
    {

        /// <summary>
        /// Gets the expression that is attached to this transition node.
        /// </summary>
        /// <value>The expression.</value>
        Expression Expression
        {
            get;
        }

        /// <summary>
        /// Gets or sets the 'user-friendly' value of this expression. Uses step names and macro names.
        /// </summary>
        /// <value>The expression value.</value>
        string ExpressionUFValue
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the 'user-hostile' value of this expression. Uses guids in place of names.
        /// </summary>
        /// <value>The expression value.</value>
        string ExpressionUHValue
        {
            get; set;
        }

        /// <summary>
        /// Gets the expanded value of this expression. Uses step names and expands macro names into their resultant names.
        /// </summary>
        /// <value>The expression expanded.</value>
        string ExpressionExpandedValue
        {
            get;
        }

        /// <summary>
        /// Gets or sets the default executable condition, that is the executable condition that this transition will
        /// evaluate unless overridden in the execution manager.
        /// </summary>
        /// <value>The default executable condition.</value>
        ExecutableCondition ExpressionExecutable
        {
            get; set;
        }

        /// <summary>
        /// Gets the transition state machine associated with this PFC transition.
        /// </summary>
        /// <value>My step state machine.</value>
        TransitionStateMachine MyTransitionStateMachine
        {
            get;
        }

    }
}
