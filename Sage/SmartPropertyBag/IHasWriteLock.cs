/* This source code licensed under the GNU Affero General Public License */

// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.SimCore
{

    /// <summary>
    /// Implemented by an object that can be set to read-only.
    /// </summary>
    public interface IHasWriteLock
    {

        /// <summary>
        /// Gets a value indicating whether this instance is currently writable.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is writable; otherwise, <c>false</c>.
        /// </value>
        bool IsWritable
        {
            get;
        }
    }

}