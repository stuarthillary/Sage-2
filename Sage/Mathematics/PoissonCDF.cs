/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections.Generic;

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// A Poisson Cumulative Density Function.
    /// </summary>
    /// <seealso cref="Highpoint.Sage.Mathematics.ICDF" />
    public class PoissonCDF : ICDF
    {
        /// <summary>
        /// The m SDI
        /// </summary>
        private SmallDoubleInterpolable _sdi;

        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Initializes a new instance of the <see cref="PoissonCDF"/> class.
        /// </summary>
        /// <param name="lambda">The lambda.</param>
        /// <param name="outerLimit">The outer limit.</param>
        public PoissonCDF(double lambda, double outerLimit)
        {
            if (!LookupTableInitialization(lambda, outerLimit))
                ComputationalInitialization(lambda, outerLimit);
        }

        /// <summary>
        /// Lookups the table initialization.
        /// </summary>
        /// <param name="lambda">The lambda.</param>
        /// <param name="outerLimit">The outer limit.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool LookupTableInitialization(double lambda, double outerLimit)
        {
            List<double> xValues = PoissonCDFLookupTable.ForLambda(lambda);
            if (xValues == null)
                return false;
            List<double> yValues = new List<double>();
            double d = -1;
            xValues.ForEach(nul => yValues.Add(++d));
            _sdi = new SmallDoubleInterpolable(xValues.ToArray(), yValues.ToArray());
            return true;
        }

        /// <summary>
        /// Computationals the initialization.
        /// </summary>
        /// <param name="_lambda">The lambda.</param>
        /// <param name="outerLimit">The outer limit.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool ComputationalInitialization(double _lambda, double outerLimit)
        {
            double lambda = _lambda;
            int intOuterLimit = (int)Math.Ceiling(outerLimit);
            InitFactorials(intOuterLimit + 1);
            double[] x = new double[intOuterLimit + 1];
            double[] y = new double[intOuterLimit + 1];
            double runningSum = 0;
            double eToThePowerOfLambda = Math.Exp(-lambda);
            int i = 0;
            x[i] = 0.0;
            y[i] = i;
            for (i = 1; i < intOuterLimit; i++)
            {
                runningSum += eToThePowerOfLambda * Math.Pow(lambda, i) / Factorial(i);
                x[i] = runningSum;
                y[i] = i;
            }
            x[i] = 1.0; // establish upper boundary at 1.0 probability.
            y[i] = y[i - 1] + ((y[i - 1] - y[i - 2])); // The last bin ramps down at half the rate of the bin before it.
            IDoubleInterpolator idi = new LinearDoubleInterpolator();
            idi.SetData(x, y);
            _sdi = new SmallDoubleInterpolable(intOuterLimit, idi);
            return true;
        }

        #region ICDF Members

        /// <summary>
        /// Returns the X-value variate from the implementing CDF that corresponds to the value of 'linear'.
        /// </summary>
        /// <param name="linear">A double in the range of (0.0-1.0].</param>
        /// <returns>System.Double.</returns>
        public double GetVariate(double linear)
        {
            return Math.Round(_sdi.GetYValue(linear) + 0.5); // Find the nearest integer value.
        }

        #endregion

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


    }

}