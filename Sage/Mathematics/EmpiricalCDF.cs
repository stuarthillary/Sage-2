/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// Implements an empirical CDF. The xValues passed in will be in the interval of [0.0,1.0), and
    /// the yValues passed in will be empirically-determined data points.
    /// </summary>
    public class EmpiricalCDF : ICDF
    {
        private readonly SmallDoubleInterpolable _empirical;

        /// <summary>
        /// Creates an empirical, table-driven CDF from a histogram containing 'n' bins (where n > 1) with low and high bounds,
        /// and a count of instances (column height, in effect) in each bin. For example, with binBounds being a double[]
        /// with values {3.0, 5.0, 6.0, 7.0}, and heights being a double[] with values {10.0, 30.0, 20.0}, and a linear
        /// double interpolator, the empirical CDF will produce values evenly spaced from 3 to 5 1/6th of the time, values
        /// evenly spaced from 5 to 6 half the time, and values evenly spaced from 6 to 7 one third of the time.
        /// <p></p>This form of the constructor defaults to a linear interpolation.
        /// </summary>
        /// <param name="binBounds">An array of 'n+1' doubles in ascending order denoting the boundaries of the bins of the histogram.</param>
        /// <param name="heights">An array of 'n' doubles denoting the height of the bins.</param>
        /// <returns>An empirical CDF that returns the same distribution as represented in the histogram.</returns>
        public EmpiricalCDF(double[] binBounds, double[] heights) : this(binBounds, heights, new LinearDoubleInterpolator()) { }

        /// <summary>
        /// Creates an empirical, table-driven CDF from a histogram containing 'n' bins (where n > 1) with low and high bounds,
        /// and a count of instances (column height, in effect) in each bin. For example, with binBounds being a double[]
        /// with values {3.0, 5.0, 6.0, 7.0}, and heights being a double[] with values {10.0, 30.0, 20.0}, and a linear
        /// double interpolator, the empirical CDF will produce values evenly spaced from 3 to 5 1/6th of the time, values
        /// evenly spaced from 5 to 6 half the time, and values evenly spaced from 6 to 7 one third of the time.
        /// </summary>
        /// <param name="binBounds">An array of 'n+1' doubles in ascending order denoting the boundaries of the bins of the histogram.</param>
        /// <param name="heights">An array of 'n' doubles denoting the height of the bins.</param>
        /// <param name="idi">A doubleInterpolator.</param>
        /// <returns>An empirical CDF that returns the same distribution as represented in the histogram.</returns>
        public EmpiricalCDF(double[] binBounds, double[] heights, IDoubleInterpolator idi)
        {
            if (binBounds.Length != heights.Length + 1)
                throw new ApplicationException("Trying to create an empirical CDF from a histogram with other than 'n-1' height entries for 'n' interval entries.");
            // First, compute the total area under the histogram.
            double area = 0.0;
            int i = 0;
            for (; i < heights.Length; i++)
            {
                if (binBounds[i + 1] <= binBounds[i])
                    throw new ApplicationException("Trying to create an empirical CDF from a histogram with unsorted bins.");
                if (heights[i] < 0)
                    throw new ApplicationException("Trying to create an empirical CDF from a histogram with a negative entry. All heights must be zero or positive.");
                area += ((binBounds[i + 1] - binBounds[i]) * heights[i]);
            }
            if (area == 0.0)
                throw new ApplicationException("Trying to create an empirical CDF from a histogram that has no entries. At least one height entry must be positive and non-zero.");

            double[] xVals = new double[binBounds.Length];
            double[] yVals = new double[binBounds.Length];

            yVals[0] = binBounds[0];
            xVals[0] = 0.0;
            for (i = 1; i < heights.Length; i++)
            {
                yVals[i] = binBounds[i];
                xVals[i] = xVals[i - 1] + (((yVals[i] - yVals[i - 1]) * heights[i - 1]) / area);
            }
            yVals[i] = binBounds[i];
            xVals[i] = 1.0;
            _empirical = new SmallDoubleInterpolable(xVals, yVals, idi);
        }

        #region Implementation of ICDF
        public double GetVariate(double linear)
        {
            return _empirical.GetYValue(linear);
        }
        #endregion

    }

}