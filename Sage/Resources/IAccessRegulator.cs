/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Resources
{
    /// <summary>
    /// An object that can be used by a ResourceManager to permit or deny individual
    /// resource aquisition reqests.
    /// </summary>
    public interface IAccessRegulator
    {
        /// <summary>
        /// Returns true if the given subject can be acquired using the presented key.
        /// </summary>
        /// <param name="subject">The resource whose acquisition is being queried.</param>
        /// <param name="usingKey">The key that is to be presented by the prospective acquirer.</param>
        /// <returns>True if the acquire will be allowed, false if not.</returns>
        bool CanAcquire(object subject, object usingKey);
    }
}
