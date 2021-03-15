/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Utility
{
    /// <summary>
    /// A struct that shadows a boolean in, for example, a temperature controller, and indicates
    /// whether that boolean is to  is to be read as its default state, or as an overridden value.
    /// </summary>
    public struct OverrideBool
    {
        private bool _boolValue;

        /// <summary>
        /// Indicates true if this object's initial value has been overridden.
        /// </summary>
        public bool Override
        {
            get; set;
        }

        /// <summary>
        /// The bool value contained in this Overridable. Override is set to true if this value is set.
        /// </summary>
        public bool BoolValue
        {
            get
            {
                return _boolValue;
            }
            set
            {
                Override = true;
                _boolValue = value;
            }
        }
    }

}
