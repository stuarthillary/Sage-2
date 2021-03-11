/* This source code licensed under the GNU Affero General Public License */

using System.Collections;
using Highpoint.Sage.SimCore; // For IExecutive and IDetachableEventController, used in Joining & Yielding.
using Highpoint.Sage.Persistence;

namespace Highpoint.Sage.Graphs
{
    /// <summary>
    /// Implemented by an object that is a participant in a directed graph. Edges may be hierarchical, meaning that an edge
    /// may have child edges that are executed as a part of its own execution.
    /// </summary>
    public interface IEdge : SimCore.ICloneable, IVisitable, IXmlPersistable, IPartOfGraphStructure, IHasName {
        /// <summary>
        /// Gets the pre vertex of the object.
        /// </summary>
        /// <value>The pre vertex.</value>
		Vertex PreVertex { get; }
        /// <summary>
        /// Gets the post vertex of the object.
        /// </summary>
        /// <value>The post vertex.</value>
		Vertex PostVertex { get; }
        /// <summary>
        /// Gets the parent edge to this one. If the graph is not hierarchical, this will be null.
        /// </summary>
        /// <returns></returns>
		IEdge GetParent();
        /// <summary>
        /// Gets the child edges of this one. No sequence is implied in this collection - child edges are executed
        /// in an order according to their vertices' relationships to each other and their parents.
        /// </summary>
        /// <value>The child edges.</value>
		IList ChildEdges { get; }
        /// <summary>
        /// Gets or sets the channel with which this edge is associated. This identifies an edge as a part of a group
        /// of edges that are to be fired together by a <see cref="T:IEdgeFiringManager"/> when a preVertex is satisfied.
        /// As an example, a vertex that had two outbound edges, a forward and a loopback, would have an <see cref="T:IEdgeFiringManager"/>
        /// attached to it that knew that after a call to its Start(...) method, it was to fire the edge associated with
        /// its loopback channel a certain number of times, followed by firing the edges associated with its forward edge
        /// once the loopback count had been reached.
        /// </summary>
        /// <value>The channel.</value>
		object Channel { get; set; }
	}
}
