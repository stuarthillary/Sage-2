/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;

namespace Highpoint.Sage.Graphs
{
    /// <summary>
    /// A Ligature is an edge that connects nodes, but unlike a task, has no duration. It is used only to model dependencies
    /// such as when two predecessor tasks must complete before a successor task is allowed to commence - the finish vertices
    /// of the two predecessor tasks would be connected to the start vertex of the successor task.
    /// </summary>
    /// <seealso cref="Highpoint.Sage.Graphs.Edge" />
    public class Ligature : Edge {

		/// <summary>
		/// Default constructor for persistence only.
		/// </summary>
		public Ligature(){}

        /// <summary>
        /// Initializes a new instance of the <see cref="Ligature"/> class.
        /// </summary>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        public Ligature(string name){
			_name = (name==null?ToString():name);
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="Ligature"/> class.
        /// </summary>
        /// <param name="from">The vertex from which this ligature starts.</param>
        /// <param name="to">The vertex at which this ligature ends.</param>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        public Ligature(Vertex from, Vertex to, string name):this(name){
			Pre = from;
			Post = to;
			from.AddPostEdge(this);
			to.AddPreEdge(this);
//			from.StructureChangeHandler +=new StructureChangeHandler(from_StructureChangeHandler);
//			to.StructureChangeHandler += new StructureChangeHandler(to_StructureChangeHandler);
		}

        /// <summary>
        /// Called by the pre-vertex when it has been satisfied - that is, all incoming edges and
        /// synchronizers to that vertex have fired.
        /// </summary>
        /// <param name="graphContext">The graph context.</param>
        public override void PreVertexSatisfied(IDictionary graphContext){
			PostVertex.PreEdgeSatisfied(graphContext, this);
		}

        /// <summary>
        /// Initializes the structural change handlers -  GainedPredecessorEvent, GainedSuccessorEvent, LostPredecessorEvent, and LostSuccessorEvent.
        /// </summary>
        protected override void InitializeStructuralChangeHandlers() {
		
		}


        /// <summary>
        /// Disconnects this instance from any parent edges, predecessors and successors. Child edges are left
        /// attached.
        /// </summary>
        public override void Disconnect(){
			Post.RemovePreEdge(this);
			Pre.RemovePostEdge(this);
//			m_pre.StructureChangeHandler -=new StructureChangeHandler(from_StructureChangeHandler);
//			m_post.StructureChangeHandler -= new StructureChangeHandler(to_StructureChangeHandler);
			Post = null;  // Set post to null first, so that invalidity will not
			// propagate downstream to a task that is immediately 
			// thereafter, disconnected.
			Pre = null;

		}

        /// <summary>
        /// Creates a new object that is a copy of the current instance. This is not supported for ligatures.
        /// </summary>
        /// <returns>A new object that is a copy of this instance. This method calls _PopulateClone.</returns>
        /// <exception cref="ApplicationException">Application attempted to clone a ligature.</exception>
        public override object Clone(){
			throw new InvalidOperationException("Application attempted to clone a ligature.");
		}

        /// <summary>
        /// Creates the pre and post vertices for this edge, providing them with default names
        /// and connecting them to this edge.
        /// </summary>
        protected override void CreateVertices(){}

        /// <summary>
        /// Creates a name for this ligature based on the names of the from and to edges.
        /// </summary>
        /// <param name="from">The edge from which this ligature starts.</param>
        /// <param name="to">The edge at which this ligature ends.</param>
        /// <returns>System.String.</returns>
        public static string CreateName(Edge from, Edge to){
			return CreateName(from.PostVertex,to.PreVertex);
		}

        /// <summary>
        /// Creates a name for this ligature based on the names of the from and to vertices.
        /// </summary>
        /// <param name="from">The vertex from which this ligature starts.</param>
        /// <param name="to">The vertex at which this ligature ends.</param>
        /// <returns>System.String.</returns>
        public static string CreateName(Vertex from, Vertex to){
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.Append("(");
			sb.Append(from.Name);
			sb.Append("->");
			sb.Append(to.Name);
			sb.Append(")");
			return sb.ToString();
		}
	}
}
