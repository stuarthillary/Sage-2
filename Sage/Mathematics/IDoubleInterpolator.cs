/* This source code licensed under the GNU Affero General Public License */

// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// Implemented by an object that performs interpolations on two arrays of doubles (an x and a y array).
    /// </summary>
    public interface IDoubleInterpolator
    {
        /// <summary>
        /// Sets the data used by this interpolator.
        /// </summary>
        /// <param name="xvals">The xvals.</param>
        /// <param name="yvals">The yvals.</param>
		void SetData(double[] xvals, double[] yvals);
        /// <summary>
        /// Gets a value indicating whether this instance has data.
        /// </summary>
        /// <value><c>true</c> if this instance has data; otherwise, <c>false</c>.</value>
		bool HasData
        {
            get;
        }
        /// <summary>
        /// Gets the Y value for the specified x value.
        /// </summary>
        /// <param name="xValue">The X value.</param>
        /// <returns></returns>
		double GetYValue(double xValue);
    }
}
