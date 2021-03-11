/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;

namespace Highpoint.Sage.Graphs.Analysis
{

    /// <summary>
    /// This class is able, in its instances, to perform a PERT analysis, including
    /// determination of critical paths, and their tasks' mean and variances.
    /// NOTE: WORKS-IN-PROGRESS
    /// </summary>
    public class PertAnalyst : CpmAnalyst
    {

        double _criticalPathMean;
        double _criticalPathVariance;
        ArrayList _criticalPath;

        //public PERTAnalyst(Vertex start, Vertex finish):base(start,finish){}
        public PertAnalyst(Edge edge) : base(edge) { }

        public override void Analyze()
        {
            base.Analyze();
            _criticalPath = new ArrayList();
            DetermineMeanAndVarianceOfCriticalPath();
        }

        public void DetermineMeanAndVarianceOfCriticalPath()
        {
            Vertex here = Start;
            double mean = 0.0;
            double vari = 0.0;

            while (here != null)
            {
                here = NextVertexInCriticalPath(here, ref mean, ref vari);
                if (here.Equals(Finish))
                {
                    _criticalPathMean = mean;
                    _criticalPathVariance = vari;
                    return;
                }
            }
            throw new ApplicationException("There is no path from the start to the finish vertices." +
                " This should NOT happen, as CPM Analysis has already taken place, and FOUND such a path.");

        }

        protected Vertex NextVertexInCriticalPath(Vertex vertex, ref double mean, ref double variance)
        {
            Edge targetEdge = null;
            foreach (Edge edge in vertex.SuccessorEdges)
            {
                targetEdge = edge;
                if (targetEdge is Ligature)
                    targetEdge = edge.PostVertex.PrincipalEdge;
                EdgeData ed = (EdgeData)Edges[targetEdge];
                if (ed == null)
                    continue;
                if (IsCriticalPath(targetEdge))
                {
                    _criticalPath.Add(targetEdge);
                    mean += ed.MeanDuration;
                    variance += ed.Variance2;
                    return targetEdge.PostVertex;
                }
            }

            if (targetEdge != null && targetEdge.Equals(Finish.PrincipalEdge))
                return Finish;

            throw new ApplicationException("The vertex " + vertex.Name + " is not on the critical path.");
        }

        public ArrayList CriticalPath => ArrayList.ReadOnly(_criticalPath);
        public TimeSpan CriticalPathMean => TimeSpan.FromTicks((long)_criticalPathMean);

        public TimeSpan CriticalPathVariance
        {
            get
            {
                long ticks = (long)Math.Sqrt(_criticalPathVariance);
                return TimeSpan.FromTicks(ticks);
            }
        }
    }
}
