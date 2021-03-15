/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.SimCore;
using System;
using System.Collections;
// ReSharper disable InconsistentlySynchronizedField

namespace Highpoint.Sage.Utility
{
    /// <summary>
    /// An exchange is a place where objects can post, read and or take tokens, either with a blocking
    /// or non-blocking call.
    /// </summary>
    public class Exchange : ITupleSpace
    {

        #region >>> Private Fields <<<
        private static readonly double _readPriority = double.MaxValue;
        private static readonly double _takePriority = _readPriority - double.Epsilon;
        private static readonly double _postPriority = _takePriority - double.Epsilon;
        private readonly IExecutive _exec;
        private readonly Hashtable _ts;
        private readonly HashtableOfLists _waitersToRead;
        private readonly HashtableOfLists _waitersToTake;
        private readonly Hashtable _blockedPosters;
        #endregion

        /// <summary>
        /// Creates a new instance of the <see cref="T:Highpoint.Sage.Utility.Exchange"/> class.
        /// </summary>
        /// <param name="exec">The exec.</param>
        public Exchange(IExecutive exec)
        {
            _exec = exec;
            // TODO: Add a GracefulAbort(...) to IDetachableEventController. 
            // exec.ExecutiveFinished +=new ExecutiveEvent(exec_ExecutiveFinished);
            _ts = Hashtable.Synchronized(new Hashtable());
            _waitersToRead = new HashtableOfLists();
            _waitersToTake = new HashtableOfLists();
            _blockedPosters = new Hashtable();
        }

        #region ITupleSpace Members

        /// <summary>
        /// Gets a value indicating whether this TupleSpace permits multiple Tuples to be posted under the same key. Currently, this will be false.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this TupleSpace permits multiple Tuples to be posted under the same key; otherwise, <c>false</c>.
        /// </value>
        public bool PermitsDuplicateKeys => false;

        /// <summary>
        /// Posts the specified tuple. If blocking is true, this call blocks
        /// the caller's thread until the Tuple is taken from the space by another caller.
        /// </summary>
        /// <param name="tuple">The tuple.</param>
        /// <param name="blocking">if set to <c>true</c> this call blocks
        /// the caller's thread until the Tuple is taken from the space by another caller..</param>
        public void Post(ITuple tuple, bool blocking)
        {
            if (blocking)
            {
                BlockingPost(tuple);
            }
            else
            {
                NonBlockingPost(tuple);
            }
        }

        /// <summary>
        /// Posts a Tuple with the specified key and data. If blocking is true, this call blocks
        /// the caller's thread until the Tuple is taken from the space by another caller.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="data">The data.</param>
        /// <param name="blocking">if set to <c>true</c> [blocking].</param>
        public void Post(object key, object data, bool blocking)
        {
            Post(new TupleWrapper(key, data), blocking);
        }

        /// <summary>
        /// Reads the specified key, returning null if it is not present in the TupleSpace.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="blocking">if set to <c>true</c> [blocking].</param>
        /// <returns>The Tuple stored under the specified key</returns>
        public ITuple Read(object key, bool blocking)
        {
            return (blocking ? BlockingRead(key) : NonBlockingRead(key));
        }

        /// <summary>
        /// Takes the Tuple entered under the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="blocking">if set to <c>true</c> the calling thread will not return until a Tuple has been found with the
        /// specified key value.</param>
        /// <returns>The Tuple stored under the specified key</returns>
        public ITuple Take(object key, bool blocking)
        {
            return (blocking ? BlockingTake(key) : NonBlockingTake(key));
        }

        /// <summary>
        /// Blocks the calling thread until the specified key is not in the TupleSpace.
        /// </summary>
        /// <param name="key">The key.</param>
        public void BlockWhilePresent(object key)
        {
            #region Ensure that this is a Detachable Event.
#if DEBUG
            if (_exec.CurrentEventType != ExecEventType.Detachable)
                throw new ApplicationException("Attempt to do a blocking post with an executive event that is not Detachable.");
#endif
            #endregion

            ITuple tuple;
            lock (_ts)
            {
                tuple = (ITuple)_ts[key];
            }
            if (tuple != null)
            {
                new BlockTilKeyGoneHandler(this, tuple).Run();
            }
        }

        #region TupleEvents
        public event TupleEvent TuplePosted;
        public event TupleEvent TupleRead;
        public event TupleEvent TupleTaken;
        #endregion
        #endregion

        private void BlockingPost(ITuple tuple)
        {
            #region Ensure that this is a Detachable Event.
#if DEBUG
            if (_exec.CurrentEventType != ExecEventType.Detachable)
                throw new ApplicationException("Attempt to do a blocking post with an executive event that is not Detachable.");
#endif
            #endregion

            NonBlockingPost(tuple);

            #region Wait 'til next take against this key.
            IDetachableEventController idec = _exec.CurrentEventController;
            _blockedPosters.Add(tuple.Key, idec);
            idec.Suspend();
            #endregion
        }
        private void NonBlockingPost(ITuple tuple)
        {
            _ts.Add(tuple.Key, tuple);
            tuple.OnPosted(this);
            TuplePosted?.Invoke(this, tuple);
            foreach (IDetachableEventController idec in _waitersToRead[tuple.Key])
                idec.Resume(_readPriority);
            foreach (IDetachableEventController idec in _waitersToTake[tuple.Key])
                idec.Resume(_takePriority);
            _waitersToRead.Remove(tuple.Key);
            _waitersToTake.Remove(tuple.Key);
        }
        private ITuple BlockingRead(object key)
        {
            #region Ensure that this is a Detachable Event.
#if DEBUG
            if (_exec.CurrentEventType != ExecEventType.Detachable)
                throw new ApplicationException("Attempt to do a blocking read with an executive event that is not Detachable.");
#endif
            #endregion
            while (true)
            {
                ITuple tuple = NonBlockingRead(key);
                if (tuple != null)
                    return tuple;
                #region Wait 'til next post against this key.
                IDetachableEventController idec = _exec.CurrentEventController;
                _waitersToRead.Add(key, idec);
                idec.Suspend();
                #endregion

            }
        }
        private ITuple NonBlockingRead(object key)
        {
            ITuple tuple = (ITuple)_ts[key];
            if (tuple != null)
            {
                tuple.OnRead(this);
                TupleRead?.Invoke(this, tuple);
            }
            return tuple;
        }
        private ITuple BlockingTake(object key)
        {

            #region Ensure that this is a Detachable Event.
#if DEBUG
            if (_exec.CurrentEventType != ExecEventType.Detachable)
                throw new ApplicationException("Attempt to do a blocking take with an executive event that is not Detachable.");
#endif
            #endregion

            while (true)
            {
                ITuple tuple = NonBlockingTake(key);
                if (tuple != null)
                    return tuple;

                #region Wait 'til next take against this key.
                IDetachableEventController idec = _exec.CurrentEventController;
                _waitersToTake.Add(key, idec);
                idec.Suspend();
                #endregion
            }
        }
        private ITuple NonBlockingTake(object key)
        {
            ITuple tuple;
            lock (_ts)
            {
                tuple = (ITuple)_ts[key];
                if (tuple != null)
                {

                    _ts.Remove(key);
                    tuple.OnTaken(this);
                    TupleTaken?.Invoke(this, tuple);

                    IDetachableEventController blockedPoster = (IDetachableEventController)_blockedPosters[tuple.Key];
                    if (blockedPoster != null)
                    {
                        _blockedPosters.Remove(tuple.Key);
                        blockedPoster.Resume(_postPriority);
                    }
                }
            }
            return tuple;
        }

        //private void exec_ExecutiveFinished(IExecutive exec) {
        //    foreach (IDetachableEventController idec in m_waitersToRead)
        //        idec.GracefulAbort();
        //    foreach (IDetachableEventController idec in m_waitersToTake)
        //        idec.GracefulAbort();
        //    m_waitersToRead.Clear();
        //    m_waitersToTake.Clear();
        //}

        private class BlockTilKeyGoneHandler
        {
            private readonly IDetachableEventController _idec;
            private readonly ITuple _tuple;
            private readonly Exchange _exchange;
            private readonly TupleEvent _myEvent;

            public BlockTilKeyGoneHandler(Exchange exchange, ITuple tuple)
            {
                _exchange = exchange;
                _idec = _exchange._exec.CurrentEventController;
                _tuple = tuple;
                _myEvent = exchange_TupleTaken;
            }

            public void Run()
            {
                _exchange.TupleTaken += _myEvent;
                _idec.Suspend();
            }

            void exchange_TupleTaken(ITupleSpace space, ITuple tuple)
            {
                if (tuple.Equals(_tuple))
                {
                    _exchange.TupleTaken -= _myEvent;
                    _idec.Resume();
                }
            }
        }

        private class TupleWrapper : ITuple
        {
            public TupleWrapper(object key, object data)
            {
                Key = key;
                Data = data;
            }

            #region ITuple Members

            public object Key
            {
                get;
            }

            public object Data
            {
                get;
            }

            public void OnPosted(ITupleSpace ts)
            {
            }

            public void OnRead(ITupleSpace ts)
            {
            }

            public void OnTaken(ITupleSpace ts)
            {
            }

            #endregion
        }
    }
}