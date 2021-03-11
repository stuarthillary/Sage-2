/* This source code licensed under the GNU Affero General Public License */
using System.Collections;
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Global

namespace Highpoint.Sage.Graphs
{
    /// <summary>
    /// Implemented by an object that is responsible for deciding when to fire one or more of a group of edges.
    /// Edge firing managers are typically associated with a vertex, and when the vertex thinks that an edge
    /// should be fired (say, because all predecessor edges have completed), it will advise the Edge Firing Manager
    /// to fire the appropriate edges.
    /// </summary>
    public interface IEdgeFiringManager
    {

        /// <summary>
        /// This is fired once at the beginning of a branch manager's being asked to review a set of edges,
        /// which happens immediately after a vertex is satisfied.
        /// </summary>
        /// <param name="graphContext">The graph context in which we are currently running.</param>
        void Start(IDictionary graphContext);

        /// <summary>
        /// Schedules the presented edge to be fired if the edge's channel matches the currently active channel.
        /// </summary>
        /// <param name="graphContext">The graph context in which we are currently running.</param>
        /// <param name="edge">The edge being considered for execution.</param>
        void FireIfAppropriate(IDictionary graphContext, Edge edge);

        /// <summary>
        /// Clears the list of branch data, essentially removing all branches from this manager.
        /// </summary>
        void ClearBranches();

    }
}
