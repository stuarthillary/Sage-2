/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// A Cauchy Cumulative Density Function.
    /// </summary>
    /// <seealso cref="Highpoint.Sage.Mathematics.ICDF" />
    public class CauchyCDF : ICDF
    {
        /// <summary>
        /// The m location
        /// </summary>
        private readonly double _location;

        /// <summary>
        /// The m shape
        /// </summary>
        private readonly double _shape;
        /// <summary>
        /// Initializes a new instance of the <see cref="CauchyCDF"/> class.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="shape">The shape.</param>
        public CauchyCDF(double location, double shape)
        {
            _location = location;
            _shape = shape;
        }

        #region ICDF Members

        /// <summary>
        /// Returns the X-value variate from the implementing CDF that corresponds to the value of 'linear'.
        /// </summary>
        /// <param name="linear">A double in the range of (0.0-1.0].</param>
        /// <returns>System.Double.</returns>
        public double GetVariate(double linear)
        {
            return _location + (_shape * (Math.Tan((linear - 0.5) * Math.PI)));
        }

        #endregion

    }

}