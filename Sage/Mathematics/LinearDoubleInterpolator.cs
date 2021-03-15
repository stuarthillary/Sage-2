/* This source code licensed under the GNU Affero General Public License */

using System;
// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// Implemented by an object that performs linear interpolations on two arrays of doubles (an x and a y array).
    /// </summary>
    public class LinearDoubleInterpolator : IDoubleInterpolator
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

            for (int i = 0; i < xvals.Length - 1; i++)
            {
                if (_xVals[i] >= _xVals[i + 1])
                {
                    throw new ArgumentException(string.Format("Illegal attempt to configure an interpolator with non-monotonic x values (index {0}={1} and index {2}={3}).", i, _xVals[i], i + 1, _xVals[i + 1]));
                }
            }
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
            while ((lowerNdx + 2 < _xVals.Length) && (_xVals[lowerNdx + 1] < xValue))
                lowerNdx++;

            // Did we walk off the end (i.e. our lower index is the last element in the array?)
            if (double.IsNaN(_xVals[lowerNdx + 1]))
            {
                lowerNdx--;
            }

            double upperX = _xVals[lowerNdx + 1];
            double upperY = _yVals[lowerNdx + 1];
            double lowerX = _xVals[lowerNdx];
            double lowerY = _yVals[lowerNdx];
            double slope = (upperY - lowerY) / (upperX - lowerX);
            double intcp = lowerY - (slope * lowerX);

            return (slope * xValue) + intcp;
        }

        #endregion

    }
}
