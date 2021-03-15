/* This source code licensed under the GNU Affero General Public License */
using System;
// ReSharper disable ClassNeverInstantiated.Global

namespace Highpoint.Sage.Randoms
{
    /// <summary>
    /// Implemented by an object that can serve random numbers, similarly to the Math.Random() PRNG.
    /// </summary>
    public interface IRandomChannel : IDisposable
    {

        /// <summary>
        /// Produces the next pseudo random integer. Ranges from int.MinValue to int.MaxValue.
        /// </summary>
        /// <returns>The next pseudo random integer.</returns>
        int Next();

        /// <summary>
        /// Produces the next pseudo random integer. Ranges from int.MinValue to the argument maxValue.
        /// </summary>
        /// <param name="maxValue">The maximum value served by the PRNG, exclusive.</param>
        /// <returns>The next pseudo random integer in the range [minValue,maxValue).</returns>
        int Next(int maxValue);

        /// <summary>
        /// Produces the next pseudo random integer. Ranges from the argument minValue to the argument maxValue.
        /// </summary>
        /// <param name="minValue">The minimum value served by the PRNG, inclusive.</param>
        /// <param name="maxValue">The maximum value served by the PRNG, exclusive.</param>
        /// <returns>The next pseudo random integer in the range [minValue,maxValue).</returns>
        int Next(int minValue, int maxValue);

        /// <summary>
        /// Returns a random double between 0 (inclusive) and 1 (exclusive).
        /// </summary>
        /// <returns>The next random double in the range [0,1).</returns>
        double NextDouble();

        /// <summary>
        /// Returns a random double on the range [min,max), unless min == max,
        /// in which case it returns min.
        /// </summary>
        /// <returns>The next random double in the range [min,max).</returns>
        double NextDouble(double min, double max);

        /// <summary>
		/// Fills an array with random bytes.
		/// </summary>
		/// <param name="bytes"></param>
		void NextBytes(byte[] bytes);
    }
}