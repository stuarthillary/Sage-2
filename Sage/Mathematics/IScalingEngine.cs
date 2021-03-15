/* This source code licensed under the GNU Affero General Public License */

#pragma warning disable 1587

namespace Highpoint.Sage.Mathematics.Scaling
{
    /// <summary>
    /// An object that is able to apply scaling to another object. It can
    /// also subsequently remove that scaling from the same object. 
    /// </summary>
    public interface IScalingEngine
    {
        /// <summary>
        /// The combined, aggregate scale of all of the subjects of this scaling engine
        /// compared to their original scale.
        /// </summary>
        double AggregateScale
        {
            get; set;
        }

        /// <summary>
        /// Rescales the implementer by the provided factor.
        /// </summary>
        /// <param name="byFactor">The factor.</param>
        void Rescale(double byFactor);
    }
}