/* This source code licensed under the GNU Affero General Public License */
using System;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Graphs.PFC.Expressions
{
    /// <summary>
    /// Abstract base class for all macros. Derives from ExpressionElement, and from that, obtains
    /// the ability to be expressed as friendly, hostile or expanded format.
    /// </summary>
    public abstract class Macro : ExpressionElement
    {
        /// <summary>
        /// All Macros start with this string.
        /// </summary>
        public static readonly string MACRO_START = "'µ";

        #region Protected Fields
        /// <summary>
        /// The Guid by which this macro will be known.
        /// </summary>
        protected Guid _guid;
        /// <summary>
        /// The friendly representation of this macro.
        /// </summary>
        protected string _name;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Macro"/> class.
        /// </summary>
        public Macro()
        {
        }

        /// <summary>
        /// Evaluates the macro using the specified arguments.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The evaluated representation of the macro.</returns>
        protected abstract string Evaluate(object[] args);

        /// <summary>
        /// Returns the string for this macro element that corresponds to the indicated representation type.
        /// </summary>
        /// <param name="t">The indicated representation type.</param>
        /// <param name="forWhom">The owner of the expression, usually a Transition.</param>
        /// <returns>The string for this expression element.</returns>
        public override string ToString(ExpressionType t, object forWhom)
        {
            _Debug.Assert(Name.StartsWith(MACRO_START));

            switch (t)
            {
                case ExpressionType.Expanded:
                    return Evaluate(new object[] { forWhom });
                case ExpressionType.Friendly:
                    return Name;
                case ExpressionType.Hostile:
                    return Guid.ToString();
                default:
                    throw new ApplicationException(string.Format("Unknown string format, {0}, was requested.", t));
            }
        }

        /// <summary>
        /// Gets macro's name. Overridden in the concrete macro class.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                _Debug.Assert(_name != String.Empty, "Must provide a value for name in all defined macros.");
                return _name;
            }
        }
        /// <summary>
        /// Gets or sets the GUID of this expression. Returns Guid.Empty if the epression element will not need to
        /// be correlated to anything (as would, for example, a string and Guid representing a Step Name element.
        /// Overridden in the concrete macro class.
        /// </summary>
        /// <value>The GUID.</value>
        public override Guid Guid
        {
            get
            {
                _Debug.Assert(!_guid.Equals(Guid.Empty), "Must provide a value for guid in all defined macros.");
                return _guid;
            }
        }
    }
}