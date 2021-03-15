/* This source code licensed under the GNU Affero General Public License */

using System;
// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// Implemented by an object that performs cosine interpolations on two arrays of doubles (an x and a y array).
    /// </summary>
    public class CosineDoubleInterpolator : IDoubleInterpolator
    {
        private double[] _xVals, _yVals;
        private bool _hasData;

        #region IDoubleInterpolator Members

        /// <summary>
        /// Sets the data used by this interpolator.
        /// </summary>
        /// <param name="xvals">The xvals.</param>
        /// <param name="yvals">The yvals.</param>
        public void SetData(double[] xvals, double[] yvals)
        {
            _xVals = xvals;
            _yVals = yvals;
            _hasData = true;
            if (_xVals.Length != _yVals.Length)
                throw new ArgumentException("XValue and YValue arrays are of unequal length.");
            if (_xVals.Length < 2)
                throw new ArgumentException(string.Format("Illegal attempt to configure an interpolator on {0} data points.", _xVals.Length));

        }

        /// <summary>
        /// Gets a value indicating whether this instance has data.
        /// </summary>
        /// <value><c>true</c> if this instance has data; otherwise, <c>false</c>.</value>
		public bool HasData => _hasData;

        /// <summary>
        /// Gets the Y value for the specified x value.
        /// </summary>
        /// <param name="xValue">The X value.</param>
        /// <returns></returns>
		public double GetYValue(double xValue)
        {

            int lowerNdx = 0;
            while (_xVals[lowerNdx + 1] < xValue)
                lowerNdx++;

            // Did we walk off the end (i.e. our lower index is the last element in the array?)
            if (double.IsNaN(_xVals[lowerNdx + 1]))
                lowerNdx--;

            double upperX = _xVals[lowerNdx + 1];
            double upperY = _yVals[lowerNdx + 1];
            double lowerX = _xVals[lowerNdx];
            double lowerY = _yVals[lowerNdx];
            double mu = (xValue - lowerX) / (upperX - lowerX);
            double mu2 = (1 - Math.Cos(mu * Math.PI)) / 2.0;
            return lowerY * (1 - mu2) + upperY * mu2;
        }

        #endregion

    }
}
