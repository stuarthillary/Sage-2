/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.Graphs.PFC.Expressions
{
    /// <summary>
    /// An abstract base class for Rote Strings, Dual Mode Strings and Macros.
    /// </summary>
    public abstract class ExpressionElement
    {

        /// <summary>
        /// Returns the string for this expression element that corresponds to the indicated representation type.
        /// </summary>
        /// <param name="t">The indicated representation type.</param>
        /// <param name="forWhom">The owner of the expression, usually a Transition.</param>
        /// <returns>The string for this expression element.</returns>
        public abstract string ToString(ExpressionType t, object forWhom);

        /// <summary>
        /// Gets or sets the GUID of this expression. Returns Guid.Empty if the expression element will not need to
        /// be correlated to anything (as would, for example, a string and Guid representing a Step Name element.)
        /// </summary>
        /// <value>The GUID.</value>
        public virtual Guid Guid
        {
            get
            {
                return Guid.Empty;
            }
            set
            {
            }
        }

        /// <summary>
        /// Gets or sets the name of this expression. Returns string.Empty if the expression element does
        /// not reasonably have a name. Macros and placeholders for OpSteps, for example, have names, where
        /// rote strings do not.
        /// </summary>
        /// <value>The name.</value>
        public virtual string Name
        {
            get
            {
                return "";
            }
            set
            {
            }
        }

        /// <summary>
        /// Gets the type of this expression element - used primarily for ascertaining type compatibility between
        /// expressions.
        /// </summary>
        /// <value>The type.</value>
        public object Type
        {
            get
            {
                return GetType();
            }
        }

        internal bool Marked = false;
    }
}