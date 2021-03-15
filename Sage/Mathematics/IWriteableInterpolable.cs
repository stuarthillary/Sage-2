/* This source code licensed under the GNU Affero General Public License */

// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// Implemented by an object that provides an interpolatable Y value for some set of X values, where the specific requested x may not be known to the object - in addition, at run time, additional known (x,y) values can be provided.
    /// </summary>
    public interface IWriteableInterpolable : IInterpolable
    {
        /// <summary>
        /// Sets the y value for the specified known x value.
        /// </summary>
        /// <param name="xValue">The x value.</param>
        /// <param name="yValue">The y value.</param>
        void SetYValue(double xValue, double yValue);
    }
}
