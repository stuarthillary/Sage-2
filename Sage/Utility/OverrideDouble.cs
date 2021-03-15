/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Utility
{


    /// <summary>
    /// A struct that shadows a double in, for example, a temperature controller, and indicates
    /// whether that double is to be read as its default state, or as an overridden value.
    /// </summary>
    public struct OverrideDouble
    {
        private double _doubleVal;

        /// <summary>
        /// Indicates true if this object's initial value has been overridden.
        /// </summary>
        public bool Override
        {
            get; set;
        }

        /// <summary>
        /// The double value contained in this Overridable. Override is set to true if this value is set.
        /// </summary>
        public double DoubleValue
        {
            get
            {
                return _doubleVal;
            }
            set
            {
                Override = true;
                _doubleVal = value;
            }
        }
    }

}
