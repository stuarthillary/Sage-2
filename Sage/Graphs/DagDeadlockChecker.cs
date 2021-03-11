/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Highpoint.Sage.Graphs
{

    /// <summary>
    /// A DAGDeadlockChecker walks a Directed Acyclic Graph, depth-first, looking for deadlocks, which it detects
    /// through the repeated encountering of a given vertex along a given path. After evaluating the DAG,
    /// it presents a collection of errors (the Errors field) in the DAG. The errors are instances of
    /// DAGStructureError, which implements IModelError, and describes either the first, or all deadlocks in
    /// the network of edges underneath the root edge.
    /// </summary>
    public class DagDeadlockChecker
    {

        #region Private Fields
        private static readonly bool _diagnostics = Diagnostics.DiagnosticAids.Diagnostics("DAGDeadlockChecker");
        private IEdge _rootEdge;
        private Hashtable _nodes;
        protected ArrayList _errors;
        private ArrayList _frontier;
        #endregion

        /// <summary>
        /// Creates a DAGDeadlockChecker that can evaluate the DAG under the specified edge.
        /// </summary>
        /// <param name="rootEdge">The edge that defines the DAG to be analyzed - the DAG that is to 
        /// be analyzed runs from the preVertex of the specified edge to its postVertex.
        /// </param>
        public DagDeadlockChecker(IEdge rootEdge)
        {
            _rootEdge = rootEdge;
        }

        /// <summary>
        /// Forces this DAGDeadlockChecker to validate the entire DAG by checking for deadlocks.
        /// </summary>
        /// <returns>True if the DAGDeadlockChecker found no errors.</returns>
        public virtual bool Check()
        {
            _errors = new ArrayList();
            _nodes = new Hashtable();
            _frontier = new ArrayList();

            Build(_rootEdge.PreVertex);

            _frontier.Add(_nodes[_rootEdge.PreVertex]);

            while (Advance())
            {
                if (_diagnostics)
                    Console.WriteLine("There are " + _frontier.Count + " nodes on the frontier.");
            }

            if (_frontier.Count > 0)
            {
                ArrayList removees = new ArrayList();
                foreach (Node n in _frontier)
                {
                    foreach (Node pred in AnyPredecessorsOf(n))
                    {
                        if (_frontier.Contains(pred))
                        {
                            if (!removees.Contains(n))
                            {
                                //Console.WriteLine("Removing " + n.Name + " because " + pred.Name + " is also on the frontier.");
                                removees.Add(n);
                                break;
                            }
                        }
                    }
                }
                foreach (Node r in removees)
                {
                    _frontier.Remove(r);
                }

                ArrayList targets = new ArrayList();
                foreach (Node n in _frontier)
                    targets.Add(n.Element);
                DagStructureError dse = new DagStructureError(_rootEdge, targets, "A deadlock was detected in the graph.");
                _errors.Add(dse);
            }

            return _errors.Count == 0;
        }

        private IEnumerable<Node> AnyPredecessorsOf(Node n)
        {
            return AnyPredecessorsOf(n, new List<Node>());
        }

        private IEnumerable<Node> AnyPredecessorsOf(Node n, List<Node> beenThere)
        {
            foreach (Node pred in n.Predecessors)
            {
                if (beenThere.Contains(pred) || !pred.Visited)
                {
                    break;
                }
                beenThere.Add(pred);
                yield return pred;
                if (pred.Visited)
                {
                    IEnumerator<Node> ienumerator = AnyPredecessorsOf(pred, beenThere).GetEnumerator();
                    while (ienumerator.MoveNext())
                    {
                        yield return ienumerator.Current;
                    }
                }
            }
        }

        private bool Advance()
        {
            bool success = false;
            if (_frontier.Count > 0)
            {
                ArrayList tmpFrontier = new ArrayList();
                Node node;
                for (int i = _frontier.Count - 1; i >= 0; i--)
                {
                    node = (Node)_frontier[i];
                    if (_diagnostics)
                        Console.Write("Evaluating " + node.Name);
                    if (PrecursorsAreSatisfied(node))
                    {
                        success = true;
                        node.Visited = true;
                        _frontier.RemoveAt(i);
                        foreach (Node s in node.Successors)
                        {
                            if (!tmpFrontier.Contains(s) && !_frontier.Contains(s))
                            {
                                tmpFrontier.Add(s);
                            }
                        }
                        if (_diagnostics)
                            Console.WriteLine(" - moving forward.");
                    }
                    else
                    {
                        if (_diagnostics)
                            Console.WriteLine(" - not ready to move forward, yet.");
                    }
                }
                _frontier.AddRange(tmpFrontier);
            }
            return success;
        }

        private bool PrecursorsAreSatisfied(Node node)
        {
            foreach (Node n in node.Predecessors)
                if (!n.Visited)
                    return false;
            return true;
        }


        private void Build(object element)
        {
            _Build(element);

            //			Console.WriteLine("Before establishing predecessors, we have the following:");
            //			foreach ( Node n in m_nodes.Values ) Console.WriteLine(n.ToString());
            EstablishPredecessors();

            //			Console.WriteLine("And after establishing predecessors, we have the following:");
            //			foreach ( Node n in m_nodes.Values ) Console.WriteLine(n.ToString());
        }

        private Node _Build(object element)
        {
            Node node = (Node)_nodes[element];
            if (node == null)
            {
                node = new Node(element);
                _nodes.Add(element, node);
                object[] successors = (object[])GetSuccessors(element).ToArray(typeof(object));
                List<Node> naSuccessors = new List<Node>();
                for (int i = 0; i < successors.Length; i++)
                {
                    Node s = _Build(successors[i]);
                    if (!naSuccessors.Contains(s))
                    {
                        naSuccessors.Add(s);
                    }
                }
                node.Successors = naSuccessors.ToArray();
            }
            return node;
        }

        private void EstablishPredecessors()
        {
            Hashtable ht = new Hashtable();
            //Console.WriteLine("There are " + m_nodes.Count + " nodes in the nodelist.");
            foreach (Node n in _nodes.Values)
            {
                foreach (Node s in n.Successors)
                {
                    //Console.WriteLine("\r\n" + n.Name + " has successor " + s.Name);
                    ArrayList preds = (ArrayList)ht[s];
                    if (preds == null)
                    {
                        //Console.WriteLine("\tCreated a new record for " + s.Name);
                        preds = new ArrayList();
                        ht.Add(s, preds);
                    }
                    else
                    {
                        //Console.WriteLine("\tFound an existing record for " + s.Name);
                    }
                    if (!preds.Contains(n))
                        preds.Add(n);
                    //Console.WriteLine("\tAdding " + n.Name + " as predecessor #" + preds.Count + " to " + n.Name);
                }
            }

            foreach (DictionaryEntry de in ht)
            {
                ((Node)de.Key).Predecessors = (Node[])((ArrayList)de.Value).ToArray(typeof(Node));
            }
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
                {
                    successors.AddRange(vertex.SuccessorEdges);
                }
                if (vertex.Synchronizer != null)
                {
                    int i = 0;
                    while (i < vertex.Synchronizer.Members.Length)
                    {
                        if (vertex.Synchronizer.Members[i++] == vertex)
                            break;
                    }
                    while (i < vertex.Synchronizer.Members.Length)
                    {
                        successors.Add(vertex.Synchronizer.Members[i++]);
                    }
                }
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

        #endregion

        #region Error Management (Present & Clear errors...)
        /// <summary>
        /// A collection of the errors that the DAGDeadlockChecker found in the DAG, during its last check.
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
            private static Node[] _emptyArray = new Node[] { };
            private bool m_onPath;
            private bool m_visited;
            private object m_element;
            private Node[] m_successors;
            private Node[] m_predecessors;

            #endregion

            public Node(object element)
            {
                m_element = element;
                m_successors = _emptyArray;
                m_onPath = false;
            }

            public object Element
            {
                get
                {
                    return m_element;
                }
            }
            public bool OnPath
            {
                get
                {
                    return m_onPath;
                }
                set
                {
                    m_onPath = value;
                }
            }
            public bool Visited
            {
                get
                {
                    return m_visited;
                }
                set
                {
                    m_visited = value;
                }
            }
            public Node[] Successors
            {
                get
                {
                    return (m_successors == null ? _emptyArray : m_successors);
                }
                set
                {
                    m_successors = (value == null ? _emptyArray : value);
                }
            }
            public Node[] Predecessors
            {
                get
                {
                    return (m_predecessors == null ? _emptyArray : m_predecessors);
                }
                set
                {
                    m_predecessors = (value == null ? _emptyArray : value);
                }
            }

            public override bool Equals(object obj)
            {
                return m_element.Equals(((Node)obj).Element);
            }
            public override int GetHashCode()
            {
                return m_element.GetHashCode();
            }

            public override string ToString()
            {
                return ((IHasName)m_element).Name + " : " + Successors.Length + " successors and " + Predecessors.Length + " predecessors.";
            }

            public string Name
            {
                get
                {
                    return ((IHasName)m_element).Name;
                }
            }
        }
    }
}
