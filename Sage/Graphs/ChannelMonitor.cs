/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;

namespace Highpoint.Sage.Graphs
{
    public class ChannelMonitor
    {
        private readonly IVertex _vertex;
        private readonly object _channelMarker;
        private readonly ArrayList _myEdges;
        private readonly ArrayList _preEdgesSatisfied;

        public ChannelMonitor(Vertex vertex, object channelMarker)
        {
            _vertex = vertex;
            _channelMarker = channelMarker;
            _myEdges = new ArrayList();
            _preEdgesSatisfied = new ArrayList();
            foreach (Edge e in vertex.PredecessorEdges)
            {
                if (channelMarker.Equals(e.Channel))
                    _myEdges.Add(e);
            }
        }

        public bool RegisterSatisfiedEdge(IDictionary graphContext, Edge edge)
        {
            if (!_myEdges.Contains(edge))
                throw new ApplicationException("Unknown edge (" + edge + ") signaled completion to " + this);

            if (_preEdgesSatisfied.Contains(edge))
                throw new ApplicationException("Edge (" + edge + ") signaled completion twice, to " + this);
            
            _preEdgesSatisfied.Add(edge);

            return (_preEdgesSatisfied.Count == _myEdges.Count);
        }
    }
}
