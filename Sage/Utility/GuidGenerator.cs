/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.Utility
{

    /// <summary>
    /// Generates a pseudorandom stream of Guids. Make sure that the maskGuid and
    /// seedGuids are 'sufficiently chaotic'. This generator is best used for testing.
    /// It is modeled after a linear feedback shift register. http://en.wikipedia.org/wiki/LFSR
    /// </summary>
    public class GuidGenerator
    {
        private readonly Guid _seedGuid;
        private readonly Guid _maskGuid;
        private readonly int _rotateBits;
        private Guid _current;
        private bool _passThrough;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:GuidGenerator"/> class.
        /// </summary>
        /// <param name="seedGuid">The seed GUID - the starting register value.</param>
        /// <param name="maskGuid">The mask GUID - the polynomial.</param>
        /// <param name="rotateBits">The number of bits to rotate the register by.</param>
        public GuidGenerator(Guid seedGuid, Guid maskGuid, int rotateBits)
        {
            _rotateBits = rotateBits;
            _seedGuid = seedGuid;
            _maskGuid = maskGuid;
            Reset();
        }

        /// <summary>
        /// Gets the next guid from this Guid Generator.
        /// </summary>
        /// <returns></returns>
        public Guid Next()
        {
            if (!_passThrough)
            {
                _current = GuidOps.Increment(_current);
                _current = GuidOps.XOR(_maskGuid, _current);
                _current = GuidOps.Rotate(_current, _rotateBits);
                return _current;
            }
            else
            {
                return Guid.NewGuid();
            }
        }

        /// <summary>
        /// Resets this Guid Generator to its initial settings.
        /// </summary>
        public void Reset()
        {
            _current = _seedGuid;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:GuidGenerator"/> is passthrough, meaning
        /// that if it is passthrough, it will simply generate a new Guid from Guid.NewGuid(); with every call
        /// to Next();
        /// </summary>
        /// <value><c>true</c> if passthrough; otherwise, <c>false</c>.</value>
        public bool Passthrough
        {
            set
            {
                _passThrough = value;
            }
            get
            {
                return _passThrough;
            }
        }
    }
}
