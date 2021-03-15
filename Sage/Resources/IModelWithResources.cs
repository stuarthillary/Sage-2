/* This source code licensed under the GNU Affero General Public License */

// ReSharper disable UnusedMemberInSuper.Global
#pragma warning disable 1587

namespace Highpoint.Sage.Resources
{
    /// <summary>
    /// Implemented by a model that manages resources.
    /// </summary>
    public interface IModelWithResources
    {

        /// <summary>
        /// Must be called by the creator when a new resource is created.
        /// </summary>
        /// <param name="resource">The resource.</param>
        void OnNewResourceCreated(IResource resource);

        /// <summary>
        /// Event that is fired when a new resource has been created.
        /// </summary>
        event ResourceEvent ResourceCreatedEvent;
    }
}
