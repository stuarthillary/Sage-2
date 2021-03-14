/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// Encapsulates the reason for a transition failure, including what went wrong,
    /// and where.
    /// </summary>
    public interface ITransitionFailureReason
    {
        /// <summary>
        /// What went wrong.
        /// </summary>
        string Reason
        {
            get;
        }
        /// <summary>
        /// Where the problem arose.
        /// </summary>
        object Object
        {
            get;
        }
    }

}
