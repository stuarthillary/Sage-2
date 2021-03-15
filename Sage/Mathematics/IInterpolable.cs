/* This source code licensed under the GNU Affero General Public License */

// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Mathematics
{

    /// <summary>
    /// Implemented by an object that provides an interpolatable Y value for some set of X values, where the specific requested x may not be known to the object.
    /// </summary>
	public interface IInterpolable
    {

        /// <summary>
        /// Gets the Y value that corresponds to the specified x value.
        /// </summary>
        /// <param name="xValue">The x value.</param>
        /// <returns></returns>
        double GetYValue(double xValue);
    }
}
