/* This source code licensed under the GNU Affero General Public License */

#pragma warning disable 1587

/// <summary>
/// Scaling is an operation that transforms an object or set of objects, typically a
/// Material Transfer Request.
/// This transformation may occur in one or many dimensions, and some
/// dimensions may not be transformed equally. The entity doing the scaling
/// would like not to have to remember the original scale, but would like
/// to be able to rescale to the original size by setting scale to unity.
/// Scaling is seen as a monolithic event - that is, the scaling operation
/// applies to the entire entity at once, and the entity now exists at the
/// new scale, without having to remember its own "unity" scale.<p></p><p></p>
/// The scaling architecture allows the developer to decorate objects with
/// other object that apply scale to them.<p></p>
/// Any object that implements IScalable may be scaled by attaching a
/// scaling engine. The scaling engine must then be connected to the
/// scalable object through a scaling adapter.<p></p> 
/// When a scaling operation is desired on an object, call that object's
/// scaling engine, and set a new scale.
/// </summary>
namespace Highpoint.Sage.Mathematics.Scaling
{

    /// <summary>
    /// This interface is implemented by any object that can be scaled.
    /// </summary>
    public interface IScalable
    {
        /// <summary>
        /// Called to command a rescaling operation where the scalable object is
        /// rescaled directly to a given scale. +1.0 sets the scale to it's original
        /// value. +2.0 sets the scale to twice its original value, +0.5 sets the
        /// scale to half of its original value.
        /// </summary>
        /// <param name="newScale">The new scale for the IScalable.</param>
        void Rescale(double newScale);

    }
}