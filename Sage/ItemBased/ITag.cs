/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.ItemBased
{
    /// <summary>
    /// A tag that can be read and written.
    /// </summary>
    public interface ITag : IReadOnlyTag
    {
        bool SetValue(string newValue);
    }
}
