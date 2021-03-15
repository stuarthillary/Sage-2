/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Utility
{
    /// <summary>
    /// Implemeted by an object that is sortable, and has SortKeys
    /// </summary>
    public interface IHasSortKeys
    {
        /// <summary>
        /// Gets the sort keys for this object.
        /// </summary>
        /// <value>The sort keys.</value>
		ISortKey[] SortKeys
        {
            get;
        }
    }
}
