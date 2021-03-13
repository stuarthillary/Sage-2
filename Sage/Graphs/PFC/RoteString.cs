/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Graphs.PFC.Expressions
{
    /// <summary>
    /// A class that represents a string in an expression that does not correlate to anything outside the expression,
    /// such as the string &quot;' = TRUE AND '&quot;
    /// </summary>
    public class RoteString : ExpressionElement
    {
        private readonly string _theString = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:RoteString"/> class.
        /// </summary>
        /// <param name="str">The rote string that this object will represent.</param>
        public RoteString(string str)
        {
            _theString = str;
        }

        /// <summary>
        /// Returns the string for this expression element that corresponds to the indicated representation type.
        /// </summary>
        /// <param name="t">The indicated representation type.</param>
        /// <param name="forWhom">The owner of the expression, usually a Transition.</param>
        /// <returns>The string for this expression element.</returns>
        public override string ToString(ExpressionType t, object forWhom)
        {
            return _theString;
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override string ToString()
        {
            return _theString;
        }
    }
}