/* This source code licensed under the GNU Affero General Public License */
/*###############################################################################
#  Material previously published at http://builder.com/5100-6387_14-5025380.html
#  Highpoint Software Systems is a Wisconsin Limited Liability Corporation.
###############################################################################*/

using System;
using System.Collections;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Dependencies
{
    /// <summary>
    /// Analyzes a collection of vertices that implement IDependencyVertex, producing, if possible, a sequence
    /// in which to process the vertices such that no vertex is processed before all vertices on which it depends have been processed.
    /// </summary>
    public class GraphSequencer : ISequencer
    {

        private readonly ArrayList _vertices = null;
        private IList _serviceSequenceList = null;
        private static readonly bool _diagnostics = Diagnostics.DiagnosticAids.Diagnostics("GraphSequencer");
        private static readonly bool _diagnostics_StackCheck = Diagnostics.DiagnosticAids.Diagnostics("GraphSequencer.StackCheck");

        /// <summary>
        /// Creates a new instance of the <see cref="T:GraphSequencer"/> class.
        /// </summary>
        public GraphSequencer()
        {
            _vertices = new ArrayList();
        }

        /// <summary>
        /// Call this with a collection of IDependencyVertex objects to add them to the
        /// sequencer for consideration next time there is a request for a service 
        /// sequence list.
        /// </summary>
        /// <param name="vertices">A collection of IDependencyVertex objects</param>
        public void AddVertices(ICollection vertices)
        {
            _serviceSequenceList = null;
            foreach (IDependencyVertex idv in vertices)
            {
                _vertices.Add(idv);
            }
        }

        /// <summary>
        /// Call this with an IDependencyVertex object to add it to the sequencer
        /// for consideration next time there is a request for a service sequence list.
        /// </summary>
        /// <param name="vertex">An IDependencyVertex object</param>
        public void AddVertex(IDependencyVertex vertex)
        {
            _serviceSequenceList = null;
            if (_vertices.Contains(vertex))
            {
                string msg = string.Format("Dependency sequencer error: Attempt to register a dependency vertex, {0}, that already exists in the list.",
                    vertex);
                throw new ApplicationException(msg);
            }
            _vertices.Add((IDependencyVertex)vertex);
        }

        /// <summary>
        /// Returns an ordered list of vertices in which the order is the order in which
        /// the vertices should be serviced.
        /// </summary>
        public IList GetServiceSequenceList()
        {
            if (_serviceSequenceList == null)
                RecalculateServiceSequence();
            return _serviceSequenceList;
        }

        /// <summary>
        /// Recalculates the service sequence.
        /// </summary>
        protected void RecalculateServiceSequence()
        {

            _serviceSequenceList = new ArrayList();

            // First we create the list of vertex data and a hashtable to find
            // the vertex from the underlying.
            ArrayList lstVerts = new ArrayList();
            Hashtable htVerts = new Hashtable();

            //_Debug.WriteLine("Calculating Service Sequence.");
            foreach (IDependencyVertex idv in _vertices)
            {
                VertexRecord v = new VertexRecord(idv);

                if (_diagnostics)
                {
                    _Debug.WriteLine("Parents of " + v.Underlying);
                    foreach (IDependencyVertex idv2 in v.Underlying.PredecessorList)
                    {
                        _Debug.WriteLine("\t" + idv2);
                    }
                }

                lstVerts.Add(v);
                htVerts.Add(idv, v);
                if (_diagnostics)
                    _Debug.WriteLine(String.Format("New vertex, {0} with {1} dependents.", idv, idv.PredecessorList.Count));
            }

            // Each underlying knows who it depends on - we need each vertex to
            // know, rather, who depends on IT.
            foreach (VertexRecord v in lstVerts)
            {
                v.EstablishChildRelationships(htVerts);
            }

            try
            {
                bool bResortNeeded = true;
                int numAffectedLastAdjustment = lstVerts.Count;
                // We will repeat the following until all nodes have been evaluated.
                while (lstVerts.Count > 0)
                {
                    //if ( lstVerts.Count%100 == 0 ) Console.WriteLine("%%%" + lstVerts.Count);

                    // Sort remaining vertices by the provided Comparer. (defaults to
                    // sorting first by nOrder, then by the vertices' provided comparator.
                    // See below for an explanation of the 'bResortNeeded' boolean.
                    if (bResortNeeded)
                    {
                        if (numAffectedLastAdjustment > lstVerts.Count * 0.02)
                        {
                            lstVerts.Sort(GetVertexComparer());
                        }
                        else
                        {
                            lstVerts = BubbleSort(lstVerts);
                        }
                    }

                    // Dumps the sort order as it progresses...
                    //foreach ( Vertex v in lstVerts ) _Debug.WriteLine(v.Underlying + " : " + v.Order);

                    // Move the least vertex to the ServiceOrder list.
                    VertexRecord next = (VertexRecord)lstVerts[0];
                    lstVerts.RemoveAt(0);
                    _serviceSequenceList.Add(next.Underlying);

                    // If the vertex we just removed had no parents, then the sort order
                    // that was valid in the preceding step is still valid. Nobody had
                    // their child-count reduced, so no one's position in the sort will
                    // have had a reason to change. We just move on to the next one...
                    numAffectedLastAdjustment = next.Underlying.PredecessorList.Count;
                    bResortNeeded = (numAffectedLastAdjustment > 0);

                    if (next.Order != 0)
                    {
                        IDependencyVertex root = next.Underlying;
                        ArrayList members = new ArrayList();
                        members.Add(root);
                        Stack stack = new Stack();
                        foreach (IDependencyVertex parent in root.PredecessorList)
                        {
                            if (!FindCycle(root, parent, ref members, ref stack))
                                break;
                        }
                        throw new GraphCycleException(members);
                    }

                    // Decrement the Order of all vertices that this one depended on.
                    next.DecrementParentsChildCount(htVerts);

                }
            }
            catch (StackOverflowException soe)
            {
                throw new ApplicationException("The GraphSequencer has detected a probable dependency cycle in the initialization sequence of this model. For details, set the GraphSequencer.StackCheck key to true in the modeler's app.config file.", soe);
            }

            #region Diagnostics

            if (_diagnostics)
            {
                _Debug.WriteLine("Dependency Solver determines sequence to be:");
                foreach (IDependencyVertex idv in _serviceSequenceList)
                {
                    _Debug.WriteLine("\t" + idv);
                }
            }

            #endregion Diagnostics

        }

        private bool FindCycle(IDependencyVertex root, IDependencyVertex next, ref ArrayList members, ref Stack stack)
        {

            if (_diagnostics_StackCheck)
            {
                if (stack.Contains(next))
                {
                    string msg = "GraphSequencer has detected a dependency cycle in the initialization sequence of this model.\r\n";
                    IDependencyVertex thisDv = next;
                    int depct = 1;
                    foreach (IDependencyVertex idv in (object[])stack.ToArray())
                    {
                        msg += string.Format("{0}.) {1} depends on {2}.\r\n", (depct++), thisDv, idv);
                        thisDv = idv;
                    }
                    throw new ApplicationException(msg);
                }
                else
                {
                    stack.Push(next);
                }
            }

            foreach (IDependencyVertex parent in next.PredecessorList)
            {
                if (parent.Equals(root) || FindCycle(root, parent, ref members, ref stack))
                {
                    members.Add(next);
                    return true;
                }
            }
            return false;
        }


        private static readonly IComparer _defaultVc = new DefaultVertexComparer();
        /// <summary>
        /// Gets the vertex comparer.
        /// </summary>
        /// <returns></returns>
        public virtual IComparer GetVertexComparer()
        {
            return _defaultVc;
        }

        private ArrayList BubbleSort(ArrayList list)
        {
            // We know that anything out of sequence is only one "bin" off. So as we move down the array, we track the last place
            // that the count incremented.
            VertexRecord[] va = (VertexRecord[])list.ToArray(typeof(VertexRecord));

            int lastStepUp = -1;
            int lastOrder = -1;
            for (int i = 0; i < va.Length; i++)
            {
                int thisOrder = va[i].Order;
                if (thisOrder > lastOrder)
                    lastStepUp = i;
                if (thisOrder < lastOrder)
                {
                    VertexRecord tmp = va[lastStepUp];
                    va[lastStepUp] = va[i];
                    va[i] = tmp;
                    lastStepUp++;
                }
                lastOrder = va[i].Order;
            }

            return new ArrayList(va);
        }

        public class DefaultVertexComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                VertexRecord v1 = (VertexRecord)x;
                VertexRecord v2 = (VertexRecord)y;

                if (v1.Order < v2.Order)
                    return -1;
                if (v1.Order > v2.Order)
                    return 1;

                // v1.Order equals v2.Order. If these orders are > 0, they won't get 
                // added to the sorted list anyhow, so we'll pretend they're equivalent,
                // and return 0. Otherwise, if the nodes' orders are zero, we'll 
                // consider the secondary sort criteria.
                if (v1.Order > 0)
                    return 0;

                try
                {
                    return v1.Underlying.SortCriteria.CompareTo(v2.Underlying.SortCriteria);
                }
                catch (Exception ex)
                {
                    _Debug.WriteLine(ex.Message);
                    return 0;
                }
            }
        }

        internal class VertexRecord
        {
            private readonly IDependencyVertex _underlying;
            private int _order;

            public VertexRecord(IDependencyVertex underlying)
            {
                _underlying = underlying;
                _order = 0;
            }

            internal void EstablishChildRelationships(Hashtable otherVertices)
            {
                foreach (IDependencyVertex idv in _underlying.PredecessorList)
                {
                    VertexRecord v = (VertexRecord)otherVertices[idv];
                    v.Order++;
                }
            }

            internal void DecrementParentsChildCount(Hashtable otherVertices)
            {
                foreach (IDependencyVertex idv in _underlying.PredecessorList)
                {
                    VertexRecord v = (VertexRecord)otherVertices[idv];
                    v.Order--;
                }
            }

            public int Order
            {
                get
                {
                    return _order;
                }
                set
                {
                    _order = value;
                }
            }

            public IDependencyVertex Underlying
            {
                get
                {
                    return _underlying;
                }
            }

            public override string ToString()
            {
                return "" + _order + " : " + _underlying;
            }
        }
    }

}