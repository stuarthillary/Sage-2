/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// A Triangular Cumulative Density Function.
    /// </summary>
    /// <seealso cref="Highpoint.Sage.Mathematics.ICDF" />
    public class TriangularCDF : ICDF
    {

        #region Private Fields
        /// <summary>
        /// The m lo
        /// </summary>
        private readonly double _lo;
        /// <summary>
        /// The m hi
        /// </summary>
        private readonly double _hi;
        /// <summary>
        /// The m PCT lo
        /// </summary>
        private readonly double _pctLo;
        /// <summary>
        /// The m PCT hi
        /// </summary>
        private readonly double _pctHi;
        /// <summary>
        /// The m lo range
        /// </summary>
        private readonly double _loRange;
        /// <summary>
        /// The m hi range
        /// </summary>
        private readonly double _hiRange;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="TriangularCDF"/> class.
        /// </summary>
        /// <param name="lowBound">The low bound.</param>
        /// <param name="peak">The peak.</param>
        /// <param name="highBound">The high bound.</param>
        public TriangularCDF(double lowBound, double peak, double highBound)
        {
            //			m_sdi = new SmallDoubleInterpolable(3);
            //			m_sdi.SetYValue(0,lowBound);
            //			m_sdi.SetYValue(.5,peak);
            //			m_sdi.SetYValue(1.0,highBound);
            _lo = lowBound;
            _hi = highBound;
            _loRange = peak - lowBound;
            _hiRange = highBound - peak;
            _pctLo = (peak - lowBound) / (highBound - lowBound);
            _pctHi = 1.0 - _pctLo;
        }
        #region ICDF Members

        /// <summary>
        /// Returns the X-value variate from the implementing CDF that corresponds to the value of 'linear'.
        /// </summary>
        /// <param name="linear">A double in the range of (0.0-1.0].</param>
        /// <returns>System.Double.</returns>
        public double GetVariate(double linear)
        {
            double retval;
            if (linear <= _pctLo)
            {
                linear /= _pctLo; // back to a [0..1)
                retval = _lo + (Math.Sqrt(linear) * _loRange);
            }
            else
            {
                linear = (1.0 - linear) / _pctHi; // back to a [0..1)
                retval = _hi - (Math.Sqrt(linear) * _hiRange);
            }
            return retval;
        }

        #endregion

    }

}