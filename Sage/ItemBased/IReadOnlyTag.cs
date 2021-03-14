/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.ItemBased
{
    /// <summary>
    /// A read only Tag.
    /// </summary>
    public interface IReadOnlyTag
    {
        ITagType TagType
        {
            get;
        }
        string Name
        {
            get;
        }
        string Value
        {
            get;
        }
    }
}
