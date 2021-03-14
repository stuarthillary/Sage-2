/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.ItemBased
{
    /// <summary>
    /// Implemented by an object (usually a service item) that has tags attached.
    /// </summary>
    public interface ITagHolder
    {
        /// <summary>
        /// Gets the tags held by this service item.
        /// </summary>
        /// <value>The tags.</value>
        TagList Tags
        {
            get;
        }
    }
}
