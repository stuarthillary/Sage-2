/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Highpoint.Sage.Graphs
{
    /// <summary>
    /// An engine for determining critical paths through a directed acyclic graph (DAG).
    /// </summary>
    /// <typeparam name="T">The type of task being represented in this DAG.</typeparam>
    public class CriticalPathAnalyst<T>
    {

        private readonly T _startNode;
        private readonly T _finishNode;
        private readonly Func<T, DateTime> _startTime;
        private readonly Func<T, TimeSpan> _duration;
        private readonly Func<T, bool> _isFixed;
        private readonly Func<T, IEnumerable<T>> _successors;
        private readonly Func<T, IEnumerable<T>> _predecessors;
        private List<T> _criticalPath;
        private Dictionary<T, TimingData> _timingData;

        /// <summary>
        /// Initializes a new instance of the <see cref="CriticalPathAnalyst{T}"/> class.
        /// </summary>
        /// <param name="startNode">The start node of the directed acyclic graph (DAG).</param>
        /// <param name="finishNode">The finish node of the DAG.</param>
        /// <param name="startTime">A function that, given a task element, returns its start time.</param>
        /// <param name="duration">A function that, given a task element, returns its duration.</param>
        /// <param name="isFixed">A function that, given a task element, returns whether its start time and duration are fixed.</param>
        /// <param name="successors">A function that, given a task element, returns its successors.</param>
        /// <param name="predecessors">A function that, given a task element, returns its predecessors.</param>
        public CriticalPathAnalyst(T startNode, T finishNode, Func<T, DateTime> startTime, Func<T, TimeSpan> duration, Func<T, bool> isFixed, Func<T, IEnumerable<T>> successors, Func<T, IEnumerable<T>> predecessors)
        {
            _startNode = startNode;
            _finishNode = finishNode;
            _startTime = startTime;
            _duration = duration;
            _isFixed = isFixed;
            _successors = successors;
            _predecessors = predecessors;

            _criticalPath = null;
            _timingData = null;
        }

        /// <summary>
        /// Returns the critical path.
        /// </summary>
        /// <value>The critical path.</value>
        public IEnumerable<T> CriticalPath
        {
            get
            {
                if (_criticalPath == null)
                {
                    ComputeCriticalPath();
                }
                return _criticalPath;
            }
        }

        /// <summary>
        /// Computes (or recomputes) the critical path. This is called automatically if necessary when the Critical Path is requested.
        /// </summary>
        public void ComputeCriticalPath()
        {
            _criticalPath = new List<T>();
            _timingData = new Dictionary<T, TimingData>();
            PropagateForward(TimingDataNodeFor(_startNode));

            TimingData tdFinish = TimingDataNodeFor(_finishNode);
            tdFinish.Fix(tdFinish.EarlyStart, tdFinish.NominalDuration, true);
            PropagateBackward(TimingDataNodeFor(_finishNode));

            AnalyzeCriticality();
        }

        private void AnalyzeCriticality()
        {
            // Rough. Starting.
            foreach (TimingData tdNode in _timingData.Values.Where(n => n.IsCritical).OrderBy(n => n.EarlyStart))
            {
                _criticalPath.Add(tdNode.Subject);
            }
        }

        // TODO: Performance improvement if TDNode had its TDNode successors & predecessors retrievable directly.

        /// <summary>
        /// Performs a depth-first propagation along a path for which all predecessors' computations are complete,
        /// adjusting early start &amp; finish according to a PERT methodology.
        /// </summary>
        /// <param name="tdNode">The TimingData node.</param>
        private void PropagateForward(TimingData tdNode)
        {

            tdNode.EarlyFinish = tdNode.EarlyStart + tdNode.NominalDuration;

            foreach (TimingData successor in _successors(tdNode.Subject).Select(n => TimingDataNodeFor(n)))
            {
                if (!successor.IsFixed)
                {
                    successor.EarlyStart = DateTimeOperations.Max(successor.EarlyStart, tdNode.EarlyFinish);
                }
                successor.RegisterPredecessor();
                if (successor.AllPredecessorsHaveWeighedIn)
                {
                    PropagateForward(successor);
                }
            }
        }

        /// <summary>
        /// Performs a depth-first propagation backwards along a path for which all successors' computations
        /// are complete, adjusting late start &amp; finish according to a PERT methodology.
        /// </summary>
        /// <param name="tdNode">The TimingData node.</param>
        private void PropagateBackward(TimingData tdNode)
        {

            tdNode.LateStart = tdNode.LateFinish - tdNode.NominalDuration;

            foreach (TimingData predecessor in _predecessors(tdNode.Subject).Select(n => TimingDataNodeFor(n)))
            {
                if (!predecessor.IsFixed)
                {
                    predecessor.LateFinish = DateTimeOperations.Min(predecessor.LateFinish, tdNode.LateStart);
                }
                predecessor.RegisterSuccessor();
                if (predecessor.AllSuccessorsHaveWeighedIn)
                {
                    PropagateBackward(predecessor);
                }
            }
        }

        /// <summary>
        /// Gets (or creates) the timing data node for the provided client-domain node.
        /// </summary>
        /// <param name="node">The client-domain node.</param>
        /// <returns></returns>
        private TimingData TimingDataNodeFor(T node)
        {
            TimingData tdNode;
            if (!_timingData.TryGetValue(node, out tdNode))
            {
                tdNode = new TimingData(
                    node,
                    _isFixed(node),
                    _startTime(node),
                    _duration(node),
                    (short)_predecessors(node).Count(),
                    (short)_successors(node).Count());
                _timingData.Add(node, tdNode);
            }
            return tdNode;
        }

        private class TimingData : ICriticalPathTimingData
        {

            public TimingData(T subject, bool isFixed, DateTime nominalStart, TimeSpan nominalDuration, short nPreds, short nSuccs)
            {
                Subject = subject;
                if (isFixed)
                {
                    Fix(nominalStart, nominalDuration, true);
                }
                else
                {
                    NominalStart = nominalStart;
                    NominalDuration = nominalDuration;
                    EarlyStart = EarlyFinish = DateTime.MinValue; // Explicit for clarity.
                    LateStart = LateFinish = DateTime.MaxValue;
                }
                _totalNumOfPredecessors = nPreds;
                _totalNumOfSuccessors = nSuccs;
                _totalNumOfPredecessorsWeighedIn = 0;
                _totalNumOfSuccessorsWeighedIn = 0;
            }
            #region ITimingData Members

            public DateTime EarlyStart
            {
                get; set;
            }

            public DateTime LateStart
            {
                get; set;
            }

            public DateTime EarlyFinish
            {
                get; set;
            }

            public DateTime LateFinish
            {
                get; set;
            }

            public double Criticality
            {
                get; set;
            }

            public bool IsCritical
            {
                get
                {
                    return EarlyStart.Equals(LateStart) && EarlyFinish.Equals(LateFinish);
                }
            }
            #endregion

            private bool m_fixed;
            public bool IsFixed
            {
                get
                {
                    return m_fixed;
                }
            }
            public void Fix(DateTime startTime, TimeSpan duration, bool setAsNominal)
            {
                EarlyStart = LateStart = startTime;
                EarlyFinish = LateFinish = startTime + duration;
                m_fixed = true;
                if (setAsNominal)
                {
                    NominalStart = startTime;
                    NominalDuration = duration;
                }
            }

            public DateTime NominalStart
            {
                get; set;
            }

            public TimeSpan NominalDuration
            {
                get; set;
            }

            public T Subject
            {
                get; set;
            }

            private short _totalNumOfPredecessors;
            private short _totalNumOfPredecessorsWeighedIn;
            internal void RegisterPredecessor()
            {
                _totalNumOfPredecessorsWeighedIn++;
            }

            internal bool AllPredecessorsHaveWeighedIn
            {
                get
                {
                    return _totalNumOfPredecessorsWeighedIn == _totalNumOfPredecessors;
                }
            }
            private short _totalNumOfSuccessors;
            private short _totalNumOfSuccessorsWeighedIn;
            internal void RegisterSuccessor()
            {
                _totalNumOfSuccessorsWeighedIn++;
            }

            internal bool AllSuccessorsHaveWeighedIn
            {
                get
                {
                    return _totalNumOfSuccessorsWeighedIn == _totalNumOfSuccessors;
                }
            }
        }
    }


}
