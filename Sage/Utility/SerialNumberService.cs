/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Utility
{
    /// <summary>
    /// Service that provides serial numbers.
    /// </summary>
    sealed public class SerialNumberService
    {
        private static long _serial;

        /// <summary>
        /// Gets the next serial number from this service.
        /// </summary>
        /// <returns>The next serial number from this service.</returns>
        public static long GetNext()
        {
            return _serial++;
        }
    }
}
