/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Graphs.PFC
{
    /// <summary>
    /// The types of operations permitted by this operation type.
    /// </summary>
    public enum OperationType
    {
        /// <summary>
        /// True if the LHS equals the RHS.
        /// </summary>
        Equal,
        /// <summary>
        /// True if the LHS does not equal the RHS.
        /// </summary>
        NotEqual,
        /// <summary>
        /// True if the LHS is an element of the RHS.
        /// </summary>
        In,
        /// <summary>
        /// True if the LHS is not an element of the RHS.
        /// </summary>
        NotIn,
        /// <summary>
        /// True if Mike tells me what it means :-)
        /// </summary>
        Exists,
        /// <summary>
        /// True if Mike tells me what it means :-)
        /// </summary>
        NotExists,
        /// <summary>
        /// True if the LHS is greater than the RHS.
        /// </summary>
        GreaterThan,
        /// <summary>
        /// True if the LHS is greater than or equal to the RHS.
        /// </summary>
        GreaterThanOrEqual,
        /// <summary>
        /// True if the LHS is less than the RHS.
        /// </summary>
        LessThan,
        /// <summary>
        /// True if the LHS is less than or equal to the RHS.
        /// </summary>
        LessThanOrEqual
    }
}
