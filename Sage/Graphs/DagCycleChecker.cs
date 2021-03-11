/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;

namespace Highpoint.Sage.Graphs
{

    /// <summary>
    /// A DAGCycleChecker walks a Directed Acyclic Graph, depth-first, looking for cycles, which it detects
    /// through the repeated encountering of a given vertex along a given path. After evaluating the DAG,
    /// it presents a collection of errors (the Errors field) in the DAG. The errors are instances of
    /// DAGStructureError, which implements IModelError, and describes either the first, or all cycles in
    /// the network of edges underneath the root edge.
    /// </summary>
    public class DAGCycleChecker
    {

        #region Private Fields
        private static readonly bool _diagnostics = Diagnostics.DiagnosticAids.Diagnostics("DAGCycleChecker");
        private readonly IEdge _rootEdge;
        private readonly ArrayList _errors;
        private bool _haltOnError;
        private readonly Stack _currentPath;
        private int _level = 0;
        private readonly Hashtable _nodes;
        private bool _collapse;
        #endregion

        public DAGCycleChecker(IEdge rootEdge) : this(rootEdge, false) { }

        /// <summary>
        /// Creates a DAGCycleChecker that can evaluate the DAG under the specified edge.
        /// </summary>
        /// <param name="rootEdge">The edge that defines the DAG to be analyzed - the DAG runs from thepreVertex of this edge to the postVertex, and includes all children.</param>
        /// <param name="collapse">If true, the graph is collapsed to make it smaller.</param>
        public DAGCycleChecker(IEdge rootEdge, bool collapse)
        {
            _rootEdge = rootEdge;
            _haltOnError = true;
            _currentPath = new Stack();
            _errors = new ArrayList();
            _nodes = new Hashtable();
            _collapse = collapse;
        }

        /// <summary>
        /// Forces this DAGCycleChecker to validate the entire DAG by checking for cycles.
        /// </summary>
        /// <param name="haltOnError">If true, the checker will stop as soon as it finds the first error.</param>
        /// <param name="startElement">The vertex at which the Cycle Checker begins its search.</param>
        /// <returns>
        /// True if the DAGCycleChecker found no errors.
        /// </returns>
        public virtual bool Check(bool haltOnError, object startElement)
        {
            _haltOnError = haltOnError;
            _currentPath.Clear();
            _errors.Clear();
            _nodes.Clear();

            Build(_rootEdge.PreVertex);
            Node start = (Node)_nodes[startElement ?? _rootEdge];

            Advance(start);

            return (_errors.Count == 0);
        }

        /// <summary>
        /// Forces this DAGCycleChecker to validate the entire DAG by checking for cycles.
        /// </summary>
        /// <param name="haltOnError">If true, the checker will stop as soon as it finds the first error.</param>
        /// <returns>True if the DAGCycleChecker found an error.</returns>
        public virtual bool Check(bool haltOnError)
        {
            return Check(haltOnError, _rootEdge.PreVertex);
        }

        private Node Build(object element)
        {
            Node node = _nodes[element] as Node;
            if (node == null)
            {
                node = new Node(element);
                _nodes.Add(element, node);
                object[] successors = (object[])GetSuccessors(element).ToArray(typeof(object));
                Node[] naSuccessors = new Node[successors.Length];
                for (int i = 0; i < successors.Length; i++)
                {
                    naSuccessors[i] = Build(successors[i]);
                }
                node.Successors = naSuccessors;
            }

            if (_collapse)
            {
                for (int i = 0; i < node.Successors.Length; i++)
                {
                    Collapse(node);
                }
            }
            return node;
        }

        private void Collapse(Node node)
        {
            if (node.Element.Equals(_rootEdge.PostVertex))
                return;
            for (int i = 0; i < node.Successors.Length; i++)
            {
                Node successor = node.Successors[i];
                Collapse(successor);
                if (successor.Successors.Length == 0)
                {
                    node.Successors[i] = null;
                }
                else if (successor.Successors.Length == 1)
                {
                    node.Successors[i] = successor.Successors[i];
                }
            }
        }

        #region Element Handlers
        /// <summary>
        /// Moves the checking cursor forward to the specified vertex. Calls EvaluateVertex(...) to ensure 
        /// that the new vertex has not been encountered along this path yet, and calls GetEdgesFromVertex(...)
        /// to determine the next group of edges to be traversed, following which, it calls Advance(Edge edge)
        /// on each of those edges. After a path has been explored, the Advance method calls Retreat(...) on
        /// the specified vertex, and the cursor retreats to where it was before this path was explored.
        /// </summary>
        /// <param name="node">The Node that is to be added to the current depth path.</param>
        private void Advance(Node node)
        {
            if (_diagnostics)
            {
                for (int i = 0; i < _level; i++)
                    Console.Write("  ");
                Console.WriteLine("Advancing to " + node.Element + ".");
            }

            if (_haltOnError && _errors.Count > 0)
                return;

            if (node.OnPath)
            {
                LogError(node.Element);
            }
            else
            {
                if (!node.Visited)
                {
                    node.Visited = true;
                    node.OnPath = true;
                    _currentPath.Push(node.Element);
                    _level++;
                    foreach (Node successor in node.Successors)
                        Advance(successor);
                    _level--;
                    node.OnPath = false;
                    _currentPath.Pop();
                }
            }
        }

        /// <summary>
        /// Returns an ArrayList of the edges that are downstream from the given vertex, in a depth-first traversal.
        /// </summary>
        /// <param name="element">The element, forward from which we wish to proceed.</param>
        /// <returns>An ArrayList of the elements that are downstream from the given element, in a depth-first traversal.</returns>
        protected virtual ArrayList GetSuccessors(object element)
        {
            ArrayList successors = new ArrayList();
            if (element is Vertex)
            {
                Vertex vertex = (Vertex)element;
                if (vertex.SuccessorEdges != null && !vertex.Equals(_rootEdge.PostVertex))
                    successors.AddRange(vertex.SuccessorEdges);
            }
            else if (element is Edge)
            {
                Edge edge = (Edge)element;
                successors.Add(edge.PostVertex);
            }
            else
            {
                //throw new ApplicationException("Don't know what to do with a " + element.ToString() + ".");
            }
            return successors;
        }

        /// <summary>
        /// Adds an error indicating that this element represents the start of a cycle. Cycles will be detected
        /// by the first recurring element in a path.
        /// </summary>
        /// <param name="element">The element that has just been added to the current depth path.</param>
        private void LogError(object element)
        {
            #region Create & add an error.
            if (_diagnostics)
            {
                for (int i = 0; i < _level; i++)
                    Console.Write("  ");
                Console.WriteLine(">>>>>>>>>>>> Element " + element + " represents the start of a cycle.");
            }

            ArrayList elements = new ArrayList();
            ArrayList pathObjArray = new ArrayList(_currentPath.ToArray());
            string narrative = "Cycle detected - elements are : ";
            int startOfLoop = pathObjArray.IndexOf(element);
            for (int i = startOfLoop; i >= 0; i--)
            {
                object elementInPath = pathObjArray[i];
                elements.Add(elementInPath);
                narrative += elementInPath.ToString();
                if (i > 1)
                {
                    narrative += ", ";
                }
                else if (i == 1)
                {
                    narrative += " and ";
                }
            }
            DagStructureError se = new DagStructureError(_rootEdge, elements, narrative);
            _errors.Add(se);
            #endregion
        }


        #endregion

        #region Error Management (Present & Clear errors...)
        /// <summary>
        /// A collection of the errors that the DAGCycleChecker found in the DAG, during its last check.
        /// </summary>
        public ICollection Errors
        {
            get
            {
                return ArrayList.ReadOnly(_errors);
            }
        }

        /// <summary>
        /// Clears out the collection of errors.
        /// </summary>
        public void ClearErrors()
        {
            _errors.Clear();
        }
        #endregion

        class Node
        {
            #region Private Fields
            private static readonly Node[] _emptyArray = new Node[] { };
            private bool _onPath;
            private bool _visited;
            private object _element;
            private Node[] _successors;
            #endregion

            public Node(object element)
            {
                _element = element;
                _successors = _emptyArray;
                _onPath = false;
            }

            public object Element
            {
                get
                {
                    return _element;
                }
            }
            public bool OnPath
            {
                get
                {
                    return _onPath;
                }
                set
                {
                    _onPath = value;
                }
            }
            public bool Visited
            {
                get
                {
                    return _visited;
                }
                set
                {
                    _visited = value;
                }
            }
            public Node[] Successors
            {
                get
                {
                    return _successors;
                }
                set
                {
                    _successors = (value == null ? _emptyArray : value);
                }
            }

            public override bool Equals(object obj)
            {
                return _element.Equals(((Node)obj)._element);
            }
            public override int GetHashCode()
            {
                return _element.GetHashCode();
            }
        }
    }
}
