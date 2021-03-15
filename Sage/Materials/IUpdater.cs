/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Materials.Chemistry;

namespace Highpoint.Sage.Materials
{
    /// <summary>
    /// An implementer makes on-request updates to one or more materials.
    /// </summary>
    public interface IUpdater
    {
        /// <summary>
        /// Performs the update operation that this implementer performs.
        /// </summary>
        /// <param name="initiator">The material to be updated (along with any dependent materials.)</param>
        void DoUpdate(IMaterial initiator);
        /// <summary>
        /// Causes this updater no longer to perform alterations on the targeted mixture. This may not be implemented in some cases.
        /// </summary>
        /// <param name="detachee">The detachee.</param>
        void Detach(IMaterial detachee);
    }
}