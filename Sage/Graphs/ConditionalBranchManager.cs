/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Graphs.Tasks;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Global

namespace Highpoint.Sage.Graphs
{
    /// <summary>
    /// A conditional branch manager is affiliated with an edge's post-vertex. It stores one or more conditions
    /// and the edge channels associated with each. A default condition causes the null-channeled outbound edge
    /// (or edges) to fire, and until we evaluate expressions, this is the only channel that will actually fire.
    /// Statics on the class are used to establish and remove branch constructs in the SOM.<para></para>
    /// <b>Since we do not evaluate expressions, this manager will assume that the first condition and target(s)
    /// provisioned are to be the default.</b><para></para>
    /// </summary>
    public class ConditionalBranchManager : IEdgeFiringManager
    {

        #region Private Fields
        private static readonly ExecEventReceiver _launchEdge = LaunchEdge;
        private readonly IModel _model;
        private readonly VolatileKey _cbmDataKey = new VolatileKey();
        private readonly List<BranchScenario> _branchScenarios = new List<BranchScenario>();
        private string _defaultChannel;
        #endregion

        #region Constructors

        /// <summary>
        ///
        /// </summary>
        /// <param name="model">The model in which this graph is running. This is necessary because the outbound
        /// edges are fired asynchronously to keep a graph's execution path from looping back over this branch
        /// manager while it is still executing.</param>
        public ConditionalBranchManager(IModel model)
        {
            _model = model;
        }

        #endregion 

        #region IEdgeFiringManager Members

        /// <summary>
        /// This is fired once at the beginning of a branch manager's being asked to review a set of edges,
        /// which happens immediately after a vertex is satisfied.
        /// </summary>
        /// <param name="graphContext">The graph context in which we are currently running.</param>
        public void Start(IDictionary graphContext)
        {
            // Figure out which channel we are going to fire. This will eventually involve evaluating the expressions.
            if (graphContext.Contains(_cbmDataKey))
            {
                graphContext.Remove(_cbmDataKey);
            }

            graphContext.Add(_cbmDataKey, "");

        }

        /// <summary>
        /// Schedules the presented edge to be fired if the edge's channel matches the currently active channel.
        /// </summary>
        /// <param name="graphContext">The graph context in which we are currently running.</param>
        /// <param name="edge">The edge being considered for execution.</param>
        public void FireIfAppropriate(IDictionary graphContext, Edge edge)
        {

            string activeChannel = (string)graphContext[_cbmDataKey];

            DateTime now = _model.Executive.Now;
            double currentPriority = _model.Executive.CurrentPriorityLevel;

            // We only fire the null channel edge, until expressions can be evaluated.
            // We're going to run the null channel
            if (string.IsNullOrEmpty(activeChannel))
            {
                activeChannel = Edge.NULL_CHANNEL_MARKER;
            }

            // The channel specified matches that of the channel we are looking at.
            if (activeChannel.Equals(edge.Channel))
            {
                _model.Executive.RequestEvent(_launchEdge, now, currentPriority, new EdgeLaunchData(edge, graphContext), ExecEventType.Detachable);
            }

        }

        /// <summary>
        /// Clears the list of branch data, essentially removing all branches from this manager.
        /// </summary>
        public void ClearBranches()
        {
            _branchScenarios?.Clear();
            _defaultChannel = null;

        }

        #endregion

        /// <summary>
        /// Adds branch scenarios, which consist of a set of conditions and correlating edge channels that will be
        /// fired for the first condition that evaluates to true (Currently, the first one specified is the one that
        /// actually runs. If a master task is specified, then that task's evaluation of its conditions will be used
        /// to guide the selection of which edge fires on this task.
        /// </summary>
        /// <param name="model">The model in which this branch scenario will run.</param>
        /// <param name="conditions">The branch conditions of each scenario.</param>
        /// <param name="channels">The channels that describe each branch.</param>
        /// <param name="targets">The targets to which each branch goes.</param>
        /// <param name="master">The master task, if there is one.</param>
        public void AddBranchScenarios(IModel model, List<string> conditions, List<string> channels, List<Task> targets, Task master)
        {
            if (conditions.Count != channels.Count)
            {
                throw new ApplicationException(_unequalListSize);
            }

            int nBranches = conditions.Count;
            for (int i = 0; i < nBranches; i++)
            {
                _branchScenarios.Add(new BranchScenario(model, conditions[i], channels[i], targets[i], master));
            }
        }

        /// <summary>
        /// Adds branch scenarios, which consist of a set of conditions and correlating edge channels that will be
        /// fired for the first condition that evaluates to true (Currently, the first one specified is the one that
        /// actually runs. If a master task is specified, then that task's evaluation of its conditions will be used
        /// to guide the selection of which edge fires on this task.
        /// </summary>
        /// <param name="model">The model in which this branch scenario will run.</param>
        /// <param name="condition">The branch condition of this scenario.</param>
        /// <param name="channel">The channel that describes this branch.</param>
        /// <param name="target">The target to which this branch goes.</param>
        /// <param name="master">The master task, if there is one.</param>
        public void AddBranchScenario(IModel model, string condition, string channel, Task target, Task master)
        {

            _branchScenarios.Add(new BranchScenario(model, condition, channel, target, master));
            if (_defaultChannel == null)
            {
                _defaultChannel = _branchScenarios[0].Channel;
            }

        }

        /// <summary>
        /// Gets or sets the channel that will be run if the expressions are un-evaluatable (which all currently are).
        /// </summary>
        /// <value>The default channel.</value>
        public string DefaultChannel
        {
            get
            {
                return _defaultChannel;
            }
            set
            {
                _defaultChannel = value;
            }
        }

        private static void LaunchEdge(IExecutive exec, object userData)
        {
            EdgeLaunchData eld = (EdgeLaunchData)userData;
            eld.Edge.PreVertexSatisfied(eld.GraphContext);
        }

        #region Static Conditional Branch Control Methods

        /// <summary>
        /// Gets the Conditional Branch Manager for a given task.
        /// </summary>
        /// <param name="task">The task whose Conditional Branch Manager is desired.</param>
        /// <param name="force">if set to <c>true</c> [force] creation of a new Conditional Branch Manager.</param>
        /// <returns>The Conditional Branch Manager for a given task.</returns>
        public static ConditionalBranchManager For(Task task, bool force)
        {
            if (!force && task.PostVertex.EdgeFiringManager != null)
            {
                throw new ApplicationException(string.Format(_cantForceOverride, task.Name));
            }

            if (task.PostVertex.EdgeFiringManager == null)
            {
                task.PostVertex.EdgeFiringManager = new ConditionalBranchManager((Model)task.Model);
            }

            return (ConditionalBranchManager)task.PostVertex.EdgeFiringManager;
        }

        /// <summary>
        /// Registers branch conditions for branching to be performed by a Conditional Branch Manager.
        /// The first condition specified is assumed to be the one that is true.
        /// </summary>
        /// <param name="model">The model in which these branch scenarios will run.</param>
        /// <param name="source">The source task from whose post vertex the branches emit.</param>
        /// <param name="conditions">The indexed conditions under which each channel is activated.</param>
        /// <param name="channels">The indexed channels for each branch.</param>
        /// <param name="targets">The target tasks to which the branches will pass control.</param>
        /// <param name="master">The master edge. Null, or same as source, if source is master.</param>
        /// <param name="force">if set to <c>true</c> [force] creation of a new Conditional Branch Manager.</param>
        public static void AddBranchScenarios(IModel model, Task source, List<string> conditions, List<string> channels, List<Task> targets, Task master, bool force)
        {
            ConditionalBranchManager cbm = For(source, true);
            cbm.AddBranchScenarios(model, conditions, channels, targets, master);
        }

        /// <summary>
        /// Clears the conditional branch data for the provided task.
        /// </summary>
        /// <param name="task">The task.</param>
        public static void ClearBranchesFor(Task task)
        {
            task.PostVertex.EdgeFiringManager = null;
        }

        /// <summary>
        /// Creates a branch link from one task's post vertex to another task's preVertex.
        /// </summary>
        /// <param name="from">The task whose post vertex the branch is to emanate from.</param>
        /// <param name="to">The task to whose pre vertex the branch is to convey control.</param>
        /// <param name="channel">The channel.</param>
        public static void CreateBranchLink(Task from, Task to, string channel)
        {
            Edge.Connect(from.PostVertex, to.PreVertex).Channel = channel;
        }

        /// <summary>
        /// Gets the branch scenarios managed by this BranchManager.
        /// </summary>
        /// <value>The targets.</value>
        public IEnumerable<IBranchScenario> BranchScenarios => _branchScenarios;

        #endregion

        public interface IBranchScenario
        {
            string Channel
            {
                get;
            }
            string Condition
            {
                get;
            }
            Task Target
            {
                get;
            }
            Task Master
            {
                get;
            }
        }

        #region Private Support Classes

        /// <summary>
        /// Encapsulates a condition, a channel and a set of slave edges.
        /// </summary>
        private class BranchScenario : IBranchScenario
        {
            private readonly IModel _model;
            private Task _target;
            private Task _master;
            private Guid _masterGuid;
            private Guid _targetGuid;

            public BranchScenario(IModel model, string condition, string channel, Task target = null, Task master = null)
            {
                _model = model;
                Condition = condition;
                Channel = channel;
                _master = master;
                _target = target;
                _masterGuid = master?.Guid ?? Guid.Empty;
                _targetGuid = target?.Guid ?? Guid.Empty;
            }

            public string Condition
            {
                get; set;
            }

            public string Channel
            {
                get; set;
            }

            public Task Master
            {
                get
                {
                    if (_master == null && _masterGuid != Guid.Empty)
                    {
                        _master = (Task)_model.ModelObjects[_masterGuid];
                    }
                    return _master;
                }
                set
                {
                    _master = value;
                }
            }

            public Guid MasterGuid
            {
                get
                {
                    return _masterGuid;
                }
                set
                {
                    _masterGuid = value;
                }
            }

            public Task Target
            {

                get
                {
                    if (_target == null && _targetGuid != Guid.Empty)
                    {
                        _target = (Task)_model.ModelObjects[_targetGuid];
                    }
                    return _target;
                }
                set
                {
                    _target = value;
                }
            }

            public Guid TargetGuid
            {
                get
                {
                    return _targetGuid;
                }
                set
                {
                    _targetGuid = value;
                }
            }
        }

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

        #endregion 

        public static string ToXmlString(ConditionalBranchManager cbm)
        {

            StringBuilder sb = new StringBuilder();
            sb.Append("<ConditionalBranchManager>\r\n<DefaultChannel>" + XmlTransform.Xmlify(cbm._defaultChannel) + "</DefaultChannel>\r\n");
            foreach (BranchScenario bs in cbm._branchScenarios)
            {

                sb.Append("<BranchScenario>\r\n"
                    + "<Channel>" + XmlTransform.Xmlify(bs.Channel) + "</Channel>\r\n"
                    + "<Condition>" + XmlTransform.Xmlify(bs.Condition) + "</Condition>\r\n"
                    + "<TargetGuid>" + XmlConvert.ToString(bs.TargetGuid) + "</TargetGuid>\r\n"
                    + "<MasterGuid>" + XmlConvert.ToString(bs.MasterGuid) + "</MasterGuid>\r\n"
                    + "</BranchScenario>\r\n");

            }

            sb.Append("</ConditionalBranchManager>");

            return sb.ToString();
        }

        public static ConditionalBranchManager FromXml(IModel model, XmlNode node)
        {
            if (node == null)
                throw new ArgumentException("Attempt to create a ConditionalBranchManager from a null XmlNode.");

            XmlNode selectSingleNode = node.SelectSingleNode("DefaultChannel");
            if (selectSingleNode != null)
            {
                ConditionalBranchManager cbm = new ConditionalBranchManager(model)
                {
                    _defaultChannel = XmlTransform.DeXmlify(selectSingleNode.InnerText)
                };


                XmlNodeList xmlNodeList = node.SelectNodes("BranchScenario");
                if (xmlNodeList != null)
                    foreach (XmlNode innerNode in xmlNodeList)
                    {
                        XmlNode singleNode = innerNode.SelectSingleNode("Condition");
                        if (singleNode == null)
                            continue;
                        string condition = singleNode.InnerText;
                        //condition = XmlConvert.deXmlify(condition); Not necessary, since it came from an Xml document in the first place.

                        XmlNode xmlNode = innerNode.SelectSingleNode("Channel");
                        if (xmlNode == null)
                            continue;
                        string channel = xmlNode.InnerText;

                        //channel = XmlConvert.deXmlify(channel); Not necessary, since it came from an Xml document in the first place.

                        XmlNode selectSingleNode1 = innerNode.SelectSingleNode("MasterGuid");
                        if (selectSingleNode1 == null)
                            continue;
                        Guid masterGuid = XmlConvert.ToGuid(selectSingleNode1.InnerText);

                        XmlNode singleNode1 = innerNode.SelectSingleNode("TargetGuid");
                        if (singleNode1 == null)
                            continue;
                        Guid targetGuid = XmlConvert.ToGuid(singleNode1.InnerText);

                        BranchScenario bs = new BranchScenario(model, condition, channel)
                        {
                            MasterGuid = masterGuid,
                            TargetGuid = targetGuid
                        };

                        cbm._branchScenarios.Add(bs);
                    }

                return cbm;
            }
            return null;
        }

        private static readonly string _cantForceOverride =
            "Cannot force override of existing IEdgeFiringManager already attached to task {0}.";

        private static readonly string _unequalListSize =
            "Caller provided a condition list with an unequal number of elements to that of the list of channels in task.";

    }
}
