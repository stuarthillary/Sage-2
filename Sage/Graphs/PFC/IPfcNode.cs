/* This source code licensed under the GNU Affero General Public License */

using System.Collections.Generic;

namespace Highpoint.Sage.Graphs.PFC
{
    /// <summary>
    /// Implemented by anything that can be a node in an SFC graph. This includes steps and transitions.
    /// Nodes are connected to Links as their predecessor &amp; successors.
    /// Links such as ParallelConvergentLinks and SeriesDivergentLinks have multiple predecessors or
    /// successors, and their logic to fire or not is dependent upon input steps' or transitions' states.
    /// </summary>
    public interface IPfcNode : IPfcElement
    {

        /// <summary>
        /// Gets or sets a value indicating whether the structure of this SFC is dirty (in effect, whether it has changed since
        /// consolidation was last done.
        /// </summary>
        /// <value><c>true</c> if [structure dirty]; otherwise, <c>false</c>.</value>
        bool StructureDirty
        {
            get; set;
        }

        /// <summary>
        /// Gets the predecessor list for this node.
        /// </summary>
        /// <value>A list of the predecessor links.</value>
        PfcLinkElementList Predecessors
        {
            get;
        }

        /// <summary>
        /// Gets the predecessor nodes list for this node. The list contains all nodes at the other end of links that are
        /// predecessors of this node.
        /// </summary>
        /// <value>A list of the predecessor nodes.</value>
        PfcNodeList PredecessorNodes
        {
            get;
        }

        /// <summary>
        /// Adds the new predecessor link to this node's list of predecessors.
        /// </summary>
        /// <param name="newPredecessor">The new predecessor link.</param>
        void AddPredecessor(IPfcLinkElement newPredecessor);

        /// <summary>
        /// Removes the predecessor link from this node's list of predecessors.
        /// </summary>
        /// <param name="currentPredecessor">The current predecessor.</param>
        /// <returns></returns>
        bool RemovePredecessor(IPfcLinkElement currentPredecessor);

        /// <summary>
        /// Gets the successor list for this node.
        /// </summary>
        /// <value>A list of the successor links.</value>
        PfcLinkElementList Successors
        {
            get;
        }

        /// <summary>
        /// Gets the successor nodes list for this node. The list contains all nodes at the other end of links that are
        /// successors of this node.
        /// </summary>
        /// <value>A list of the successor nodes.</value>
        PfcNodeList SuccessorNodes
        {
            get;
        }

        /// <summary>
        /// Adds the new successor link to this node's list of successors.
        /// </summary>
        /// <param name="newSuccessor">The new successor link.</param>
        void AddSuccessor(IPfcLinkElement newSuccessor);

        /// <summary>
        /// Removes the successor link from this node's list of successors.
        /// </summary>
        /// <param name="currentSuccessor">The current successor.</param>
        /// <returns></returns>
        bool RemoveSuccessor(IPfcLinkElement currentSuccessor);


        /// <summary>
        /// Gets the link that connects this node to a successor node. Returns null if there is no such link.
        /// </summary>
        /// <param name="successorNode">The successor node.</param>
        /// <returns></returns>
        IPfcLinkElement GetLinkForSuccessorNode(IPfcNode successorNode);

        /// <summary>
        /// Gets the link that connects this node to a predecessor node. Returns null if there is no such link.
        /// </summary>
        /// <param name="predecessorNode">The predecessor node.</param>
        /// <returns></returns>
        IPfcLinkElement GetLinkForPredecessorNode(IPfcNode predecessorNode);

        /// <summary>
        /// Gives the specified link (which must be one of the outbound links from this node) the highest 
        /// priority of all links outbound from this node. Retuens false if the specified link is not a 
        /// successor link to this node. NOTE: This API will renumber the outbound links' priorities.
        /// </summary>
        /// <param name="outbound">The link, already in existence and an outbound link from this node, that 
        /// is to be set to the highest priority of all links already outbound from this node.</param>
        /// <returns></returns>
        bool SetLinkHighestPriority(IPfcLinkElement outbound);

        /// <summary>
        /// Gives the specified link (which must be one of the outbound links from this node) the lowest 
        /// priority of all links outbound from this node. Retuens false if the specified link is not a 
        /// successor link to this node. NOTE: This API will renumber the outbound links' priorities.
        /// </summary>
        /// <param name="outbound">The link, already in existence and an outbound link from this node, that 
        /// is to be set to the lowest priority of all links already outbound from this node.</param>
        /// <returns></returns>
        bool SetLinkLowestPriority(IPfcLinkElement outbound);

        /// <summary>
        /// Gets or sets the graph ordinal of this node - a number that roughly (but consistently)
        /// represents its place in the execution order for this graph. Loopbacks' ordinals indicate
        /// their place in the execution order as of their first execution.
        /// </summary>
        /// <value>The graph ordinal.</value>
        int GraphOrdinal
        {
            get; set;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is simple. A node is simple if it 
        /// has one input and one output and performs no tasks beyond a pass-through.
        /// </summary>
        /// <value><c>true</c> if this instance is simple; otherwise, <c>false</c>.</value>
        bool IsSimple
        {
            get;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is null. A node that is null can be
        /// eliminated when PFCs are combined.
        /// </summary>
        /// <value><c>true</c> if this instance is null; otherwise, <c>false</c>.</value>
        bool IsNullNode
        {
            get; set;
        }

        /// <summary>
        /// Used by a variety of graph analysis algorithms.
        /// </summary>
        NodeColor NodeColor
        {
            get; set;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a start node.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is a start node; otherwise, <c>false</c>.
        /// </value>
        bool IsStartNode
        {
            get;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a finish node.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is a finish node; otherwise, <c>false</c>.
        /// </value>
        bool IsFinishNode
        {
            get;
        }

        /// <summary>
        /// A string dictionary containing name/value pairs that represent graphics &amp; layout-related values.
        /// </summary>
        Dictionary<string, string> GraphicsData
        {
            get;
        }

    }
}
