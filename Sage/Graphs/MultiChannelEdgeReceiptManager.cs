/* This source code licensed under the GNU Affero General Public License */
using System.Collections;

namespace Highpoint.Sage.Graphs
{

    public class MultiChannelEdgeReceiptManager : IEdgeReceiptManager
    {
        private readonly Vertex _vertex;
        private readonly PreEdgesSatisfiedKey _preEdgesSatisfiedKey = new PreEdgesSatisfiedKey();
        public MultiChannelEdgeReceiptManager(Vertex vertex)
        {
            _vertex = vertex;
        }

        #region IEdgeReceiptManager Members

        public void OnPreEdgeSatisfied(IDictionary graphContext, Edge edge)
        {
            IList preEdges = _vertex.PredecessorEdges;

            if (preEdges.Count < 2)
            { // If there's only one pre-edge, it must be okay to fire the vertex.
                _vertex.FireVertex(graphContext);
            }
            else
            {
                object channelMarker = null;
                Hashtable channelHandlers = (Hashtable)graphContext[_preEdgesSatisfiedKey];
                if (channelHandlers == null)
                {
                    channelHandlers = new Hashtable();
                    graphContext[_preEdgesSatisfiedKey] = channelHandlers;
                    foreach (Edge _edge in preEdges)
                    {
                        channelMarker = _edge.Channel;
                        if (!channelHandlers.Contains(channelMarker))
                        {
                            channelHandlers.Add(channelMarker, new ChannelMonitor(_vertex, channelMarker));
                        }
                    }
                }

                channelMarker = edge.Channel;
                ChannelMonitor channelMonitor = (ChannelMonitor)channelHandlers[channelMarker];

                if (channelMonitor.RegisterSatisfiedEdge(graphContext, edge))
                {
                    graphContext.Remove(_preEdgesSatisfiedKey);
                    _vertex.FireVertex(graphContext);
                }
            }
        }

        #endregion
    }
}
