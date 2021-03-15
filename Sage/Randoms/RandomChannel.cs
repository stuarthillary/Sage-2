/* This source code licensed under the GNU Affero General Public License */
// ReSharper disable ClassNeverInstantiated.Global

namespace Highpoint.Sage.Randoms
{
    internal class RandomChannel : IRandomChannel
    {

        protected ULongGetter GetNextULong;
        protected readonly MersenneTwisterFast Mtf;

        public RandomChannel(ulong seed)
        {
            Mtf = new MersenneTwisterFast();
            Mtf.Initialize(seed);
            GetNextULong = Mtf.genrand_int32;
        }

        public RandomChannel(ulong[] initArray)
        {
            Mtf = new MersenneTwisterFast();
            Mtf.Initialize(initArray);
            GetNextULong = Mtf.genrand_int32;
        }

        ~RandomChannel()
        {
            Dispose();
        }

        #region IRandomChannel Members

        public int Next()
        {
            int retval = (int)GetNextULong();
            return retval;
        }

        public int Next(int maxValue)
        {
            return ((int)GetNextULong()) % maxValue;
        }

        public int Next(int minValue, int maxValue)
        {
            return (int)(minValue + ((uint)GetNextULong()) % (maxValue - minValue));
        }

        /// <summary>
        /// [0,1)
        /// </summary>
        /// <returns></returns>
        public double NextDouble()
        {
            return GetNextULong() * (1.0 / 4294967296.0);
            /* divided by 2^32 */
        }

        public double NextDouble(double min, double max)
        {
            //System.Diagnostics.Debug.Assert(min >= 0.0 && max <= 1.0 && min <= max);
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (min == max)
                return min;

            double x = GetNextULong() * (1.0 / 4294967296.0);
            x *= (max - min);
            return min + x;
        }


        public void NextBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return;
            int byteNum = bytes.Length;
            while (true)
            {
                unchecked
                {
                    ulong number = GetNextULong();
                    bytes[byteNum] = (byte)(number & 0xFF);
                    number >>= 8;
                    if (byteNum-- == 0)
                        break;
                    bytes[byteNum] = (byte)(number & 0xFF);
                    number >>= 8;
                    if (byteNum-- == 0)
                        break;
                    bytes[byteNum] = (byte)(number & 0xFF);
                    number >>= 8;
                    if (byteNum-- == 0)
                        break;
                    bytes[byteNum] = (byte)(number & 0xFF);
                    //number >> 8;
                    if (byteNum-- == 0)
                        break;
                }
            }
        }

        #endregion

        public virtual void Dispose()
        {
        }

    }
}