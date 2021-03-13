/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.Graphs.PFC.Execution
{
    public class ExecutionEngineConfiguration
    {

        public static readonly TimeSpan DEFAULT_SCANNING_PERIOD = TimeSpan.FromMinutes(1.0);
        public static readonly bool DEFAULT_STRUCTURE_LOCKING = true;
        public static readonly int DEFAULT_ = 1;

        public ExecutionEngineConfiguration()
        {
        }

        public ExecutionEngineConfiguration(TimeSpan scanningPeriod)
        {
            ScanningPeriod = scanningPeriod;
        }

        public TimeSpan ScanningPeriod
        {
            get;
            set;
        } = DEFAULT_SCANNING_PERIOD;
        public bool StructureLockedDuringRun
        {
            get;
            set;
        } = DEFAULT_STRUCTURE_LOCKING;
    }
}
