/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.SimCore
{

    /// <summary>
    /// Interface implemented by all synchronizers. A Synchronizer is an object
    /// that is capable of making sure that things that could otherwise take place
    /// at differing times, occur at the same time.
    /// </summary>
    public interface ISynchronizer
    {
        /// <summary>
        /// Acquires a Synchronization Channel. A synch channel is used once by one
        /// object that wishes to be synchronized. Once all channels that have been
        /// acquired from a synchronizer have had their 'Synchronize' methods called,
        /// all channels' users are allowed to proceed.
        /// </summary>
        /// <param name="sequence">A sequence indicator that determines which synch
        /// channels' owners are instructed to proceed first. Lesser IComparables go
        /// first.</param>
        /// <returns>A Synch Channel with the assigned priority.</returns>
        ISynchChannel GetSynchChannel(IComparable sequence);
    }
}
