/* This source code licensed under the GNU Affero General Public License */
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased
{
    /// <summary>
    /// A list of tags.
    /// </summary>
    public class TagList : List<ITag>
    {

        /// <summary>
        /// Filters the list on those tags with the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A sublist consisting only of tags with the specified type.</returns>
        public List<ITag> FilterOn(ITagType type)
        {
            return FindAll(delegate (ITag tag)
            {
                return tag.TagType.Equals(type);
            });
        }

        /// <summary>
        /// Filters the list on those tags with the specified type.
        /// </summary>
        /// <param name="typeName">The name of the type.</param>
        /// <returns>
        /// A sublist consisting only of tags with the specified type name.
        /// </returns>
        public List<ITag> FilterOn(string typeName)
        {
            return FindAll(delegate (ITag tag)
            {
                return tag.TagType.TypeName.Equals(typeName);
            });
        }

        /// <summary>
        /// Gets the <see cref="Highpoint.Sage.ItemBased.ITag"/> with the specified name.
        /// </summary>
        /// <value></value>
        public ITag this[string name]
        {
            get
            {
                return Find(delegate (ITag tag)
                {
                    return tag.Name.Equals(name);
                });
            }
        }
    }
}
