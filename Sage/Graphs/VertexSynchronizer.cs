/* This source code licensed under the GNU Affero General Public License */


using Highpoint.Sage.Persistence;
using Highpoint.Sage.SimCore; // For executive.
using System;
using System.Collections;

namespace Highpoint.Sage.Graphs
{
    /// a synchronization primitive between two or more vertices, where the vertices
    /// are, once all are able to fire, fired in the order specified in the 'vertices'
    /// array.
    public class VertexSynchronizer : IXmlPersistable
    {

        public static void Synchronize(IExecutive exec, params Vertex[] vertices)
        {
            ArrayList verticesToSynchronize = new ArrayList();
            foreach (Vertex vtx in vertices)
            {
                if (vtx.Synchronizer != null)
                {
                    foreach (Vertex vtx2 in vtx.Synchronizer.Members)
                    {
                        if (!verticesToSynchronize.Contains(vtx2))
                            verticesToSynchronize.Add(vtx2);
                    }
                    vtx.Synchronizer.Destroy();
                }
                else
                {
                    if (!verticesToSynchronize.Contains(vtx))
                        verticesToSynchronize.Add(vtx);
                }
            }

            Vertex[] allVertices = (Vertex[])verticesToSynchronize.ToArray(typeof(Vertex));
            VertexSynchronizer vs = new VertexSynchronizer(exec, allVertices, ExecEventType.Detachable);

        }

        private Vertex[] _vertices; // This one is contained in the synchronizer
        private IExecutive _exec;
        private ExecEventType _eventType;

        /// <summary>
        /// Creates a synchronization between two or more vertices, where the vertices
        /// are, once all are able to fire, fired in the order specified in the 'vertices'
        /// array.
        /// </summary>
        /// <param name="exec">The executive in whose simulation this VS is currently running.</param>
        /// <param name="vertices">An array of vertices to be synchronized.</param>
        /// <param name="vertexFiringType">The type of ExecEvent that successor edges to this vertex
        /// should be called with.</param>
        public VertexSynchronizer(IExecutive exec, Vertex[] vertices, ExecEventType vertexFiringType)
        {

            _vertices = vertices;
            _exec = exec;
            _eventType = vertexFiringType;
            //Trace.Write("CREATING SYNCHRONIZER WITH TASKS ( " );

            foreach (Vertex vertex in _vertices)
            {
                if (vertex.PrincipalEdge is Tasks.Task)
                    ((Tasks.Task)vertex.PrincipalEdge).SelfValidState = false;
            }

            foreach (Vertex vertex in _vertices)
            {
                //Trace.Write(vertex.Name + ", ");
                if (vertex.Role.Equals(Vertex.WhichVertex.Post))
                {
                    throw new ApplicationException("Cannot synchronize postVertices at this time.");
                }
                else
                {
                    vertex.SetSynchronizer(this);
                }
            }
            //_Debug.WriteLine(" )");
        }

        /// <summary>
        /// Removes this synchronizer from all vertices that it was synchronizing.
        /// </summary>
        public void Destroy()
        {
            foreach (Vertex vertex in _vertices)
            {
                vertex.SetSynchronizer(null);
            }
            _vertices = new Vertex[] { };
        }

        internal void NotifySatisfied(Vertex vertex, IDictionary graphContext)
        {

            #region // HACK: This leaves orphaned Synchronizers hanging off the graph.
            ArrayList newVertices = new ArrayList();
            foreach (Vertex _vertex in _vertices)
            {
                if (_vertex.PrincipalEdge.GetParent() != null)
                {
                    newVertices.Add(_vertex);
                }
            }
            _vertices = (Vertex[])newVertices.ToArray(typeof(Vertex));
            #endregion

            ArrayList satisfiedVertices = (ArrayList)graphContext[_vsKey];
            if (satisfiedVertices == null)
            {
                satisfiedVertices = new ArrayList(_vertices.Length);
                graphContext.Add(_vsKey, satisfiedVertices);
            }

            foreach (Vertex _vertex in _vertices)
            {
                if (vertex.Equals(_vertex) && !satisfiedVertices.Contains(vertex))
                {
                    satisfiedVertices.Add(vertex);
                }
            }

            if (satisfiedVertices.Count == _vertices.Length)
            {
                foreach (Vertex _vertex in _vertices)
                {
                    // We need to fire these both at the same time and priorities, but to
                    // queue them up as separate simultaneous events. This is because a vertex
                    // that relies on activity on a second, simultaneous vertex's handler, will
                    // need to yield to that vertex, which means that vertex will need to be on
                    // a separate thread - hence we fire them as separate events.
                    _exec.RequestEvent(new ExecEventReceiver(_vertex._AsyncFireVertexHandler), _exec.Now, _exec.CurrentPriorityLevel, graphContext, _eventType);
                    //_vertex._FireVertex(graphContext);
                }
                graphContext.Remove(_vsKey);
            }
        }

        /// <summary>
        /// A sequenced array of the member vertices of this synchronizer.
        /// </summary>
        public Vertex[] Members { get { return _vertices; } }

        private readonly object _vsKey = new VolatileKey();

        #region IXmlPersistable Members
        public VertexSynchronizer() { }
        public virtual void SerializeTo(XmlSerializationContext xmlsc)
        {
            xmlsc.StoreObject("EventType", _eventType);
            xmlsc.StoreObject("VertexCount", _vertices.Length);
            for (int i = 0; i < _vertices.Length; i++)
            {
                xmlsc.StoreObject("Vertex_" + i, _vertices[i]);
            }
            // Skipping m_vsKey & m_executive.
        }

        public virtual void DeserializeFrom(XmlSerializationContext xmlsc)
        {
            // TODO:  Add Vertex.DeserializeFrom implementation
            _exec = ((Model)xmlsc.ContextEntities["Model"]).Executive;
            _eventType = (ExecEventType)xmlsc.LoadObject("EventType");
            int vertexCount = (int)xmlsc.LoadObject("VertexCount");
            _vertices = new Vertex[vertexCount];
            //for ( int i = 0 ; i < vertexCount ; i++ ) {
            throw new NotImplementedException("Vertex deserialization not yet implemented in VertexSynchronizers.");
            //}
        }

        #endregion
    }

}
