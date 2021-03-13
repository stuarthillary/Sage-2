/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Graphs.PFC
{
    public abstract class PfcNode : PfcElement, IPfcNode
    {
        #region Private Fields

        private readonly PfcLinkElementList _predecessors;
        private readonly PfcLinkElementList _successors;

        private bool _isResetting = false;
        private readonly bool _isSimple = true;
        private bool _isNull = true;
        private bool _structureDirty = true;
        private int _graphOrdinal = 0;
        internal object ScratchPad = null;
        private Dictionary<string, string> _graphicsData = null;
        private DateTime? _earliestStart = null;

        #endregion Private Fields

        #region Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="T:PfcNode"/> class.
        /// </summary>
        public PfcNode() : this(null, null, null, Guid.NewGuid()) { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:PfcNode"/> class.
        /// </summary>
        /// <param name="parent">The PFC this step runs as a part of.</param>
        /// <param name="name">The name of this step.</param>
        /// <param name="description">The description for this step.</param>
        /// <param name="guid">The GUID of this step.</param>
        public PfcNode(IProcedureFunctionChart parent, string name, string description, Guid guid)
            : base(parent, name, description, guid)
        {

            _predecessors = new PfcLinkElementList();
            _successors = new PfcLinkElementList();
        }

        #endregion Constructors

        #region IPfcNode Members

        /// <summary>
        /// Gets a value indicating whether this instance is simple. A node is simple if it
        /// has one input and one output and performs no tasks beyond a pass-through. In the case
        /// of a Step, a Simple step is a Null step. This also facilitates graph reduction.
        /// </summary>
        /// <value><c>true</c> if this instance is simple; otherwise, <c>false</c>.</value>
        public bool IsSimple
        {
            get
            {
                return _isSimple;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is null. A node that is null can be
        /// eliminated when PFCs are combined.
        /// </summary>
        /// <value><c>true</c> if this instance is null; otherwise, <c>false</c>.</value>
        public virtual bool IsNullNode
        {
            get
            {
                return _isNull;
            }
            set
            {
                _isNull = value;
            }
        }

        public NodeColor NodeColor
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the earliest time that this element can start.
        /// </summary>
        /// <value>The earliest start.</value>
        public DateTime? EarliestStart
        {
            get
            {
                return _earliestStart;
            }
            set
            {
                _earliestStart = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is start node.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is start node; otherwise, <c>false</c>.
        /// </value>
        public bool IsStartNode
        {
            get
            {
                return (_predecessors == null || _predecessors.Count == 0);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is finish node.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is finish node; otherwise, <c>false</c>.
        /// </value>
        public bool IsFinishNode
        {
            get
            {
                return (_successors == null || _successors.Count == 0);
            }
        }

        /// <summary>
        /// A string dictionary containing name/value pairs that represent graphics &amp; layout-related values.
        /// </summary>
        /// <value></value>
        public Dictionary<string, string> GraphicsData
        {
            get
            {
                if (_graphicsData == null)
                {
                    _graphicsData = new Dictionary<string, string>();
                }
                return _graphicsData;
            }
        }


        /// <summary>
        /// Determines whether this instance is connected to anything upstream or downstream.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this instance is connected; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsConnected()
        {
            return PredecessorNodes.Count > 0 || SuccessorNodes.Count > 0;
        }

        /// <summary>
        /// Updates the portion of the structure of the SFC that relates to this element.
        /// This is called after any structural changes in the Sfc, but before the resultant data
        /// are requested externally.
        /// </summary>
        public override void UpdateStructure()
        {
            _successors.Sort(Parent.LinkComparer);
        }

        /// <summary>
        /// Gets or sets the graph ordinal of this node - a number that roughly (but consistently)
        /// represents its place in the execution order for this graph. Loopbacks' ordinals indicate
        /// their place in the execution order as of their first execution.
        /// </summary>
        /// <value>The graph ordinal.</value>
        public int GraphOrdinal
        {
            get
            {
                return _graphOrdinal;
            }
            set
            {
                _graphOrdinal = value;
            }
        }

        /// <summary>
        /// Gets the link that connects this node to a successor node. Returns null if there is no such link.
        /// </summary>
        /// <param name="successorNode">The successor.</param>
        /// <returns></returns>
        public IPfcLinkElement GetLinkForSuccessorNode(IPfcNode successorNode)
        {
            IPfcLinkElement retval = null;
            _successors.ForEach(delegate (IPfcLinkElement le)
            {
                if (le.Successor == successorNode)
                    retval = le;
            });
            return retval;
        }

        /// <summary>
        /// Gets the link that connects this node to a predecessor node. Returns null if there is no such link.
        /// </summary>
        /// <param name="predecessorNode">The predecessor.</param>
        /// <returns></returns>
        public IPfcLinkElement GetLinkForPredecessorNode(IPfcNode predecessorNode)
        {
            IPfcLinkElement retval = null;
            _predecessors.ForEach(delegate (IPfcLinkElement le)
            {
                if (le.Predecessor == predecessorNode)
                    retval = le;
            });
            return retval;
        }

        /// <summary>
        /// Gives the specified link (which must be one of the outbound links from this node) the highest
        /// priority of all links outbound from this node. Retuens false if the specified link is not a
        /// successor link to this node. NOTE: This API will renumber the outbound links' priorities.
        /// </summary>
        /// <param name="outbound">The link, already in existence and an outbound link from this node, that
        /// is to be set to the highest priority of all links already outbound from this node.</param>
        /// <returns></returns>
        public bool SetLinkHighestPriority(IPfcLinkElement outbound)
        {
            if (!_successors.Contains(outbound))
            {
                return false;
            }

            _successors.Sort(Parent.LinkComparer);
            _successors.Remove(outbound);
            _successors.Add(outbound);

            List<IPfcLinkElement> links = new List<IPfcLinkElement>(_successors);
            for (int i = 0; i < links.Count; i++)
            {
                links[i].Priority = (int)(_successors.Count - i);
            }

            _successors.Clear();
            _successors.AddRange(links);

            _successors.Sort(Parent.LinkComparer);

            return true;
        }
        /// <summary>
        /// Gives the specified link (which must be one of the outbound links from this node) the lowest
        /// priority of all links outbound from this node. Retuens false if the specified link is not a
        /// successor link to this node. NOTE: This API will renumber the outbound links' priorities.
        /// </summary>
        /// <param name="outbound">The link, already in existence and an outbound link from this node, that
        /// is to be set to the lowest priority of all links already outbound from this node.</param>
        /// <returns></returns>
        public bool SetLinkLowestPriority(IPfcLinkElement outbound)
        {
            if (!_successors.Contains(outbound))
            {
                return false;
            }

            _successors.Sort(Parent.LinkComparer);
            _successors.Remove(outbound);
            _successors.Insert(0, outbound);

            List<IPfcLinkElement> links = new List<IPfcLinkElement>(_successors);
            for (int i = 0; i < links.Count; i++)
            {
                links[i].Priority = (int)(_successors.Count - i);
            }

            _successors.Clear();
            _successors.AddRange(links);

            _successors.Sort(Parent.LinkComparer);

            return true;
        }

        #endregion

        #region IResettable Members

        /// <summary>
        /// Resets this instance. Used at, or pertaining to, runtime execution.
        /// </summary>
        public override void Reset()
        {
            if (!_isResetting)
            {
                _isResetting = true;
                foreach (IPfcLinkElement linkNode in _predecessors)
                {
                    linkNode.Reset();
                }
                foreach (IPfcLinkElement linkNode in _successors)
                {
                    linkNode.Reset();
                }
                _isResetting = false;
            }
        }

        #endregion

        #region IPfcLinkable Members

        /// <summary>
        /// Gets or sets a value indicating whether the structure of this SFC is dirty (in effect, whether it has changed since
        /// consolidation was last done.
        /// </summary>
        /// <value><c>true</c> if [structure dirty]; otherwise, <c>false</c>.</value>
        public bool StructureDirty
        {
            get
            {
                return _structureDirty;
            }
            set
            {
                if (_structureDirty)
                {
                    return;
                } // TODO: This could be factored better to not use all of the casting.
                _structureDirty = value;
                if (_structureDirty && this is IPfcStepNode)
                {
                    foreach (IProcedureFunctionChart childPfc in ((IPfcStepNode)this).Actions.Values)
                    {
                        foreach (IPfcElement child in childPfc.Nodes)
                        {
                            ((IPfcNode)child).StructureDirty = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the predecessor list for this node.
        /// </summary>
        /// <value>The predecessor link.</value>
        public PfcLinkElementList Predecessors
        {
            get
            {
                return _predecessors;
            }
        }

        /// <summary>
        /// Adds the new predecessor link to this node's list of predecessors.
        /// </summary>
        /// <param name="newPredecessor">The new predecessor link.</param>
        public void AddPredecessor(IPfcLinkElement newPredecessor)
        {
            _Debug.Assert(newPredecessor.Successor != null);
            _predecessors.Add(newPredecessor);
            StructureDirty = true;
        }

        /// <summary>
        /// Removes the predecessor link from this node's list of predecessors.
        /// </summary>
        /// <param name="currentPredecessor">The current predecessor.</param>
        /// <returns></returns>
        public bool RemovePredecessor(IPfcLinkElement currentPredecessor)
        {
            if (!_predecessors.Contains(currentPredecessor))
            {
                return false;
            }
            _predecessors.Remove(currentPredecessor);
            StructureDirty = true;
            return true;
        }

        /// <summary>
        /// Gets the successor list for this node. Do not modify this list.
        /// </summary>
        /// <value>A list of the successor links.</value>
        public PfcLinkElementList Successors
        {
            get
            {
                return _successors;
            }
        }

        /// <summary>
        /// Gets the predecessor node list for this node.
        /// </summary>
        /// <value>A list of the nodes at the other end of this node's predecessors (which are all links).</value>
        public PfcNodeList PredecessorNodes
        {
            get
            {
                PfcNodeList retval = new PfcNodeList();
                foreach (IPfcLinkElement link in _predecessors)
                {
                    if (link.Predecessor != null)
                    {
                        retval.Add(link.Predecessor);
                    }
                }
                return retval;
            }
        }


        /// <summary>
        /// Gets the successor list for this node. Do not modify this list.
        /// </summary>
        /// <value>A list of the nodes at the other end of this node's predecessors (which are all links).</value>
        public PfcNodeList SuccessorNodes
        {
            get
            {
                PfcNodeList retval = new PfcNodeList();
                foreach (IPfcLinkElement link in _successors)
                {
                    if (link.Successor != null)
                    {
                        retval.Add(link.Successor);
                    }
                }
                return retval;
            }
        }

        /// <summary>
        /// Adds the new successor link to this node's list of successors.
        /// </summary>
        /// <param name="newSuccessor">The new successor link.</param>
        public void AddSuccessor(IPfcLinkElement newSuccessor)
        {
            _Debug.Assert(newSuccessor.Predecessor == this);
            _successors.Add(newSuccessor);

            if (!newSuccessor.Priority.HasValue)
            {
                newSuccessor.Priority = 0;
            }

            ResortSuccessorLinks();
            StructureDirty = true;
        }

        /// <summary>
        /// Resorts the successor links according to their priorities.
        /// </summary>
        internal void ResortSuccessorLinks()
        {
            _successors.Sort(new PfcLink.LinkComparer());
        }

        /// <summary>
        /// Removes the successor link from this node's list of successors.
        /// </summary>
        /// <param name="currentSuccessor">The current successor.</param>
        /// <returns></returns>
        public bool RemoveSuccessor(IPfcLinkElement currentSuccessor)
        {
            if (!_successors.Contains(currentSuccessor))
            {
                return false;
            }
            _successors.Remove(currentSuccessor);
            StructureDirty = true;
            return true;
        }

        #endregion

        public class NodeComparer : IComparer<IPfcNode>
        {
            public int Compare(IPfcNode x, IPfcNode y)
            {
                int retval = Comparer<int>.Default.Compare(x.GraphOrdinal, y.GraphOrdinal);
                if (retval == 0)
                {
                    retval = Utility.GuidOps.Compare(x.Guid, y.Guid);
                }
                return retval;
            }
        }
    }
}
