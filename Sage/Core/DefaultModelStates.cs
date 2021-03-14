/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// The two default states for a model. The Model's state machine can be replaced with a
    /// custom one, but these are the states of the default state machine.
    /// </summary>
    public enum DefaultModelStates
    {
        /// <summary>
        /// The state machine is idle.
        /// </summary>
        Idle,
        /// <summary>
        /// The state machine is running.
        /// </summary>
        Running
    }
}