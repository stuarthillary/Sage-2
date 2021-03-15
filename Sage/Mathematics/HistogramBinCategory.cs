/* This source code licensed under the GNU Affero General Public License */

using System;
// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// A flag enumerator that specifies which bins in a histogram the caller is referring to.
    /// </summary>
    [Flags]
    public enum HistogramBinCategory : byte
    {
        /// <summary>
        /// No bins are to be, or were, included in the operation.
        /// </summary>
        None = 0,
        /// <summary>
        /// Only the off-scale-low bin is to be, or was, included in the operation.
        /// </summary>
        OffScaleLow = 0x01,
        /// <summary>
        /// All in-range bins are to be, or were, included in the operation.
        /// </summary>
        InRange = 0x02,
        /// <summary>
        /// Only the off-scale-high bin is to be, or was, included in the operation.
        /// </summary>
        OffScaleHigh = 0x04,
        /// <summary>
        /// All bins are to be, or were, included in the operation.
        /// </summary>
        All = 0x07
    }
}



