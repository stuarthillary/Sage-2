/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.Graphs.PFC.Expressions
{
    /// <summary>
    /// This class represents a string that correlates to an object. It is given a Guid that
    /// is logged into the participant directory, so that its name or its Guid can be changed
    /// without losing coherency. It is usually an object name.
    /// </summary>
    public class DualModeString : ExpressionElement
    {

        #region Private Fields

        private Guid _guid;
        private string _name;

        #endregion 

        /// <summary>
        /// Initializes a new instance of the <see cref="T:DualModeString"/> class.
        /// </summary>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        public DualModeString(Guid guid, string name)
        {
            _guid = guid;
            _name = name;
        }

        /// <summary>
        /// Returns the string for this DualModeString that corresponds to the indicated representation type.
        /// </summary>
        /// <param name="t">The indicated representation type.</param>
        /// <param name="forWhom">The owner of the expression, usually a Transition.</param>
        /// <returns>The string for this expression element.</returns>
        public override string ToString(ExpressionType t, object forWhom)
        {
            switch (t)
            {
                case ExpressionType.Expanded:
                    return _name;
                case ExpressionType.Friendly:
                    return _name;
                case ExpressionType.Hostile:
                    return string.Format("{0}", _guid);
                default:
                    throw new ApplicationException(string.Format("Unknown string format, {0}, was requested.", t));
            }
        }

        /// <summary>
        /// Gets or sets the GUID of this expression. Returns Guid.Empty if the epression element will not need to
        /// be correlated to anything (as would, for example, a string and Guid representing a Step Name element.
        /// </summary>
        /// <value>The GUID.</value>
        public override Guid Guid
        {
            get
            {
                return _guid;
            }
            set
            {
                _guid = value;
            }
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

    }
}