/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Utility
{
    /// <summary>
    /// An object implementing this interface tracks the values that a double takes on, 
    /// registering its initial, minimum, maximum, and final values. This interface is
    /// just the public face of the bookkeeping. See class DoubleTracker.
    /// </summary>
    public interface IDoubleTracker
    {
        /// <summary>
        /// The first recorded value of the double.
        /// </summary>
        double InitialValue
        {
            get;
        }
        /// <summary>
        /// The last recorded value of the double.
        /// </summary>
        double FinalValue
        {
            get;
        }
        /// <summary>
        /// The minimum recorded value of the double.
        /// </summary>
        double MinValue
        {
            get;
        }
        /// <summary>
        /// The maximum recorded value of the double.
        /// </summary>
        double MaxValue
        {
            get;
        }
    }
}
