/* This source code licensed under the GNU Affero General Public License */
using System;
// ReSharper disable ClassNeverInstantiated.Global

namespace Highpoint.Sage.Randoms
{
    /// <summary>
    /// Class GlobalRandomServer is a singleton RandomServer that exists and can be obtained from anywhere in a process space. See 
    /// RandomServer for details.
    /// </summary>
    public class GlobalRandomServer
    {
        #region Private Fields
        private static readonly object @lock = new object();
        private static volatile RandomServer _instance;
        private static ulong _seed = (ulong)DateTime.Now.Ticks;
        private static int _bufferSize;
        private static int _globalRandomChannelBufferSize;
        private static IRandomChannel _globalRandomChannel;
        private static ulong _globalRandomChannelSeed;
        #endregion

        /// <summary>
        /// Sets the random seed for the global random server. This is a super-seed 
        /// which is used to seed any channels not otherwise explicitly seeded that 
        /// are obtained from the Global Random Server.
        /// </summary>
        /// <param name="seed">The seed.</param>
        /// <exception cref="ApplicationException">Calls to GlobalRandomServer.SetSeed(long newSeed) must be performed before any call to GlobalRandomServer.Instance.</exception>
        public static void SetSeed(ulong seed)
        {
            if (_instance == null)
            {
                _seed = seed;
            }
            else
            {
                throw new ApplicationException("Calls to GlobalRandomServer.SetSeed(long newSeed) must be performed before any call to GlobalRandomServer.Instance.");
            }
        }

        /// <summary>
        /// Sets the size of the buffer for each of the double-buffer sides. 
        /// Generation is done into one buffer on a worker thread while 
        /// service is taken from the other.
        /// </summary>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <exception cref="ApplicationException">Calls to GlobalRandomServer.SetBufferSize(int bufferSize) must be performed before any call to GlobalRandomServer.Instance.</exception>
        public static void SetBufferSize(int bufferSize)
        {
            if (_instance == null)
            {
                _bufferSize = bufferSize;
            }
            else
            {
                throw new ApplicationException("Calls to GlobalRandomServer.SetBufferSize(int bufferSize) must be performed before any call to GlobalRandomServer.Instance.");
            }
        }

        /// <summary>
        /// Sets the seed for the GlobalRandomChannel. The seed must be set before the first call to use the GlobalRandomChannel.
        /// </summary>
        /// <param name="seed">the GlobalRandomChannel seed</param>
        /// <exception cref="ApplicationException">Calls to GlobalRandomServer.SetBufferSize(int bufferSize) must be performed before any call to GlobalRandomServer.Instance.</exception>
        public static void SetGlobalRandomChannelSeed(ulong seed)
        {
            if (_globalRandomChannel == null)
            {
                _globalRandomChannelSeed = seed;
            }
            else
            {
                throw new ApplicationException("Calls to GlobalRandomServer.SetGlobalRandomChannelSeed(ulong seed) must be performed before any call to GlobalRandomServer.GlobalRandomChannel.");
            }
        }

        /// <summary>
        /// Sets the size of the buffer for each of the double-buffer 
        /// sides of the GlobalRandomChannel. Generation is done into 
        /// one buffer on a worker thread while service is taken from 
        /// the other.
        /// </summary>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <exception cref="ApplicationException">Calls to GlobalRandomServer.SetGlobalRandomChannelBufferSize(int bufferSize) must be performed before any call to GlobalRandomServer.SetGlobalRandomChannelBufferSize.</exception>
        public static void SetGlobalRandomChannelBufferSize(int bufferSize)
        {
            if (_globalRandomChannel == null)
            {
                _globalRandomChannelBufferSize = bufferSize;
            }
            else
            {
                throw new ApplicationException("Calls to GlobalRandomServer.SetGlobalRandomChannelBufferSize(int bufferSize) must be performed before any call to GlobalRandomServer.GlobalRandomChannel.");
            }
        }

        /// <summary>
        /// Gets the global random channel.
        /// </summary>
        /// <value>The global random channel.</value>
        public static IRandomChannel GlobalRandomChannel => _globalRandomChannel ??
                                                            (_globalRandomChannel = Instance.GetRandomChannel(_globalRandomChannelSeed, _globalRandomChannelBufferSize));

        /// <summary>
        /// Gets the singleton instance of the global random server.
        /// </summary>
        /// <value>The instance.</value>
        public static RandomServer Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (@lock)
                    {
                        if (_instance == null)
                            _instance = new RandomServer(_seed, _bufferSize);
                    }
                }
                return _instance;
            }
        }
    }
}