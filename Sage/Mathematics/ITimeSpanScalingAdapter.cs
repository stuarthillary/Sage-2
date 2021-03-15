/* This source code licensed under the GNU Affero General Public License */

using System;
#pragma warning disable 1587

namespace Highpoint.Sage.Mathematics.Scaling
{
    /// <summary>
    /// Implemented by an object that has a TimeSpan value that can be scaled.
    /// </summary>
    public interface ITimeSpanScalingAdapter : IScalable
    {
        /// <summary>
        /// Gets the current value of the data that is scaled by this adapter.
        /// </summary>
        /// <value>The current value of the data that is scaled by this adapter.</value>
        TimeSpan CurrentValue
        {
            get;
        }

        /// <summary>
        /// Gets the value of the data that is scaled by this adapter when scale is 1.0.
        /// </summary>
        TimeSpan FullScaleValue
        {
            get;
        }

        /// <summary>
        /// Clones this ITimeSpanScalingAdapter.
        /// </summary>
        /// <returns>The cloned instance.</returns>
        ITimeSpanScalingAdapter Clone();
    }
}