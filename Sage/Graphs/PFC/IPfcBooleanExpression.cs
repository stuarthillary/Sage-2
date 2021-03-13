/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Graphs.PFC
{
    /// <summary>
    /// An interface implemented by anything in an SFC that is to be evaluated as an expression that returns a boolean.
    /// </summary>
    public interface IPfcBooleanExpression : IPfcExpression<bool>
    {
        /// <summary>
        /// Gets the left hand side of the boolean expression.
        /// </summary>
        /// <value>The left hand side of the boolean expression..</value>
        string Lhs
        {
            get;
        }
        /// <summary>
        /// Gets the right hand side of the boolean expression.
        /// </summary>
        /// <value>The right hand side of the boolean expression..</value>
        string Rhs
        {
            get;
        }

    }
}
