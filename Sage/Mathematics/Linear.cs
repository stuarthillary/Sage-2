/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// Implements a linear CDF with an X-range of (0.0-1.0]
    /// </summary>
    public class Linear : ICDF
    {
        /// <summary>
        /// Returns the X-value variate from a Linear CDF that corresponds to the value of 'linear'.
        /// </summary>
        /// <param name="linear">A double in the range of (0.0-1.0].</param>
        /// <returns></returns>
        public double GetVariate(double linear)
        {
            return linear;
        }
    }

}