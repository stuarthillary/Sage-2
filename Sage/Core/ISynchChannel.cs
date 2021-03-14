/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// Implemented by a synch channel. A synch channel is obtained from a Synchronizer, and
    /// is a one-use, one-client mechanism for gating execution. Once all clients that have 
    /// acquired synch channels have called 'Synchronize' on those channels, they are all allowed
    /// to proceed, in the order implied by the Sequencer IComparable.
    /// </summary>
    public interface ISynchChannel
    {
        /// <summary>
        /// Called by a synch channel to indicate that it is ready to proceed. It will be
        /// allowed to do so once all clients have called this method.
        /// </summary>
        void Synchronize();

        /// <summary>
        /// Gets a sequencer that can be used in a Sort operation to determine the order in which the 
        /// clients are allowed to proceed.
        /// </summary>
        /// <value>The sequencer.</value>
		IComparable Sequencer
        {
            get;
        }
    }
}
