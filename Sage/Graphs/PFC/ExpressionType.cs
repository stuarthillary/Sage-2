/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Graphs.PFC.Expressions
{
    /// <summary>
    /// Expressions are prepresented as Hostile (with Guids), Friendly (names, including macro names) and Expanded (all names, plus macros evaluated.)
    /// </summary>
    public enum ExpressionType
    {
        /// <summary>
        /// The expression has proper names and macros expanded.
        /// </summary>
        Expanded,
        /// <summary>
        /// The expression has proper names and macros referenced by name.
        /// </summary>
        Friendly,
        /// <summary>
        /// All names and macros are replaced by their guids.
        /// </summary>
        Hostile
    }
}