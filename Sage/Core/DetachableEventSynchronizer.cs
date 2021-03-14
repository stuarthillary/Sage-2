/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using System.Diagnostics;

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// Synchronizes a series of threads that are all running on DetachableEvent handlers.
    /// The clients will call Synchronize on DetachableEvent threads, and the threads will
    /// pause until all open channels contain paused threads. Then, in the specified sequence,
    /// all clients will be resumed.
    /// </summary>
    public class DetachableEventSynchronizer
    {
        private readonly IModel _model;
        private readonly IExecutive _exec;
        private readonly ArrayList _synchChannels;
        private readonly SortedList _waiters;

        /// <summary>
        /// Creates a DetachableEventSynchronizer and attaches it to the specified executive.
        /// </summary>
        /// <param name="model">The model that owns the executive that will be managing all of the Detachable Events
        /// that are to be synchronized.</param>
        public DetachableEventSynchronizer(IModel model)
        {
            _model = model;
            _exec = _model.Executive;
            _synchChannels = new ArrayList();
            _waiters = new SortedList();
        }

        /// <summary>
        /// Acquires a Synchronization Channel. A synch channel is used once by one
        /// object that wishes to be synchronized. Once all channels that have been
        /// acquired from a synchronizer have had their 'Synchronize' methods called,
        /// all channels' users are allowed to proceed. Note that the constructor need
        /// not be called from a DetachableEvent thread - but Synchronize(...) will
        /// need to be.
        /// </summary>
        /// <param name="sequencer">A sequence indicator that determines which synch
        /// channels' owners are instructed to proceed first.</param>
        /// <returns>A Synch Channel with the assigned priority.</returns>
        public ISynchChannel GetSynchChannel(IComparable sequencer)
        {
            ISynchChannel synchChannel = new SynchChannel(this, sequencer);
            _synchChannels.Add(synchChannel);
            return synchChannel;
        }

        private void LogSynchronization(object sortKey, IDetachableEventController idec, SynchChannel sc)
        {
            if (_waiters.ContainsValue(idec))
            {
                throw new ApplicationException("Synchronize(...) called on a SynchChannel that is already waiting.");
            }
            if (!_synchChannels.Contains(sc))
            {
                throw new ApplicationException("SynchChannel applied to a synchronizer that did not own it - serious error.");
            }

            if ((_waiters.Count + 1) == _synchChannels.Count)
            {
                _exec.RequestEvent(new ExecEventReceiver(LaunchAll), _exec.Now, _exec.CurrentPriorityLevel, null);
            }
            _waiters.Add(sortKey, idec);
            idec.SetAbortHandler(new DetachableEventAbortHandler(idec_AbortionEvent));
            idec.Suspend();
            idec.ClearAbortHandler();
        }

        private void LaunchAll(IExecutive exec, object userData)
        {
            foreach (IDetachableEventController idec in _waiters.Values)
            {
                idec.Resume();
            }
        }

        /// A synch channel is obtained from a Synchronizer, and
        /// is a one-use, one-client mechanism for gating execution. Once all clients that have 
        /// acquired synch channels have called 'Synchronize' on those channels, they are all allowed
        /// to proceed, in the order implied by the Sequencer IComparable.
        internal class SynchChannel : ISynchChannel
        {

            #region Private Fields

            private readonly DetachableEventSynchronizer _ds;
            private readonly IComparable _sortKey;

            #endregion 

            /// <summary>
            /// Creates a new instance of the <see cref="T:SynchChannel"/> class.
            /// </summary>
            /// <param name="ds">The <see cref="Highpoint.Sage.SimCore.DetachableEventSynchronizer"/>.</param>
            /// <param name="sortKey">The sort key.</param>
			public SynchChannel(DetachableEventSynchronizer ds, IComparable sortKey)
            {
                _ds = ds;
                _sortKey = sortKey;
            }

            /// <summary>
            /// Gets a sequencer that can be used in a Sort operation to determine the order in which the
            /// clients of a Synchronizer are allowed to proceed.
            /// </summary>
            /// <value>The sequencer.</value>
			public IComparable Sequencer
            {
                get
                {
                    return _sortKey;
                }
            }

            #region ISynchronizer Members
            /// <summary>
            /// Called by a synch channel to indicate that it is ready to proceed. It will be
            /// allowed to do so once all clients have called this method.
            /// </summary>
            public void Synchronize()
            {
                IDetachableEventController idec = _ds._exec.CurrentEventController;
                _ds.LogSynchronization(_sortKey, idec, this);
            }
            #endregion
        }

        private void idec_AbortionEvent(IExecutive exec, IDetachableEventController idec, params object[] args)
        {
            string narrative = "A synchronizer failed to complete. It had " + _synchChannels.Count
                + " synch channels - following is the stack trace:\r\n" + new StackTrace(true);
            _model.AddWarning(new GenericModelWarning("Synchronizer Aborted", narrative, this, null));
        }
    }
}
