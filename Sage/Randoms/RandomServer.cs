/* This source code licensed under the GNU Affero General Public License */
using System;
// ReSharper disable ClassNeverInstantiated.Global

namespace Highpoint.Sage.Randoms
{
    internal delegate ulong ULongGetter();

    /// <summary>
    /// The random server serves channels from which fixed sequences of pseudo-random
    /// numbers can be drawn. Each channel is serviced by a Mersenne Twister Random
    /// Number Generator. The server can be created with or without a default buffer
    /// size, and thereafter, each channel can be created with or without a specified
    /// buffer size. If a channel's buffer size is not specified, it utilizes a buffer
    /// of the default size.
    /// <p></p>
    /// <b>Buffering Scheme:</b><p></p>
    /// Each channel, if it has a non-zero buffer size, utilizes a double buffering
    /// scheme where the PRNG is filling the back buffer in a producer thread while
    /// the model is using the front buffer in its consumer thread. Due to the Windows
    /// threading model, this results in better PRNG performance even when the consumer
    /// thread is processor-bound, at least over the longer-haul (more than a few tens
    /// of consumptions.) It will really shine when there is more than one processor
    ///  in the system. You can play around with buffer size, and the effect probably
    ///  varies somewhat with RNG consumption rate, but you might start with a number
    ///  somewhere around 100. This will be the subject of study, some day...<p></p>
    ///  <b>Note: </b>If using buffering, the model/consumer must call Dispose() on
    ///  the Random Channel once it is finished with that Random Channel.
    /// <p></p>If there is a zero buffer size specified, the consumer always goes
    /// straight to the PRNG to get its next value. This option may be slightly faster
    /// in cases where the machine is running threads that are higher than user priority,
    /// and usually starving the system, but in our tests, it ran at about half the speed.
    /// In this case, there is no explicit need to call Dispose() on the Random Channel.<p></p>
    /// <b>Coming Enhancements:</b><p></p>
    /// Two, mainly. First, using a single thread per RandomServer, rather than per RandomChannel.
    /// And second, making it so that you don't have to call Dispose() any more.
    /// </summary>
    public class RandomServer
    {

        #region Private Fields
        private readonly MersenneTwisterFast _seedGenerator;
        private readonly int _defaultBufferSize;
        #endregion

        /// <summary>
        /// Creates a RandomServer with a specified hyperSeed and default buffer size.
        /// </summary>
        /// <param name="hyperSeed">This is the seed that will initialize the PRNG that
        /// provides seeds for RandomChannels that do not have a specified seed. This is
        /// a way of having an entire model's sequence be repeatable without having to
        /// hard code all of the RC's seed values.</param>
        /// <param name="defaultBufferSize">The buffer size that will be applied to channels
        /// that do not have an explicit buffer size specified. This provides a good way
        /// to switch the entire model's buffering scheme on or off at one location.</param>
        // ReSharper disable once UnusedParameter.Local
        public RandomServer(ulong hyperSeed, int defaultBufferSize = 0)
        {
            _defaultBufferSize = 0;// defaultBufferSize;
            _seedGenerator = new MersenneTwisterFast();
            _seedGenerator.Initialize(hyperSeed);
        }

        /// <summary>
        /// Creates a RandomServer with a zero buffer size (and therefore single-threaded
        /// RandomChannels), and a hyperSeed that is based on the time of day.
        /// </summary>
        public RandomServer() : this((ulong)DateTime.Now.Ticks) { }

        /// <summary>
        /// Gets a RandomChannel with a specified seed and buffer size.
        /// </summary>
        /// <param name="seed">The seed value for the PRNG behind this channel.</param>
        /// <param name="bufferSize">The buffer size for this channel. Non-zero enables double-buffering.</param>
        /// <returns>The random channel from which random numbers may be obtained in a repeatable sequence.</returns>
        public IRandomChannel GetRandomChannel(ulong seed, int bufferSize)
        {
            if (bufferSize == 0)
            {
                return new RandomChannel(seed);
            }
            else
            {
                return new BufferedRandomChannel(seed, bufferSize);
            }
        }

        /// <summary>
        /// Gets a RandomChannel with a specified seed and buffer size.
        /// </summary>
        /// <param name="initArray">An array of unsigned longs that will be used to initialize the PRNG behind this channel.</param>
        /// <param name="bufferSize">The buffer size for this channel. Non-zero enables double-buffering.</param>
        /// <returns>The random channel from which random numbers may be obtained in a repeatable sequence.</returns>
        // ReSharper disable once UnusedParameter.Global
        public IRandomChannel GetRandomChannel(ulong[] initArray, int bufferSize = 0)
        {
            if (bufferSize == 0)
            {
                return new RandomChannel(initArray);
            }
            else
            {
                return new BufferedRandomChannel(initArray, bufferSize);
            }
        }

        /// <summary>
        /// Gets a RandomChannel with a seed and buffer size provided by the RandomServer.
        /// </summary>
        public IRandomChannel GetRandomChannel()
        {
            return GetRandomChannel(_seedGenerator.genrand_int32(), _defaultBufferSize);
        }
    }
}