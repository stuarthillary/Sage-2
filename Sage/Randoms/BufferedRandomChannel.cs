/* This source code licensed under the GNU Affero General Public License */
using System.Threading;
// ReSharper disable ClassNeverInstantiated.Global

namespace Highpoint.Sage.Randoms
{
    internal class BufferedRandomChannel : RandomChannel
    {

        #region Private Fields
        private static readonly int min_Buffer_Size = 10;
        private readonly object _lockObject = new object();
        private Thread _bufferThread;
        private int _bufferSize;
        private ulong[] _bufferA;
        private ulong[] _bufferB;
        private ulong[] _inUse;
        private int _nextInUseCell;
        private ulong[] _beingFilled;
        private int _nFills;
        private int _nExpectedFills = 1;
        #endregion

        public BufferedRandomChannel(ulong seed, int bufferSize) : base(seed)
        {
            GetNextULong = BufferedGetNextULong;
            Init(bufferSize);
        }

        public BufferedRandomChannel(ulong[] initArray, int bufferSize) : base(initArray)
        {
            Init(bufferSize);
            GetNextULong = BufferedGetNextULong;
        }

        ~BufferedRandomChannel()
        {
            Dispose();
        }

        private void Init(int bufferSize)
        {
            _bufferSize = bufferSize > min_Buffer_Size ? bufferSize : min_Buffer_Size;
            _bufferA = new ulong[bufferSize];
            _bufferB = new ulong[bufferSize];
            _beingFilled = _bufferA;
            for (int i = 0; i < _bufferSize; i++)
                _bufferA[i] = Mtf.genrand_int32();
            _inUse = _bufferA;
            _beingFilled = _bufferB;
            _bufferThread = new Thread(FillBuffer);
            _bufferThread.Start();
            // Must wait for the buffer thread to lock on the lockObject.
            while (!_bufferThread.ThreadState.Equals(ThreadState.WaitSleepJoin))
            {/* spinwait for the buffer thread to lock on the lockObject.*/
            }
            _bufferThread.IsBackground = true; // Allow thread termination.
        }

        private ulong BufferedGetNextULong()
        {
            unchecked
            {
                if (_nextInUseCell == _bufferSize)
                {
                    SwapBuffers();
                }
                return _inUse[_nextInUseCell++];
            }
        }

        private void SwapBuffers()
        {
            lock (_lockObject)
            {
                // The next few lines make sure that we can't come back and swap buffers
                // before the producer thread has had a chance to refill the back buffer.
                while (_nFills != _nExpectedFills)
                {
                    Monitor.Pulse(_lockObject);
                    Monitor.Wait(_lockObject);
                }
                _nExpectedFills = _nFills + 1;
                ulong[] tmp = _inUse;
                _inUse = _beingFilled;
                _beingFilled = tmp;
                _nextInUseCell = 0;
                Monitor.Pulse(_lockObject);
            }
        }


        private void FillBuffer()
        {
            try
            {
                unchecked
                {
                    lock (_lockObject)
                    {
                        while (true)
                        {
                            for (int i = 0; i < _bufferSize; i++)
                                _beingFilled[i] = Mtf.genrand_int32();
                            _nFills++; // Mark this iteration complete.
                            Monitor.Pulse(_lockObject); // In case SwapBuffers is waiting.
                            Monitor.Wait(_lockObject);
                        }
                    }
                }
            }
            catch (ThreadInterruptedException e)
            {
            }
        }

        #region IDisposable Members
        public override void Dispose()
        {
            if (_bufferThread != null)
            {
                _bufferThread.Interrupt();
                _bufferThread.Join();
            }
        }

        #endregion
    }
}