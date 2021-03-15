/* This source code licensed under the GNU Affero General Public License */
// Note - code in this file is separately licensed as specified below.
using System;
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// Not originally written by Highpoint Software Systems, LLC. Written by Walt Fair, obtained
    /// from the CodeProject site below on 5/17/2009, and used per the Code Project Open License 
    /// at the site below. Several .NET / C# semantic improvements added.
    /// http://www.codeproject.com/KB/recipes/LinReg.aspx ( Walt's excellent article. )
    /// http://www.codeproject.com/info/cpol10.aspx       ( CodeProject Open License 1.02 )
    /// </summary>
    public class LinearRegression
    {
        double[,] _v;            // Least squares and var/covar matrix
        public double[] C;        // Coefficients
        public double[] Sec;      // Std Error of coefficients
        double _rysq;            // Multiple correlation coefficient
        double _sdv;             // Standard deviation of errors
        double _fReg;            // Fisher F statistic for regression
        double[] _ycalc;         // Calculated values of Y
        double[] _dy;            // Residual values of Y

        public double FisherF => _fReg;

        public double CorrelationCoefficient => _rysq;

        public double StandardDeviation => _sdv;

        public double[] CalculatedValues => _ycalc;

        public double[] Residuals => _dy;

        public double[] Coefficients => C;

        public double[] CoefficientsStandardError => Sec;

        public double[,] VarianceMatrix => _v;

        /// <summary>
        /// Performs a linear regression on the data in Y (independent), X (dependent) and W (weights).
        /// </summary>
        /// <param name="y">The dependent values in the data series.</param>
        /// <param name="x">The independent values in the data series.</param>
        /// <param name="w">The weights assigned to the corresponding X,Y pairs.</param>
        /// <param name="order">The order of solution desired (1=average, 2=line, 3=parabola, etc.)</param>
        /// <returns>True if the regression was successful.</returns>
        public bool Regress(double[] y, double[] x, double[] w, int order)
        {
            return Regress(y, GetExpandedXMatrix(x, order), w);
        }

        private double[,] GetExpandedXMatrix(double[] xArr, int order)
        {
            double[,] x = new double[order, xArr.Length];
            for (int ndxSample = 0; ndxSample < xArr.Length; ndxSample++)
            {
                double sample = xArr[ndxSample];
                x[0, ndxSample] = 1;
                for (int ndxOrder = 1; ndxOrder < order; ndxOrder++)
                {
                    x[ndxOrder, ndxSample] = sample;
                    sample *= sample;
                }
            }
            return x;
        }

        /// <summary>
        /// Performs a linear regression on the data in Y (independent), X (dependent.) All data points are given equal weighting.
        /// </summary>
        /// <param name="y">The dependent values in the data series.</param>
        /// <param name="x">The independent values in the data series.</param>
        /// <param name="order">The order of solution desired (1=average, 2=line, 3=parabola, etc.)</param>
        /// <returns>True if the regression was successful.</returns>
        public bool Regress(double[] y, double[] x, int order)
        {
            double[] w = new double[y.Length];
            for (int i = 0; i < w.Length; i++)
            {
                w[i] = 1;
            }
            return Regress(y, GetExpandedXMatrix(x, order), w);
        }

        private bool Regress(double[] y, double[,] x, double[] w)
        {
            int m = y.Length;             // M = Number of data points
            int n = x.Length / m;         // N = Number of linear terms
            int ndf = m - n;              // Degrees of freedom
            _ycalc = new double[m];
            _dy = new double[m];
            // If not enough data, don't attempt regression
            if (ndf < 1)
            {
                return false;
            }
            _v = new double[n, n];
            C = new double[n];
            Sec = new double[n];
            double[] b = new double[n];   // Vector for LSQ

            // Clear the matrices to start out
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    _v[i, j] = 0;

            // Form Least Squares Matrix
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    _v[i, j] = 0;
                    for (int k = 0; k < m; k++)
                        _v[i, j] = _v[i, j] + w[k] * x[i, k] * x[j, k];
                }
                b[i] = 0;
                for (int k = 0; k < m; k++)
                    b[i] = b[i] + w[k] * x[i, k] * y[k];
            }
            // V now contains the raw least squares matrix
            if (!SymmetricMatrixInvert(_v))
            {
                return false;
            }
            // V now contains the inverted least square matrix
            // Matrix multpily to get coefficients C = VB
            for (int i = 0; i < n; i++)
            {
                C[i] = 0;
                for (int j = 0; j < n; j++)
                    C[i] = C[i] + _v[i, j] * b[j];
            }

            // Calculate statistics
            double tss = 0;
            double rss = 0;
            double ybar = 0;
            double wsum = 0;
            for (int k = 0; k < m; k++)
            {
                ybar = ybar + w[k] * y[k];
                wsum = wsum + w[k];
            }
            ybar = ybar / wsum;
            for (int k = 0; k < m; k++)
            {
                _ycalc[k] = 0;
                for (int i = 0; i < n; i++)
                    _ycalc[k] = _ycalc[k] + C[i] * x[i, k];
                _dy[k] = _ycalc[k] - y[k];
                tss = tss + w[k] * (y[k] - ybar) * (y[k] - ybar);
                rss = rss + w[k] * _dy[k] * _dy[k];
            }
            double ssq = rss / ndf;
            _rysq = 1 - rss / tss;
            _fReg = 9999999;
            if (_rysq < 0.9999999)
                _fReg = _rysq / (1 - _rysq) * ndf / (n - 1);
            _sdv = Math.Sqrt(ssq);

            // Calculate var-covar matrix and std error of coefficients
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                    _v[i, j] = _v[i, j] * ssq;
                Sec[i] = Math.Sqrt(_v[i, i]);
            }
            return true;
        }


        public bool SymmetricMatrixInvert(double[,] v)
        {
            int n = (int)Math.Sqrt(v.Length);
            double[] t = new double[n];
            double[] q = new double[n];
            double[] r = new double[n];
            int l, m;

            // Invert a symetric matrix in V
            for (m = 0; m < n; m++)
                r[m] = 1;
            int k = 0;
            for (m = 0; m < n; m++)
            {
                double big = 0;
                for (l = 0; l < n; l++)
                {
                    double ab = Math.Abs(v[l, l]);
                    if ((ab > big) && (r[l] != 0))
                    {
                        big = ab;
                        k = l;
                    }
                }
                if (big == 0)
                {
                    return false;
                }
                r[k] = 0;
                q[k] = 1 / v[k, k];
                t[k] = 1;
                v[k, k] = 0;
                if (k != 0)
                {
                    for (l = 0; l < k; l++)
                    {
                        t[l] = v[l, k];
                        if (r[l] == 0)
                            q[l] = v[l, k] * q[k];
                        else
                            q[l] = -v[l, k] * q[k];
                        v[l, k] = 0;
                    }
                }
                if ((k + 1) < n)
                {
                    for (l = k + 1; l < n; l++)
                    {
                        if (r[l] != 0)
                            t[l] = v[k, l];
                        else
                            t[l] = -v[k, l];
                        q[l] = -v[k, l] * q[k];
                        v[k, l] = 0;
                    }
                }
                for (l = 0; l < n; l++)
                    for (k = l; k < n; k++)
                        v[l, k] = v[l, k] + t[l] * q[k];
            }
            m = n;
            l = n - 1;
            for (k = 1; k < n; k++)
            {
                m = m - 1;
                l = l - 1;
                for (int j = 0; j <= l; j++)
                    v[m, j] = v[j, m];
            }
            return true;
        }

        // TODO: Move this into a test class.
        //public double RunTest(double[] x)
        //{
        //    int nRuns = 1;
        //    int n1 = 0;
        //    int n2 = 0;
        //    if (x[0] > 0)
        //        n1 = 1;
        //    else
        //        n2 = 1;

        //    for (int k = 1; k < x.Length; k++)
        //    {
        //        if (x[k] > 0)
        //            n1++;
        //        else
        //            n2++;
        //        if (x[k] * x[k - 1] < 0)
        //            nRuns++;
        //    }
        //    return 1;
        //}

    }
}
