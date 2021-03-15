/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Randoms {
    /// <summary>
    /// This is a port of Takuji Nishimura and Makoto Matsumoto's famous
    /// Mersenne Twister Pseudorandom number generator. It was ported to C#
    /// by Peter Bosch for Highpoint Software Systems, LLC's Sage�
    /// product. See the following, but be aware that the RandomServer
    /// architecture is independent of the PRNG being used, and is the property
    /// of, and copyrighted by, Highpoint Software Systems, LLC.
    /// <p></p>
    /// 	Copyright (C) 1997 - 2002, Makoto Matsumoto and Takuji Nishimura,
    /// 	All rights reserved.                          
    /// <p></p>
    /// 	Redistribution and use in source and binary forms, with or without
    /// 	modification, are permitted provided that the following conditions
    /// 	are met:
    /// <p></p>
    /// 	1. Redistributions of source code must retain the above copyright
    /// 	notice, this list of conditions and the following disclaimer.
    /// <p></p>
    /// 	2. Redistributions in binary form must reproduce the above copyright
    /// 	notice, this list of conditions and the following disclaimer in the
    /// 	documentation and/or other materials provided with the distribution.
    /// <p></p>
    /// 	3. The names of its contributors may not be used to endorse or promote 
    /// 	products derived from this software without specific prior written 
    /// 	permission.
    /// <p></p>
    /// 	THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
    /// 	"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
    /// 	LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
    /// 	A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT OWNER OR
    /// 	CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
    /// 	EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
    /// 	PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
    /// 	PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
    /// 	LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
    /// 	NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
    /// 	SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
    /// <p></p>
    /// <p></p>
    /// 	Any feedback is very welcome.<p></p>
    /// 	http://www.math.keio.ac.jp/matumoto/emt.html<p></p>
    /// 	email: matumoto@math.keio.ac.jp<p></p>
    ///     Updated as of 20160915 : 
    ///     http://www.math.sci.hiroshima-u.ac.jp/~m-mat/MT/emt.html<p></p>
    ///     email: m-mat@math.sci.hiroshima-u.ac.jp<p></p>
    /// 
    /// </summary>
    public class MersenneTwisterFast {
		/* 
		 * Translated to C# by Peter Bosch, 12/15/2003.

		A C-program for MT19937, with initialization improved 2002/1/26.
		   Coded by Takuji Nishimura and Makoto Matsumoto.

		   Before using, initialize the state by using init_genrand(seed)  
		   or init_by_array(init_key, key_length).

		   Copyright (C) 1997 - 2002, Makoto Matsumoto and Takuji Nishimura,
		   All rights reserved.                          

		   Redistribution and use in source and binary forms, with or without
		   modification, are permitted provided that the following conditions
		   are met:

			 1. Redistributions of source code must retain the above copyright
				notice, this list of conditions and the following disclaimer.

			 2. Redistributions in binary form must reproduce the above copyright
				notice, this list of conditions and the following disclaimer in the
				documentation and/or other materials provided with the distribution.

			 3. The names of its contributors may not be used to endorse or promote 
				products derived from this software without specific prior written 
				permission.

		   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
		   "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
		   LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
		   A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT OWNER OR
		   CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
		   EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
		   PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
		   PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
		   LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
		   NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
		   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.


		   Any feedback is very welcome.
		   http://www.math.keio.ac.jp/matumoto/emt.html
		   email: matumoto@math.keio.ac.jp
		*/

		/* Period parameters */  
		private const ulong n = 624;
		private const ulong m = 397;
		private const ulong matrix_A = 0x9908b0dfUL;   /* constant vector a */
		private const ulong upper_Mask = 0x80000000UL; /* most significant w-r bits */
		private const ulong lower_Mask = 0x7fffffffUL; /* least significant r bits */
		private static readonly ulong[] mag01 = new ulong[]{0x0UL, matrix_A};
        private ulong[] mt = new ulong[n]; /* the array for the state vector  */
        private ulong _mti=n + 1; /* mti==N+1 means mt[N] is not initialized */

        /// <summary>
        /// Initializes this Mersenne Twister with the specified seed.
        /// </summary>
        /// <param name="s">The s.</param>
		public void Initialize(ulong s) {
				mt[0]= s & 0xffffffffUL;
			for (_mti=1; _mti<n; _mti++) {
				mt[_mti] = 
					(1812433253UL * (mt[_mti-1] ^ (mt[_mti-1] >> 30)) + _mti); 
				/* See Knuth TAOCP Vol2. 3rd Ed. P.106 for multiplier. */
				/* In the previous versions, MSBs of the seed affect   */
				/* only MSBs of the array mt[].                        */
				/* 2002/01/09 modified by Makoto Matsumoto             */
				mt[_mti] &= 0xffffffffUL;
				/* for >32 bit machines */
			}
		}

        /// <summary>
        /// Initializes the Mersenne Twister with the specified init_key.
        /// </summary>
        /// <param name="initKey">The initialization key.</param>
		public void Initialize(ulong[] initKey) {
			ulong keyLength = (ulong)initKey.Length;
            Initialize(19650218UL);
			ulong i = 1; ulong j = 0;
			ulong k = (n>keyLength ? n : keyLength);
			for (; (k!=0); k--) {
				mt[i] = (mt[i] ^ ((mt[i-1] ^ (mt[i-1] >> 30)) * 1664525UL))
					+ initKey[j] + j; /* non linear */
				mt[i] &= 0xffffffffUL; /* for WORDSIZE > 32 machines */
				i++; j++;
				if (i>=n) { mt[0] = mt[n-1]; i=1; }
				if (j>=keyLength) j=0;
			}
			for (k=n-1; (k!=0); k--) {
				mt[i] = (mt[i] ^ ((mt[i-1] ^ (mt[i-1] >> 30)) * 1566083941UL))
					- i; /* non linear */
				mt[i] &= 0xffffffffUL; /* for WORDSIZE > 32 machines */
				i++;
				if (i>=n) { mt[0] = mt[n-1]; i=1; }
			}

			mt[0] = 0x80000000UL; /* MSB is 1; assuring non-zero initial array */ 
		}

        /// <summary>
        /// generates a random number on the [0,0xffffffff] interval
        /// </summary>
        /// <returns>A random number on the [0,0xffffffff] interval.</returns>
		public ulong genrand_int32(){
			ulong y;
			unchecked {
				/* mag01[x] = x * MATRIX_A  for x=0,1 */

				if (_mti >= n) { /* generate N words at one time */
					ulong kk;

					if (_mti == n+1)   /* if init_genrand() has not been called, */
						Initialize(5489UL); /* a default initial seed is used */

					for (kk=0;kk<n-m;kk++) {
						y = (mt[kk]&upper_Mask)|(mt[kk+1]&lower_Mask);
						mt[kk] = mt[kk+m] ^ (y >> 1) ^ mag01[y & 0x1UL];
					}
					for (;kk<n-1;kk++) {
						y = (mt[kk]&upper_Mask)|(mt[kk+1]&lower_Mask);
						mt[kk] = mt[kk+(m-n)] ^ (y >> 1) ^ mag01[y & 0x1UL];
					}
					y = (mt[n-1]&upper_Mask)|(mt[0]&lower_Mask);
					mt[n-1] = mt[m-1] ^ (y >> 1) ^ mag01[y & 0x1UL];

					_mti = 0;
				}


                y = mt[_mti++];


				/* Tempering */
				y ^= (y >> 11);
				y ^= (y << 7) & 0x9d2c5680UL;
				y ^= (y << 15) & 0xefc60000UL;
				y ^= (y >> 18);
			}
			return y;
		}

        /// <summary>
        /// Generates a random number on the [0,0x7fffffff] interval.
        /// </summary>
        /// <returns></returns>
		public ulong genrand_int31() {
			return genrand_int32()>>1;
		}

        /// <summary>
        /// Generates a random number on the [0,1] real interval.
        /// </summary>
        /// <returns>A random number on the [0,1] real interval.</returns>
		public double genrand_real1() {
			return genrand_int32()*(1.0/4294967295.0); 
			/* divided by 2^32-1 */ 
		}

        /// <summary>
        /// Generates a random number on the [0,1) real interval.
        /// </summary>
        /// <returns>A random number on the [0,1) real interval.</returns>
        public double genrand_real2() {
			return genrand_int32()*(1.0/4294967296.0); 
			/* divided by 2^32 */
		}

        /// <summary>
        /// Generates a random number on the (0,1) real interval.
        /// </summary>
        /// <returns>A random number on the (0,1) real interval.</returns>
        public double genrand_real3() {
			return (genrand_int32() + 0.5) * (1.0/4294967296.0); 
			/* divided by 2^32 */
		}

        /// <summary>
        /// Generates a random number on [0,1) with 53-bit resolution.
        /// </summary>
        /// <returns></returns>
		public double genrand_res53() { 
			ulong a=genrand_int32()>>5, b=genrand_int32()>>6; 
			return(a*67108864.0+b)*(1.0/9007199254740992.0); 
		} 
		/* These real versions are due to Isaku Wada, 2002/01/09 added */
	}
}
