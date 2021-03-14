/* This source code licensed under the GNU Affero General Public License */
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased
{
    /// <summary>
    /// Implemented by an object that changes the values of the tags on service objects it handles.
    /// </summary>
    public interface IChangesTagsOnServiceObjects
    {
        /// <summary>
        /// Gets a list of the tag types that can be changed on service objects.
        /// </summary>
        /// <value>The tag types affected.</value>
        List<ITagType> TagTypesAffected
        {
            get;
        }
    }
}
