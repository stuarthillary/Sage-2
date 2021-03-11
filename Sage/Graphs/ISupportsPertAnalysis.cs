/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.Graphs.Analysis
{
    /// <summary>
    /// This interface is implemented by any edge in a graph where the edge has
    /// duration, and therefore can be used as a part of the computations necessary
    /// to performing a PERT analysis.
    /// NOTE: WORKS-IN-PROGRESS
    /// </summary>
    public interface ISupportsPertAnalysis : ISupportsCpmAnalysis
    {
        /// <summary>
        /// Optimistic duration is the minimum amount of time that executing the specific task has taken across all runs of the model since the last call to ResetDurationData();
        /// </summary>
        /// <returns>The optimistic duration for this task.</returns>
        TimeSpan GetOptimisticDuration();
        /// <summary>
        /// Pessimistic duration is the maximum amount of time that executing the specific task has taken across all runs of the model since the last call to ResetDurationData();
        /// </summary>
        /// <returns>The pessimistic duration for this task.</returns>
        TimeSpan GetPessimisticDuration();
    }
}
