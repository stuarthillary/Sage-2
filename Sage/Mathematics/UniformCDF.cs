/* This source code licensed under the GNU Affero General Public License */


namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// A Uniform Cumulative Density Function.
    /// </summary>
    /// <seealso cref="Highpoint.Sage.Mathematics.ICDF" />
    public class UniformCDF : ICDF
    {
        /// <summary>
        /// The m minimum
        /// </summary>
        readonly double _min;
        /// <summary>
        /// The m dx
        /// </summary>
        readonly double _dx;
        /// <summary>
        /// Initializes a new instance of the <see cref="UniformCDF"/> class.
        /// </summary>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        public UniformCDF(double min, double max)
        {
            _min = min;
            _dx = (max - min);
        }

        #region ICDF Members

        /// <summary>
        /// Returns the X-value variate from the implementing CDF that corresponds to the value of 'linear'.
        /// </summary>
        /// <param name="linear">A double in the range of (0.0-1.0].</param>
        /// <returns>System.Double.</returns>
        public double GetVariate(double linear)
        {
            return _min + (_dx * linear);
        }

        #endregion

    }

}