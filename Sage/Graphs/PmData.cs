/* This source code licensed under the GNU Affero General Public License */

using System.Collections;

namespace Highpoint.Sage.Graphs
{
#if DEBUG
    /// <summary>
    /// PMData is Post-Mortem data, data that indicates which vertices and edges
    /// have fired in a particular graph execution. It does not exist in a non-
    /// debug build.
    /// </summary>
    public class PmData
    {
        /// <summary>
        /// A list of the vertices in a graph that were fired in a given run of  the graph.
        /// </summary>
        public ArrayList VerticesFired = new ArrayList();
        /// <summary>
        /// A list of the edges in a graph that were fired in a given run of  the graph.
        /// </summary>
        public ArrayList EdgesFired = new ArrayList();
    }
#endif
}
