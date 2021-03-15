/* This source code licensed under the GNU Affero General Public License */

// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Resources
{
    /// <summary>
    /// An enumeration of the types of transactions that can take place, involving a resource.
    /// </summary>
    public enum ResourceAction
    {
        /// <summary>
        /// The resource was requested. This does not infer success.
        /// </summary>
        Request,
        /// <summary>
        /// The resource was reserved. This indicates that the resource was taken out of general availability, but not granted.
        /// </summary>
        Reserved,
        /// <summary>
        /// The resource was unreserved. This indicates that the resource was placed back into general availability after having been reserved.
        /// </summary>
        Unreserved,
        /// <summary>
        /// The resource was acquired. This indicates that all or part of the resource's capacity was removed from general availability.
        /// </summary>
        Acquired,
        /// <summary>
        /// The resource was released. This means that all or part of its capacity was placed back into general availability.
        /// </summary>
        Released
    }
}