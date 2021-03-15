/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// With the value of 'linear' in the Y-value range of (0.0-1.0], this
    /// will return the X-value variate from the implementing CDF. 
    /// </summary>
    public interface ICDF
    {
        /// <summary>
        /// Returns the X-value variate from the implementing CDF that corresponds to the value of 'linear'.
        /// </summary>
        /// <param name="linear">A double in the range of (0.0-1.0].</param>
        /// <returns></returns>
        double GetVariate(double linear);
    }

}