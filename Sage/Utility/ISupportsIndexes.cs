/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Utility
{
    /// <summary>
    /// This interface is implemented by an object that is required to support index numbers.
    /// That is, another (perhaps manager) object will want to refer to objects of this type
    /// by index numbers that it, itself, assigns.
    /// </summary>
    public interface ISupportsIndexes
    {
        /// <summary>
        /// A growable array of index number locations.
        /// </summary>
        uint[] Index
        {
            get;
        }
        /// <summary>
        /// Causes the object to grow its index array.
        /// </summary>
        void GrowIndex();
    }
}
