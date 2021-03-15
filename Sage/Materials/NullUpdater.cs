/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Materials.Chemistry;

namespace Highpoint.Sage.Materials
{
    /// <summary>
    /// A dummy that makes no updates to any materials.
    /// </summary>
    internal class NullUpdater : IUpdater
    {
        /// <summary>
        /// Performs the update operation that this implementer performs.
        /// </summary>
        /// <param name="initiator">The material to be updated (along with any dependent materials.)</param>
        public void DoUpdate(IMaterial initiator)
        {
        }
        /// <summary>
        /// Causes this updater no longer to perform alterations on the targeted mixture. This may not be implemented in some cases.
        /// </summary>
        /// <param name="detachee">The detachee.</param>
        public void Detach(IMaterial detachee)
        {
        }
    }
}