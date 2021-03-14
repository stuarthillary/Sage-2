/* This source code licensed under the GNU Affero General Public License */
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased
{
    /// <summary>
    /// Implemented by an object that adds tags to service objects it handles.
    /// </summary>
    public interface IAddsTagsToServiceObjects
    {
        /// <summary>
        /// Gets a list of the tag types that can be added to service objects.
        /// </summary>
        /// <value>The tag types affected.</value>
        List<ITagType> TagTypesAffected
        {
            get;
        }
    }
}
