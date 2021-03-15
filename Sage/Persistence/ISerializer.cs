/* This source code licensed under the GNU Affero General Public License */

using System.Collections;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace Highpoint.Sage.Persistence
{
    /// <summary>
    /// An object that implements ISerializer knows how to store one or more types of objects
    /// into an archive, and subsequently, to take tham out of the archive. The object that
    /// implements this interface might be thought of as an archive.
    /// </summary>
    public interface ISerializer
    {

        /// <summary>
        /// Stores the object 'obj' under the key 'key'.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="obj">The object.</param>
        void StoreObject(object key, object obj);

        /// <summary>
        /// Loads the object stored under the key, 'key'.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>System.Object.</returns>
        object LoadObject(object key);

        /// <summary>
        /// Resets this instance.
        /// </summary>
        void Reset();

        /// <summary>
        /// Gets the context entities.
        /// </summary>
        /// <value>The context entities.</value>
        Hashtable ContextEntities
        {
            get;
        }
    }
}