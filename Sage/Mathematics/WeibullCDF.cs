/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// A Weibull Cumulative Density Function. See http://www.itl.nist.gov/div898/handbook/eda/section3/eda366.htm
    /// </summary>
    /// <seealso cref="Highpoint.Sage.Mathematics.ICDF" />
    public class WeibullCDF : ICDF
    {
        /// <summary>
        /// The SmallDoubleInterpolable along which the CDF is plotted.
        /// </summary>
        private readonly SmallDoubleInterpolable _sdi;

        /// <summary>
        /// Initializes a new instance of the <see cref="WeibullCDF"/> class.
        /// </summary>
        /// <param name="gamma">The gamma.</param>
        /// <param name="nBins">The n bins.</param>
        public WeibullCDF(double gamma, int nBins)
        {
            double increment = 1.0 / nBins;
            double invGamma = 1.0 / gamma;
            double[] cdfY = new double[nBins + 1];
            double[] cdfX = new double[nBins + 1];
            int i;
            for (i = 0; i < nBins - 12; i++)
            {
                // Since we provide the Y, and expect to pull the X, we must
                // invert the CDF - i.e. insert the Y values into the x array,
                // and vice-versa. That lets us provide the Y value of the CDF into
                // the x-parameter of the GetYValue(...) call on the interpolator,
                // and receive the actual X value that would deliver that Y value.
                cdfX[i] = i * increment;
                cdfY[i] = Math.Pow(-Math.Log(1.0 - cdfX[i]), invGamma);
            }
            for (i = nBins - 12; i < nBins; i++)
            {
                cdfX[i] = cdfX[i - 1] + (0.5 * (1 - cdfX[i - 1]));
                cdfY[i] = Math.Pow(-Math.Log(1.0 - cdfX[i]), invGamma);
            }

            cdfX[i] = 1.0;
            cdfY[i] = (cdfY[i - 1] + (3 * (cdfY[i - 1] - cdfY[i - 2])));

            _sdi = new SmallDoubleInterpolable(cdfX, cdfY);

        }

        #region ICDF Members

        /// <summary>
        /// Returns the X-value variate from the implementing CDF that corresponds to the value of 'linear'.
        /// </summary>
        /// <param name="linear">A double in the range of (0.0-1.0].</param>
        /// <returns>System.Double.</returns>
        public double GetVariate(double linear)
        {
            return _sdi.GetYValue(linear);
        }

        #endregion

    }

}