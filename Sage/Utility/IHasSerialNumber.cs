/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Utility
{
    /// <summary>
    /// Implemented by an object that has a serial number.
    /// </summary>
    public interface IHasSerialNumber
    {
        /// <summary>
        /// The object's serial number.
        /// </summary>
        long SerialNumber
        {
            get;
        }
    }
}
