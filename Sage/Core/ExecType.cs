/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.SimCore
{
    public enum ExecType
    {
        /// <summary>
        /// The full featured executive is multi-threaded, and supports the full API.
        /// </summary>
        FullFeatured,
        /// <summary>
        /// The single threaded executive obtains highest performance through a limited feature set.
        /// </summary>
        SingleThreaded
    }
}