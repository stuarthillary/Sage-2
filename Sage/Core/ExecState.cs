/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// Describes the state of the executive.
    /// </summary>
    public enum ExecState
    {
        /// <summary>
        /// The executive is stopped.
        /// </summary>
        Stopped,
        /// <summary>
        /// The executive is running.
        /// </summary>
        Running,
        /// <summary>
        /// The executive is paused.
        /// </summary>
        Paused,
        /// <summary>
        /// The executive is finished.
        /// </summary>
        Finished
    }
}
