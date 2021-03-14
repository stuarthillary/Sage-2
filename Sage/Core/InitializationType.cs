/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// This enumeration describes when in the lifecycle of a model, the initializer is called.
    /// </summary>
    public enum InitializationType
    {
        /// <summary>
        /// The initializer is called during model setup, in the transition from Dirty to initialized.
        /// </summary>
        PreRun,
        /// <summary>
        /// The initializer is called during model run, while the model is in the running state.
        /// </summary>
        RunTime
    }
}
