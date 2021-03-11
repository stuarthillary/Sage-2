/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.SimCore;
using System;
using System.Collections;
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Global

namespace Highpoint.Sage.Graphs
{
    /// <summary>
    /// The CountedBranchManager fires one channel a specified number of times, and then fires
    /// another channel a specified number of times, etc. It then repeats as necessary. This is useful
    /// in looping &amp; branching - the edge firing manager will fire the loopback edge a number of
    /// times followed by the shunt or pass-forward edge.
    /// </summary>
    public class CountedBranchManager : IEdgeFiringManager
    {

        #region Private Fields
        private static readonly ExecEventReceiver _launchEdge = LaunchEdge;
        private readonly object[] _channels;
        private readonly int[] _counts;
        private static VolatileKey _cbmDataKey;
        private readonly IModel _model;
        #endregion

        /// <summary>
        /// Creates a counted branch manager that will fire all outbound edges with channels matching the
        /// zeroth channel, a number of times, followed by those matching the first channel another number
        /// of times, etc. The channels array and the counts array must have the same number of elements, and
        /// they are considered paired arrays - that is, the zeroth element of one goes with the zeroth element
        /// of the other, likewise the first, second, etc.
        /// </summary>
        /// <param name="model">The model in which this graph is running. This is necessary because the outbound
        /// edges are fired asynchronously to keep a graph's execution path from looping back over this branch
        /// manager while it is still executing.</param>
        /// <param name="channels">An array of channel objects that determine which outbound edges will fire.
        /// <B>IMPORTANT NOTE: Edges with null channel markers must be specified by the Edge.NullChannelMarker object.</B></param>
        /// <param name="counts">An array of integers that will determine how many times the given edges will fire.</param>
        public CountedBranchManager(IModel model, object[] channels, int[] counts)
        {
            _channels = channels;
            _counts = counts;
            _cbmDataKey = new VolatileKey();
            _model = model;
        }

        /// <summary>
        /// This is fired once at the beginning of this branch manager's being asked to review a set of edges,
        /// which happens immediately after a vertex is satisfied. After that, FireIfAppropriate(...) is called
        /// once for each outbound edge.
        /// </summary>
        /// <param name="graphContext">The graph context in which we are currently running.</param>
        public void Start(IDictionary graphContext)
        {
            CbmData data = (CbmData)graphContext[_cbmDataKey];
            if (data == null)
            {
                data = new CbmData();
                graphContext.Add(_cbmDataKey, data);
            }
            data.CurrentPriority = _model.Executive.CurrentPriorityLevel;
            data.Now = _model.Executive.Now;
            if (data.Remaining == 0)
                AdvanceChannel(data);
            data.Remaining--;
            //Console.WriteLine("CountedBranchManager.Start: Active channel " + m_channels[data.ActiveChannel].ToString() + ", " + data.Remaining + " iterations.");
        }

        /// <summary>
        /// Schedules the presented edge to be fired if the edge's channel matches the currently active channel.
        /// </summary>
        /// <param name="graphContext">The graph context in which we are currently running.</param>
        /// <param name="edge">The edge being considered for execution.</param>
        public void FireIfAppropriate(IDictionary graphContext, Edge edge)
        {
            //System.Diagnostics.Debugger.Break();
            //Console.Write("Reviewing edge " + edge.Name + " for firing. Its channel marker is  " + edge.Channel.ToString());
            CbmData data = (CbmData)graphContext[_cbmDataKey];
            // If data is null, here, it is probably because the vertex did not call Start before firing branch edges.

            if (_channels[data.ActiveChannel].Equals(edge.Channel))
            {
                //Console.WriteLine(" Scheduling it to fire.");
                _model.Executive.RequestEvent(_launchEdge, data.Now, data.CurrentPriority, new EdgeLaunchData(edge, graphContext));
            }
            else
            {
                //Console.WriteLine(" Nope, not this time.");
                // We're not going to fire it this time around.
            }
        }

        private void AdvanceChannel(CbmData data)
        {
            data.ActiveChannel++;
            if (data.ActiveChannel == _channels.Length)
                data.ActiveChannel = 0;
            data.Remaining = _counts[data.ActiveChannel];
        }

        /// <summary>
        /// The model through which edge executions are scheduled.
        /// </summary>
        public IModel Model => _model;

        private static void LaunchEdge(IExecutive exec, object userData)
        {
            EdgeLaunchData eld = (EdgeLaunchData)userData;
            eld.Edge.PreVertexSatisfied(eld.GraphContext);
        }

        #region Support Data Structures (All private)
        private struct EdgeLaunchData
        {
            public readonly Edge Edge;
            public readonly IDictionary GraphContext;
            public EdgeLaunchData(Edge edge, IDictionary graphContext)
            {
                Edge = edge;
                GraphContext = graphContext;
            }
        }

        private class CbmData
        {
            public int ActiveChannel = -1;
            public double CurrentPriority;
            public DateTime Now;
            public int Remaining;
        }
        #endregion


        #region IEdgeFiringManager Members


        public void ClearBranches()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}
