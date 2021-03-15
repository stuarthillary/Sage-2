/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.Scheduling
{
    public interface ITimePeriodBase
    {
        /// <summary>
        /// Reads and writes the Start Time. Modification of other parameters is according to
        /// the AdjustmentMode (which defaults to FixedDuration.)
        /// </summary>
        DateTime StartTime
        {
            get; set;
        }

        /// <summary>
        /// Reads and writes the End Time.
        /// </summary>
        DateTime EndTime
        {
            get; set;
        }

        /// <summary>
        /// Reads and writes the Duration.
        /// </summary>
        TimeSpan Duration
        {
            get; set;
        }
    }
}
