/* This source code licensed under the GNU Affero General Public License */

#pragma warning disable 1587

namespace Highpoint.Sage.Mathematics.Scaling
{
    /// <summary>
    /// A class that manages linear scaling of a double. If linearity is 2.0, for example, 
    /// a rescaling of 2.0 quadruples the underlying value, and a rescaling of 0.5 quarters
    /// the underlying value.
    /// </summary>
    public class DoubleLinearScalingAdapter : IDoubleScalingAdapter
    {

        #region Private Fields

        private readonly double _originalValue;
        private readonly double _linearity;
        private double _currentValue;

        #endregion 

        /// <summary>
        /// Creates a new instance of the <see cref="T:DoubleLinearScalingAdapter"/> class.
        /// </summary>
        /// <param name="originalValue">The original value of the underlying data.</param>
        /// <param name="linearity">The linearity.</param>
        public DoubleLinearScalingAdapter(double originalValue, double linearity){
            _currentValue = _originalValue = originalValue;
            _linearity = linearity;
        }

        /// <summary>
        /// Rescales the underlying data by the specified aggregate scale, taking this DoubleLinearScalingAdapter's linearity into account.
        /// </summary>
        /// <param name="aggregateScale">The aggregate scale.</param>
        public void Rescale(double aggregateScale){
            _currentValue = _originalValue*(1-(_linearity*(1-aggregateScale)));
        }

        /// <summary>
        /// Gets the current value of the data that is scaled by this adapter.
        /// </summary>
        /// <value>The current value of the data that is scaled by this adapter.</value>
        public double CurrentValue => _currentValue;

        /// <summary>
        /// Gets the value of the data that is scaled by this adapter when scale is 1.0.
        /// </summary>
        /// <value>The full scale value.</value>
        public double FullScaleValue => _originalValue;

        /// <summary>
        /// Clones this IDoubleScalingAdapter.
        /// </summary>
        /// <returns>The closed instance.</returns>
		public IDoubleScalingAdapter Clone(){
			return new DoubleLinearScalingAdapter(_originalValue,_linearity);
		}

        /// <summary>
        /// Gets the original value of the underlying data.
        /// </summary>
        /// <value>The original value.</value>
		public double OriginalValue => _originalValue;

        /// <summary>
        /// Gets the linearity of this DoubleLinearScalingAdapter.
        /// </summary>
        /// <value>The linearity.</value>
		public double Linearity => _linearity;
    }
}