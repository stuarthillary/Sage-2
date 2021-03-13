/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Graphs.PFC
{
    /// <summary>
    /// A PfcLink is a part of one of these types of aggregate links, depending on the type of its predecessor
    /// or successor, and the number of (a) successors its predecessor has, and (b) predecessors its successor has.
    /// </summary>
    public enum AggregateLinkType
    {
        Unknown,
        Simple,
        ParallelConvergent,
        SeriesConvergent,
        ParallelDivergent,
        SeriesDivergent
    }
}
