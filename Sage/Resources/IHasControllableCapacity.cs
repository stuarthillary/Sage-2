/* This source code licensed under the GNU Affero General Public License */

// ReSharper disable UnusedMemberInSuper.Global
#pragma warning disable 1587

namespace Highpoint.Sage.Resources
{
    /// <summary>
    /// Implemented by a class (usually a resource) that has a quantity that can be considered
    /// capacity. In the case of IHasControllableCapacity, though, the current capacity (Available)
    /// and maximum capacity (Capacity) can be overridden.
    /// and 
    /// </summary>
    public interface IHasControllableCapacity
    {

        /// <summary>
        /// The current capacity of this resource - how much 'Available' can be at its highest value.
        /// </summary>
        double Capacity
        {
            get; set;
        }

        /// <summary>
        /// The amount of a resource that can be acquired over and above the amount that is actually there.
        /// It is illegal to set PermissibleOverbook quantity on an atomic resource, since atomicity implies
        /// that all or none are granted anyway.
        /// </summary>
        double PermissibleOverbook
        {
            get; set;
        }

        /// <summary>
        /// How much of this resource is currently available to service requests.
        /// </summary>
        double Available
        {
            get; set;
        }
    }
}
