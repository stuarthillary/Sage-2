/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.Mathematics
{

    /// <summary>
    /// Implements an exponential CDF mapped across a table with a specified number of entries or bins.
    /// Y values will range from (0.0-1.0], and the x-values at the given Y value will be stored in the
    /// corresponding bin.
    /// </summary>
    public class ExponentialCDF : ICDF
    {
        private readonly double[] _cdfX;
        private readonly int _nBins;
        private readonly double _location;
        public ExponentialCDF(double location, double scale, int nBins)
        {
            _location = location;
            _nBins = nBins;
            double delta = 1.0 / _nBins;
            _cdfX = new double[nBins + 1];
            for (int i = 1; i <= nBins; i++)
            {
                double y = i * delta;
                if (y == 1)
                    y -= (delta / 10.0); // Calculation with Y = 1 blows up. This distorts the top .2% of the histogram. 
                _cdfX[i] = (-scale * Math.Log(1 - y));
            }
        }

        #region Implementation of ICDF
        public double GetVariate(double linear)
        {
            // linear is (0..1] - should assert that.

            double scala = linear * _nBins;
            int lowerBin = (int)scala;
            double fraction = scala - lowerBin;
            double lowerX = _cdfX[lowerBin];
            if (fraction == 0.0)
                return _location + lowerX;
            double upperX = _cdfX[lowerBin + 1];
            double retval = lowerX + (fraction * (upperX - lowerX));

            return _location + retval;
        }
        #endregion
    }

}