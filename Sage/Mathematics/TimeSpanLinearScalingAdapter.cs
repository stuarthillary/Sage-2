/* This source code licensed under the GNU Affero General Public License */

using System;
#pragma warning disable 1587

namespace Highpoint.Sage.Mathematics.Scaling
{
    /// <summary>
    /// A class that manages linear scaling of a TimeSpan. If linearity is 2.0, for example, 
    /// a rescaling of 2.0 quadruples the underlying value, and a rescaling of 0.5 quarters
    /// the underlying value. Slope of the scaling line.
    /// </summary>
    public class TimeSpanLinearScalingAdapter : ITimeSpanScalingAdapter {

        #region Private Fields
        private readonly double _linearity;
        private TimeSpan _originalValue;
        private TimeSpan _currentValue;

        #endregion 

        /// <summary>
        /// Creates a new instance of the <see cref="T:TimeSpanLinearScalingAdapter"/> class.
        /// </summary>
        /// <param name="originalValue">The original value of this TimeSpanLinearScalingAdapter's underlying data.</param>
        /// <param name="linearity">The linearity.</param>
        public TimeSpanLinearScalingAdapter(TimeSpan originalValue, double linearity){
            _currentValue = _originalValue = originalValue;
            _linearity = linearity;
        }

        /// <summary>
        /// Rescales the underlying data by the specified aggregate scale, taking this TimeSpanLinearScalingAdapter's linearity into account.
        /// </summary>
        /// <param name="aggregateScale">The aggregate scale.</param>
        public void Rescale(double aggregateScale){
            double factor = (1-(_linearity*(1-aggregateScale)));
            _currentValue = TimeSpan.FromTicks((long)(_originalValue.Ticks*factor));
        }

        /// <summary>
        /// Gets the current value of the data that is scaled by this adapter.
        /// </summary>
        /// <value>The current value of the data that is scaled by this adapter.</value>
        public TimeSpan CurrentValue => _currentValue;

        /// <summary>
        /// Gets the value of the data that is scaled by this adapter when scale is 1.0.
        /// </summary>
        /// <value></value>
        public TimeSpan FullScaleValue => _originalValue;

        /// <summary>
        /// Clones this TimeSpanLinearScalingAdapter.
        /// </summary>
        /// <returns>The cloned instance.</returns>
		public ITimeSpanScalingAdapter Clone(){
			return new TimeSpanLinearScalingAdapter(_originalValue,_linearity);
		}

        /// <summary>
        /// Gets the original value of the data that is scaled by this adapter.
        /// </summary>
        /// <value>The original value of the data that is scaled by this adapter.</value>
		public TimeSpan OriginalValue => _originalValue;

        /// <summary>
        /// Gets the linearity of this TimeSpanLinearScalingAdapter.
        /// </summary>
        /// <value>The linearity.</value>
		public double Linearity => _linearity;
    }
}