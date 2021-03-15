/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Persistence;
using System;
// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// This class provides an interpolable data set that uses a linear interpolation
    /// with slope discontinuities at each data point, if the preceding and following
    /// line segments are differently-sloped.
    /// </summary>
    public class SmallDoubleInterpolable : IWriteableInterpolable, IXmlPersistable
    {
        private double[] _xVals, _yVals;
        private int _nEntries;
        private readonly IDoubleInterpolator _interpolator;

        /// <summary>
        /// Constructor for an uninitialized SmallDoubleInterpolable, for persistence operations.
        /// </summary>
		public SmallDoubleInterpolable()
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="T:SmallDoubleInterpolable"/> class which will contain a specified number of data points.
        /// </summary>
        /// <param name="nPoints">The number of data points.</param>
		public SmallDoubleInterpolable(int nPoints)
        {
            _xVals = new double[nPoints];
            _yVals = new double[nPoints];
            foreach (double[] da in new[] { _xVals, _yVals })
            {
                for (int i = 0; i < da.Length; i++)
                    da[i] = double.NaN;
            }
            _nEntries = 0;
            _interpolator = new LinearDoubleInterpolator();
            _interpolator.SetData(_xVals, _yVals);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="T:SmallDoubleInterpolable"/> class with a specified number of points and a provided interpolator.
        /// </summary>
        /// <param name="nPoints">The n points.</param>
        /// <param name="idi">The doubleInterpolator that this <see cref="T:SmallDoubleInterpolable"/> will use.</param>
		public SmallDoubleInterpolable(int nPoints, IDoubleInterpolator idi) : this(nPoints)
        {
            _interpolator = idi;
            if (!_interpolator.HasData)
            {
                _interpolator.SetData(_xVals, _yVals);
            }
        }

        /// <summary>
        /// Creates and initializes a SmallDoubleInterpolable from two arrays of correlated
        /// X and Y values.
        /// </summary>
        /// <param name="xVals"></param>
        /// <param name="yVals"></param>
        public SmallDoubleInterpolable(double[] xVals, double[] yVals) : this(xVals.Length)
        {
            if (xVals.Length != yVals.Length)
                throw new ArgumentException("SmallDoubleInterpolable being initialized with unequal-length arrays.");
            if (xVals.Length < 2)
                throw new ArgumentException(string.Format("Illegal attempt to configure an interpolator on {0} data points.", xVals.Length));
            for (int i = 0; i < xVals.Length; i++)
                SetYValue(xVals[i], yVals[i]);
            // Faster, but depends on values occurring in increasing order.
            //			m_xVals = (double[])xVals.Clone();
            //			m_yVals = (double[])yVals.Clone();
            //			m_nEntries = xVals.Length;
        }
        /// <summary>
        /// Creates and initializes a SmallDoubleInterpolablefrom two arrays of correlated
        /// X and Y values.
        /// </summary>
        /// <param name="xVals">The correlated x values.</param>
        /// <param name="yVals">The correlated y values.</param>
        /// <param name="idi">The IDoubleInterpolator to be used to discern Y values between known x values.</param>
		public SmallDoubleInterpolable(double[] xVals, double[] yVals, IDoubleInterpolator idi) : this(xVals, yVals)
        {
            _interpolator = idi;
            _interpolator.SetData(xVals, yVals);
        }

        /// <summary>
        /// Gets the Y value that corresponds to the specified x value.
        /// </summary>
        /// <param name="xValue">The x value.</param>
        /// <returns></returns>
		public double GetYValue(double xValue)
        {
            return _interpolator.GetYValue(xValue);
        }
        /// <summary>
        /// Sets the y value for the specified known x value.
        /// </summary>
        /// <param name="xValue">The x value.</param>
        /// <param name="yValue">The y value.</param>
		public void SetYValue(double xValue, double yValue)
        {
            if (double.IsNaN(xValue) || double.IsNaN(yValue))
            {
                throw new ApplicationException("Cannot use double.NaN as an X or a Y value in an interpolable.");
            }
            if (double.IsInfinity(xValue) || double.IsInfinity(yValue))
            {
                throw new ApplicationException("Cannot use double.Infinity values as an X or a Y value in an interpolable.");
            }

            // 1.) Find where the new number belongs.
            int insertionPoint = 0;
            for (; (insertionPoint < _xVals.Length && _xVals[insertionPoint] < xValue); insertionPoint++)
            {
            }

            // 2.) If it's an insert, see if we have room. If not, then make room,
            //     and move all data points above the insertion point, up one slot.
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (insertionPoint == _xVals.Length || _xVals[insertionPoint] != xValue)
            {
                if (_nEntries == (_xVals.Length - 1))
                {
                    double[] xTmp = _xVals;
                    double[] yTmp = _yVals;
                    _xVals = new double[xTmp.Length * 2];
                    _yVals = new double[yTmp.Length * 2];
                    Array.Copy(xTmp, _xVals, xTmp.Length);
                    Array.Copy(yTmp, _yVals, yTmp.Length);
                    // Set unused values to double.NaN
                    for (int i = yTmp.Length; i < _yVals.Length; i++)
                    {
                        _xVals[i] = double.NaN;
                        _yVals[i] = double.NaN;
                    }
                    _interpolator.SetData(_xVals, _yVals);
                }

                // Move stuff up to make room.
                for (int i = _nEntries; i >= insertionPoint; i--)
                {
                    _xVals[i + 1] = _xVals[i];
                    _yVals[i + 1] = _yVals[i];
                }
                _nEntries++;
            }

            // 3.) Finally place the value.
            _xVals[insertionPoint] = xValue;
            _yVals[insertionPoint] = yValue;

        }

        #region IXmlPersistable Members

        /// <summary>
        /// Serializes this object to the specified XmlSerializatonContext.
        /// </summary>
        /// <param name="xmlsc">The XmlSerializatonContext into which this object is to be stored.</param>
        public void SerializeTo(XmlSerializationContext xmlsc)
        {
            xmlsc.StoreObject("NumberOfEntries", _nEntries);
            //xmlsc.StoreObject("XVals",m_xVals);
            //xmlsc.StoreObject("YVals",m_yVals);
            for (int i = 0; i < _nEntries; i++)
            {
                xmlsc.StoreObject("XVals_" + i, _xVals[i]);
                xmlsc.StoreObject("YVals_" + i, _yVals[i]);
            }
        }

        /// <summary>
        /// Deserializes this object from the specified XmlSerializatonContext.
        /// </summary>
        /// <param name="xmlsc">The XmlSerializatonContext from which this object is to be reconstituted.</param>
        public void DeserializeFrom(XmlSerializationContext xmlsc)
        {
            _nEntries = (int)xmlsc.LoadObject("NumberOfEntries");
            _xVals = new double[_nEntries];
            _yVals = new double[_nEntries];
            for (int i = 0; i < _nEntries; i++)
            {
                _xVals[i] = (double)xmlsc.LoadObject("XVals_" + i);
                _yVals[i] = (double)xmlsc.LoadObject("YVals_" + i);
            }
        }

        #endregion
    }
}
