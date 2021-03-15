/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// A Binomial Cumulative Density Function.
    /// </summary>
    /// <seealso cref="Highpoint.Sage.Mathematics.ICDF" />
    public class BinomialCDF : ICDF
    {
        /// <summary>
        /// The m CDF
        /// </summary>
        private readonly SmallDoubleInterpolable _cdf;
        /// <summary>
        /// Initializes a new instance of the <see cref="BinomialCDF"/> class.
        /// </summary>
        /// <param name="probability">The probability.</param>
        /// <param name="numberOfOpps">The number of opps.</param>
        public BinomialCDF(double probability, int numberOfOpps)
        {
            double p = probability;
            int n = numberOfOpps;

            //			double[] xVals = new double[n+1]; // This will be [0,1];
            //			double[] yVals = new double[n+1]; // This will be [0,numberOfOpps];
            ArrayList xVals = new ArrayList();
            ArrayList yVals = new ArrayList();


            InitFactorials(numberOfOpps);
            double cumP = 0.0; // cumulative probability.
            for (int x = 0; x <= n; x++)
            {
                double tmp = cumP + (Factorial(n) / (Factorial(x) * Factorial(n - x))) * Math.Pow(p, x) * Math.Pow(1 - p, n - x);
                if (tmp != cumP && tmp != 1.0)
                {
                    yVals.Add((double)x);
                    cumP = tmp;
                    xVals.Add(cumP);
                }
                else
                {
                    yVals.Add((double)n);
                    xVals.Add(1.0);
                    break;
                }
            }

            double[] xvals = (double[])xVals.ToArray(typeof(double));
            double[] yvals = (double[])yVals.ToArray(typeof(double));
            _cdf = new SmallDoubleInterpolable(xvals, yvals);

        }

        /// <summary>
        /// The m factorials
        /// </summary>
        private double[] _factorials;
        /// <summary>
        /// Initializes the factorials.
        /// </summary>
        /// <param name="max">The maximum.</param>
        private void InitFactorials(int max)
        {
            _factorials = new double[max + 1];
            double f = 1;
            _factorials[0] = 1;
            for (int i = 1; i <= max; i++)
            {
                f *= i;
                _factorials[i] = f;
            }
        }

        /// <summary>
        /// Factorials the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns>System.Double.</returns>
        private double Factorial(int x)
        {
            return _factorials[x];
        }

        #region ICDF Members

        /// <summary>
        /// Returns the X-value variate from the implementing CDF that corresponds to the value of 'linear'.
        /// </summary>
        /// <param name="linear">A double in the range of (0.0-1.0].</param>
        /// <returns>System.Double.</returns>
        public double GetVariate(double linear)
        {
            return _cdf.GetYValue(linear);
        }

        #endregion
    }

}