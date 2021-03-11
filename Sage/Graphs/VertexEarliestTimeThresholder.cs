/* This source code licensed under the GNU Affero General Public License */


using Highpoint.Sage.SimCore; // For executive.
using System;
using System.Collections;

namespace Highpoint.Sage.Graphs
{

    /// <summary>
    /// When attached to a vertex in a graph, this object ensures that the vertex does not fire before a specified simulation time.
    /// </summary>
    public class VertexEarliestTimeThresholder
    {

        #region Private Fields

        private readonly TriggerDelegate _vertexTrigger;
        private readonly IModel _model;
        private DateTime _earliest;
        private readonly Vertex _vertex;

        #endregion 

        /// <summary>
        /// Creates a new instance of the <see cref="T:VertexEarliestTimeThresholder"/> class.
        /// </summary>
        /// <param name="model">The model in which the <see cref="T:VertexEarliestTimeThresholder"/> exists.</param>
        /// <param name="vertex">The vertex that this object will control.</param>
		public VertexEarliestTimeThresholder(IModel model, Vertex vertex)
            : this(model, vertex, DateTime.MinValue) { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:VertexEarliestTimeThresholder"/> class.
        /// </summary>
        /// <param name="model">The model in which the <see cref="T:VertexEarliestTimeThresholder"/> exists.</param>
        /// <param name="vertex">The vertex that this object will control.</param>
        /// <param name="earliest">The earliest time that the vertex should be allowed to fire.</param>
		public VertexEarliestTimeThresholder(IModel model, Vertex vertex, DateTime earliest)
        {
            _earliest = earliest;
            _model = model;
            _vertex = vertex;
            _vertexTrigger = vertex.FireVertex;
            vertex.FireVertex = new TriggerDelegate(FireTheVertex);
        }

        /// <summary>
        /// Gets or sets the earliest time that the vertex should be allowed to fire.
        /// </summary>
        /// <value>The earliest time that the vertex should be allowed to fire.</value>
		public DateTime Earliest
        {
            get
            {
                return _earliest;
            }
            set
            {
                _earliest = value;
            }
        }

        private void FireTheVertex(IDictionary graphContext)
        {
            if (_model.Executive.Now < _earliest)
            {
                TimeSpan ts = (_earliest - _model.Executive.Now);
                // _Debug.WriteLine(m_model.Executive.Now + " : " + "Will fire vertex " + m_vertex.Name + " after a delay of " + string.Format("{0:d2}:{1:d2}:{2:d2}",ts.Hours,ts.Minutes,ts.Seconds));
                ExecEventType eet = _model.Executive.CurrentEventType;
                _model.Executive.RequestEvent(new ExecEventReceiver(_FireTheVertex), _earliest, 0, graphContext, eet);
            }
            else
            {
                // _Debug.WriteLine(m_model.Executive.Now + " : " + "Firing vertex " + m_vertex.Name);
                _vertexTrigger(graphContext);
            }
        }

        private void _FireTheVertex(IExecutive exec, object graphContext)
        {
            FireTheVertex((IDictionary)graphContext);
        }

    }
}