/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;

namespace Highpoint.Sage.Resources
{
    public interface IResourceManagerCollection
    {
        /// <summary>
        /// Adds a resource manager to the model. Fires the ResourceManagerAdded event. A resource manager is any
        /// implementer of IResourceManager, including a self-managing resource.
        /// </summary>
        /// <param name="manager">The manager to be added.</param>
        void Add(IResourceManager manager);

        /// <summary>
        /// Removes a resource manager from the model. Fires the ResourceManagerRemoved event.
        /// </summary>
        /// <param name="manager">The manager to be removed.</param>
        void Remove(IResourceManager manager);

        /// <summary>
        /// Retrieves a resource manager that is known to the SOMModel, by its guid.
        /// </summary>
        /// <param name="guid">The guid for which the resource manager is requested.</param>
        /// <returns>The resource manager for the quid that was requested.</returns>
        IResourceManager GetResourceManager(Guid guid);

        /// <summary>
        /// Returns a collection of all resource managers known to this collection.
        /// </summary>
        /// <returns></returns>
        ICollection GetResourceManagers();

        /// <summary>
        /// Fired when a resource manager is added to the model.
        /// </summary>
        event ResourceManagerChangeListener ResourceManagerAdded;

        /// <summary>
        /// Fired when a resource manager is removed from the model.
        /// </summary>
        event ResourceManagerChangeListener ResourceManagerRemoved;
    }
}
