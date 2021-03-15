/* This source code licensed under the GNU Affero General Public License */

// ReSharper disable UnusedMemberInSuper.Global
#pragma warning disable 1587

namespace Highpoint.Sage.Resources
{
    /// <summary>
    /// Implemented by a class (usually a resource) that has a quantity that can be considered
    /// capacity.
    /// </summary>
    public interface IHasCapacity
    {

        /// <summary>
        /// The capacity of this resource that will be in effect if the resource experiences a reset.
        /// </summary>
        double InitialCapacity
        {
            get;
        }

        /// <summary>
        /// The current capacity of this resource - how much 'Available' can be, at its highest value.
        /// </summary>
        double Capacity
        {
            get;
        }

        /// <summary>
        /// The amount of a resource that can be acquired over and above the amount that is actually there.
        /// It is illegal to set PermissibleOverbook quantity on an atomic resource, since atomicity implies
        /// that all or none are granted anyway.
        /// </summary>
        double PermissibleOverbook
        {
            get;
        }

        /// <summary>
        /// The quantity of this resource that will be available if the resource experiences a reset.
        /// </summary>
        double InitialAvailable
        {
            get;
        }

        /// <summary>
        /// How much of this resource is currently available to service requests.
        /// </summary>
        double Available
        {
            get;
        }
    }
}
