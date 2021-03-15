/* This source code licensed under the GNU Affero General Public License */

#pragma warning disable 1587

namespace Highpoint.Sage.Mathematics.Scaling
{
    /// <summary>
    /// Implemented by an object that has a double value that can be scaled.
    /// </summary>
    public interface IDoubleScalingAdapter : IScalable
    {
        /// <summary>
        /// Gets the current value of the data that is scaled by this adapter.
        /// </summary>
        /// <value>The current value of the data that is scaled by this adapter.</value>
        double CurrentValue
        {
            get;
        }

        /// <summary>
        /// Gets the value of the data that is scaled by this adapter when scale is 1.0.
        /// </summary>
        /// <value>The full scale value.</value>
        double FullScaleValue
        {
            get;
        }

        /// <summary>
        /// Clones this IDoubleScalingAdapter.
        /// </summary>
        /// <returns>The closed instance.</returns>
		IDoubleScalingAdapter Clone();
    }
}