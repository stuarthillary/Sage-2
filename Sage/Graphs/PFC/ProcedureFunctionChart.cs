/* COPYRIGHT_NOTICE */
using Highpoint.Sage.Diagnostics;
using Highpoint.Sage.Graphs.PFC.Execution;
using Highpoint.Sage.Graphs.PFC.Expressions;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using XMLCONVERT = System.Xml.XmlConvert;

namespace Highpoint.Sage.Graphs.PFC
{

    public delegate IPfcStepNode StartStepResolver(IProcedureFunctionChart pfc);

    /// <summary>
    /// Represents &amp; contains a Procedure Function Chart instance. Includes methods for building,
    /// triggering, and reducing an SFC graph. The SFC is maintained SFC-Compliant (meaning that Transitions 
    /// always lead to Steps, and Steps always lead to Transitions, and both always do so via Links.
    /// </summary>
    [XmlSchemaProvider("GetPfcSchema")]
    public class ProcedureFunctionChart : IProcedureFunctionChart
    {

        #region Private Members

        private IModel _model;
        private string _name = null;
        private string _description = null;
        private Guid _guid = Guid.Empty;

        private static readonly bool _diagnostics = DiagnosticAids.Diagnostics("ProcedureFunctionChart");

        private IPfcStepNode _parent = null;

        private PfcNodeList _nodeList = null;
        private PfcStepNodeList _stepNodeList = null;
        private PfcLinkElementList _linkNodeList = null;
        private PfcTransitionNodeList _transitionNodeList = null;

        private static Guid _pfcFromStepMaskGuid = new Guid("89415910-A44D-4d0b-BCBC-29E19BE378D5");
        private Guid _modelMaskForSelfGuid = new Guid("8BC12586-C2BD-405a-BABF-37F8F19F7535");
        private Guid _elementFactoryMaskGuid = new Guid("B66CE340-FF4A-43e0-A85F-270C06AE8373");
        private Guid _guidGeneratorMaskGuid = new Guid("C50E5875-E03E-4560-8D54-0D57C9EA03B5");
        private Guid _guidGeneratorSeedMaskGuid = new Guid("D1137F29-DAE7-4342-97EE-E10DFABE452C");
        private IPfcElementFactory _sfcElementFactory = null;

        private GuidGenerator _guidGenerator = null;

        private ParticipantDirectory _participantDirectory;

        private static bool _defaultNullness = false; // Newly-created nodes are, by default, not null.

        private static string _serializerVersionTag = "SerializationVersion";
        private static double _currentSerializerVersion = 0.29;
        private double _currentlyDeserializingVersion = 0.0;

        private DateTime? _earliestStart = null;

        /// <summary>
        /// PathLengthCap is the maximum path length that _LookForwardForNodesOnPathEndingAt will use.
        /// This becomes necessary in large recipes to limit the exponentially-explosive search time.
        /// </summary>
        private int _pathLengthCap = 60;

        private IProcedureFunctionChart _source = null;

        private static ExpressionElement _defaultExpression = new PredecessorsComplete();

        private ExecutionEngine _executionEngine = null;

        private ExecutionEngineConfiguration _executionEngineConfiguration = null;

        private static IComparer<IPfcLinkElement> _linkComparer = new PfcLink.LinkComparer();
        private IComparer<IPfcLinkElement> m_linkComparer = _linkComparer;

        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="T:ProcedureFunctionChart"/> class.
        /// </summary>
        public ProcedureFunctionChart() : this(null, "PFC", "", Guid.Empty) { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:ProcedureFunctionChart"/> class.
        /// </summary>
        /// <param name="model">The model in which this <see cref="T:ProcedureFunctionChart"/> will run.</param>
        public ProcedureFunctionChart(IModel model) : this(model, "PFC", "", Guid.Empty) { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:ProcedureFunctionChart"/> class.
        /// </summary>
        /// <param name="model">The model in which this object runs.</param>
        /// <param name="name">The name of the new <see cref="T:ProcedureFunctionChart"/>.</param>
        public ProcedureFunctionChart(IModel model, string name) : this(model, name, "", Guid.Empty) { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:ProcedureFunctionChart"/> class.
        /// </summary>
        /// <param name="model">The model in which this <see cref="T:ProcedureFunctionChart"/> will run.</param>
        /// <param name="name">The name of the new <see cref="T:ProcedureFunctionChart"/>.</param>
        /// <param name="description">The description of the new <see cref="T:ProcedureFunctionChart"/>.</param>
        /// <param name="guid">The GUID of the new <see cref="T:ProcedureFunctionChart"/>.</param>
        /// <param name="elementFactory">The element factory from which this SFC will create its new elements.</param>
        public ProcedureFunctionChart(IModel model, string name, string description, Guid guid, IPfcElementFactory elementFactory)
            : this(model, name, description, guid)
        {
            _sfcElementFactory = elementFactory;
            _sfcElementFactory.HostPfc = this;
            _guidGenerator = _sfcElementFactory.GuidGenerator;
            StartStepResolver = null;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="T:ProcedureFunctionChart"/> class.
        /// </summary>
        /// <param name="model">The model in which this <see cref="T:ProcedureFunctionChart"/> will run.</param>
        /// <param name="name">The name of the new <see cref="T:ProcedureFunctionChart"/>.</param>
        /// <param name="description">The description of the new <see cref="T:ProcedureFunctionChart"/>.</param>
        /// <param name="guid">The GUID of the new <see cref="T:ProcedureFunctionChart"/>.</param>
        public ProcedureFunctionChart(IModel model, string name, string description, Guid guid)
        {

            if (guid.Equals(Guid.Empty))
            {
                guid = Guid.NewGuid();
            }

            DoPruneOrphans = true;

            InitializeIdentity(model, name, description, guid);

            _stepNodeList = new PfcStepNodeList();
            _linkNodeList = new PfcLinkElementList();
            _transitionNodeList = new PfcTransitionNodeList();
            _nodeList = new PfcNodeList();
            _guidGenerator = new GuidGenerator(Guid.Empty, Guid.Empty, 0);
            _guidGenerator.Passthrough = true;
            _sfcElementFactory = new PfcElementFactory(this);
            _participantDirectory = new ParticipantDirectory();
            _participantDirectory.RegisterMacro(typeof(PredecessorsComplete));

            IMOHelper.RegisterWithModel(this);
        }

        private void InitializeGuidGenerators(Guid providedGuid)
        {

            #region PFC Guid assignment.
            if (!providedGuid.Equals(Guid.Empty))
            {
                _guid = providedGuid;
            }
            else if (_model != null)
            {
                _guid = GuidOps.XOR(_model.Guid, _modelMaskForSelfGuid);
            }
            else
            {
                _guid = Guid.NewGuid();
            }
            #endregion

            #region Main Guid Generator
            Guid seed = GuidOps.XOR(_guid, _guidGeneratorSeedMaskGuid);
            _guidGenerator = new GuidGenerator(seed, _guidGeneratorMaskGuid, 7);
            #endregion

            #region Element Factory Guid Generator
            _sfcElementFactory = new PfcElementFactory(this);
            Guid baseGuid = GuidOps.XOR(_guid, _elementFactoryMaskGuid);
            ((PfcElementFactory)_sfcElementFactory).SetRepeatable(baseGuid);
            #endregion

        }

        /// <summary>
        /// Creates a new SFC with the given step as its root element.
        /// </summary>
        /// <param name="step">The step.</param>
        /// <returns></returns>
        public static ProcedureFunctionChart CreateFromStep(IPfcStepNode step)
        {
            return CreateFromStep(step, false);
        }

        /// <summary>
        /// Creates a new SFC with the given step as its root element.
        /// </summary>
        /// <param name="step">The step.</param>
        /// <param name="autoFlatten">if set to <c>true</c> automatically flattens the resultant PFC.</param>
        /// <returns></returns>
        public static ProcedureFunctionChart CreateFromStep(IPfcStepNode step, bool autoFlatten)
        {

            if (step.Actions == null)
            {
                throw new ApplicationException(string.Format("Deriving a Pfc from a step ({0}) that has no Actions is not possible.", step.Name));
            }

            if (step.Actions.Count > 1)
            {
                throw new NotSupportedException(string.Format("Deriving a single Pfc from a step ({0}) that has more than one Action is not yet supported.", step.Name));
            }

            ProcedureFunctionChart pfc = null;
            if (step.Actions == null || step.Actions.Count == 0)
            {
                return new ProcedureFunctionChart(step.Model, step.Name + ".PFC", "", GuidOps.XOR(step.Guid, _pfcFromStepMaskGuid));
            }
            else
            {
                foreach (ProcedureFunctionChart subPfc in step.Actions.Values)
                {
                    if (autoFlatten)
                    {
                        subPfc.Flatten();
                    }
                    pfc = subPfc;
                }
            }

            return pfc;
        }

        #endregion Constructors

        ///// <summary>
        ///// Not yet implemented.
        ///// Initializes the SFC with the specified steps, transitions and links. Any pre-existent nodes are cleared out.
        ///// </summary>
        ///// <param name="steps">The steps.</param>
        ///// <param name="transitions">The transitions.</param>
        ///// <param name="links">The links.</param>
        //public void Initialize(PfcStepNodeList steps, PfcTransitionNodeList transitions, PfcLinkElementList links) {
        //    throw new NotImplementedException();
        //}

        #region ElementFactory Members

        /// <summary>
        /// Gets or sets the element factory in use by this ProcedureFunctionChart.
        /// </summary>
        /// <value>The element factory.</value>
        public IPfcElementFactory ElementFactory
        {
            get
            {
                return _sfcElementFactory;
            }
            set
            {
                _sfcElementFactory = value;
            }
        }

        #endregion

        #region Element Creation

        #region Create SfcStep

        /// <summary>
        /// Creates and adds a step with the specified information. Throws an exception if the Guid is already in use.
        /// </summary>
        /// <returns>The <see cref="T:IPfcStepNode"/>.</returns>
        public IPfcStepNode CreateStep()
        {
            return CreateStep(null, null, Guid.Empty);
        }

        /// <summary>
        /// Creates and adds a step with the specified information. Throws an exception if the Guid is already in use.
        /// </summary>
        /// <param name="name">The name of the step.</param>
        /// <param name="description">The description of the step.</param>
        /// <param name="guid">The GUID of the step.</param>
        /// <returns>The <see cref="T:IPfcStepNode"/>.</returns>
        public IPfcStepNode CreateStep(string name, string description, Guid guid)
        {
            if (guid.Equals(Guid.Empty))
            {
                guid = _guidGenerator.Next();
            }
            IPfcStepNode step = _sfcElementFactory.CreateStepNode(name, guid, description);
            step.IsNullNode = _defaultNullness;

            _stepNodeList.Add(step);
            _nodeList.Add(step);
            _participantDirectory.RegisterMapping(step.Name, step.Guid);

            return step;
        }
        #endregion Create SfcStep

        #region Create SfcTransition
        /// <summary>
        /// Creates and adds a transition with default information. Throws an exception if the Guid is already in use.
        /// </summary>
        /// <returns>The <see cref="T:IPfcTransitionNode"/>.</returns>
        public IPfcTransitionNode CreateTransition()
        {
            return CreateTransition(null, null, Guid.Empty);
        }

        /// <summary>
        /// Creates and adds a transition with the specified information. Throws an exception if the Guid is already in use.
        /// </summary>
        /// <param name="name">Name of the transition.</param>
        /// <param name="description">The transition description.</param>
        /// <param name="guid">The transition GUID.</param>
        /// <returns>The <see cref="T:IPfcTransitionNode"/>.</returns>
        public IPfcTransitionNode CreateTransition(string name, string description, Guid guid)
        {
            if (guid.Equals(Guid.Empty))
            {
                guid = _guidGenerator.Next();
            }

            IPfcTransitionNode transition = _sfcElementFactory.CreateTransitionNode(name, guid, description);

            _transitionNodeList.Add(transition);
            _nodeList.Add(transition);

            // PCB, 10/29/06: During load, while adding child PFC under an action, the child Pfc's 
            // existing connections were added into the parent ParticipantDirectory, followed by those
            // of the parent as we conencted it node-to-node. This is backwards, since it gives
            // naming precedence to the transitions of the child nodes. Transitions never are members
            // of expressions anyhow, so we don't need them in the Participant Directory.
            //m_participantDirectory.RegisterMapping(transition.Name, transition.Guid);

            return transition;
        }

        #endregion Create SfcTransition

        #region Create SfcLink

        /// <summary>
        /// Creates a new link. It must later be bound to a predecessor and a successor.
        /// Throws an exception if the Guid is already known to this ProcedureFunctionChart.
        /// </summary>
        /// <returns>The <see cref="T:IPfcLinkElement"/>.</returns>
        public IPfcLinkElement CreateLink()
        {
            return CreateLink(null, null, Guid.Empty);
        }

        /// <summary>
        /// Creates a new link. It must later be bound to a predecessor and a successor.
        /// Throws an exception if the Guid is already known to this ProcedureFunctionChart.
        /// </summary>
        /// <param name="name">The name of the new link.</param>
        /// <param name="description">The description of the new link.</param>
        /// <param name="guid">The GUID of the new link.</param>
        /// <returns>The <see cref="T:IPfcLinkElement"/>.</returns>
        public IPfcLinkElement CreateLink(string name, string description, Guid guid)
        {
            if (guid.Equals(Guid.Empty))
            {
                guid = _guidGenerator.Next();
            }

            IPfcLinkElement link = _sfcElementFactory.CreateLinkElement(name, guid, description);

            return link;
        }

        /// <summary>
        /// Creates a link with the specified name &amp; guid.
        /// </summary>
        /// <param name="name">The name of the new link.</param>
        /// <param name="description">The description of the new link.</param>
        /// <param name="guid">The GUID of the new link.</param>
        /// <param name="predecessor">The predecessor to the new link.</param>
        /// <param name="successor">The successor of the new link.</param>
        /// <returns>The <see cref="T:IPfcLinkElement"/>.</returns>
        public IPfcLinkElement CreateLink(string name, string description, Guid guid, IPfcNode predecessor, IPfcNode successor)
        {

            if (guid.Equals(Guid.Empty))
            {
                guid = _guidGenerator.Next();
            }

            IPfcLinkElement link = null;
            foreach (IPfcLinkElement _link in predecessor.Successors)
            {
                if (_link.Successor.Equals(successor))
                {
                    link = _link;
                    break;
                }
            }

            if (link == null)
            {
                link = CreateLink(name, description, guid);

                _linkNodeList.Add(link);

                if (predecessor != null)
                {
                    Bind(predecessor, link);
                }
                if (successor != null)
                {
                    Bind(link, successor);
                }
            }

            return link;

        }

        #endregion Create SfcLink

        /// <summary>
        /// Adds the element to the PFC.
        /// </summary>
        /// <param name="element"></param>
        public void AddElement(IPfcElement element)
        {
            switch (element.ElementType)
            {
                case PfcElementType.Link:
                    _linkNodeList.Add((IPfcLinkElement)element);
                    break;
                case PfcElementType.Transition:
                    _transitionNodeList.Add((IPfcTransitionNode)element);
                    _nodeList.Add((IPfcNode)element);
                    break;
                case PfcElementType.Step:
                    _stepNodeList.Add((IPfcStepNode)element);
                    _nodeList.Add((IPfcNode)element);
                    break;
                default:
                    break;
            }
        }

        #endregion Element Creation

        #region Binding

        #region Simple Binding & Unbinding Constructs

        /// <summary>
        /// Binds the two nodes. If both are steps, it inserts a transition between them, and if both are 
        /// transitions, it inserts a step between them - in both cases, creating links between the 'from'
        /// node, the shim node, and the 'to' node. Piggybacking is allowed by default. Use the full-featured
        /// API to disallow piggybacking.
        /// </summary>
        /// <param name="from">The node from which a connection is being established.</param>
        /// <param name="to">The node to which a connection is being established.</param>
        public void Bind(IPfcNode from, IPfcNode to)
        {
            IPfcLinkElement iPfcLink1;
            IPfcNode shimNode;
            IPfcLinkElement iPfcLink2;
            Bind(from, to, out iPfcLink1, out shimNode, out iPfcLink2, true);
        }

        /// <summary>
        /// Binds the two nodes. If both are steps, it inserts a transition between them, and if both are
        /// transitions, it inserts a step between them - in both cases, creating links between the 'from'
        /// node, the shim node, and the 'to' node. If piggybacking is allowed, and a suitable path already exists,
        /// we use that path instead. A suitable path is either a link between differently-typed nodes, or a
        /// link-node-link path between same-typed nodes, where the interstitial node is simple, and opposite-typed.
        /// </summary>
        /// <param name="from">The node from which a connection is being established.</param>
        /// <param name="to">The node to which a connection is being established.</param>
        /// <param name="iPfcLink1">The first link element.</param>
        /// <param name="shimNode">The shim node, if one was created.</param>
        /// <param name="iPfcLink2">The second link element, if one was created.</param>
        /// <param name="allowPiggybacking">if set to <c>true</c>, we allow an existing link to serve the purpose of this requested link.</param>
        public void Bind(IPfcNode from, IPfcNode to, out IPfcLinkElement iPfcLink1, out IPfcNode shimNode, out IPfcLinkElement iPfcLink2, bool allowPiggybacking)
        {
            iPfcLink1 = iPfcLink2 = null;
            shimNode = null;

            if (allowPiggybacking)
            {
                // If we're binding from A to B, and there's already a direct link, or a link through another shim node,
                // do we want to piggyback on that link/chain, or create a new one? I'm saying that if no shim node is
                // required, or if the shim node is designated as a null node, we piggyback this bind request onto the
                // existing construct.
                if (!from.ElementType.Equals(to.ElementType))
                {
                    // A link is all that's needed - if a link already exists, then we don't create a new one.
                    if (from.SuccessorNodes.Contains(to))
                    {
                        return;
                    }
                }
                else
                {
                    // There's a required shim node. If the shim node is simple, and connects to the 'to' node, we piggyback. 
                    foreach (IPfcNode node in from.SuccessorNodes)
                    {
                        if (node.IsSimple && node.SuccessorNodes.Contains(to))
                        {
                            return;
                        }
                    }
                }
            }

            shimNode = RequiredShimNode(from, to);

            if (shimNode != null)
            {
                iPfcLink1 = CreateLink(null, null, Guid.Empty, from, shimNode);
                iPfcLink2 = CreateLink(null, null, Guid.Empty, shimNode, to);
            }
            else
            {
                // It's step-to-transition or transition-to-step, or we don't care about SfcCompliance. Just link 'em!
                iPfcLink1 = CreateLink(null, null, Guid.Empty, from, to);
            }
        }

        /// <summary>
        /// Binds the linkable and the link node. If the link node is floating (and has nothing on the other end yet),
        /// it just binds them. Otherwise, if the link is bound to the same type of node on the other side that we've
        /// indicated to bind to, it inserts a shim node of the opposite type between the two same-type nodes.
        /// </summary>
        /// <param name="from">The upstream node of the binding.</param>
        /// <param name="to">The downstream node of the binding.</param>
        public void Bind(IPfcNode from, IPfcLinkElement to)
        {
            if (to.Successor != null)
            {
                IPfcNode shimNode = RequiredShimNode(from, to.Successor);
                if (shimNode != null)
                {
                    // Bind them.
                    CreateLink(null, null, Guid.Empty, from, shimNode);
                    Connect(shimNode, to);
                }
            }
            Connect(from, to);
        }

        /// <summary>
        /// Binds the linkable and the link node. If the link node is floating (and has nothing on the other end yet),
        /// it just binds them. Otherwise, if the link is bound to the same type of node on the other side that we've
        /// indicated to bind to, it inserts a shim node of the opposite type between the two same-type nodes.
        /// </summary>
        /// <param name="from">The upstream node of the binding.</param>
        /// <param name="to">The downstream node of the binding.</param>
        public void Bind(IPfcLinkElement from, IPfcNode to)
        {
            if (from.Predecessor != null)
            {
                IPfcNode shimNode = RequiredShimNode(from.Predecessor, to);
                if (shimNode != null)
                {
                    Bind(from, shimNode);
                    Bind(shimNode, to);
                }
                else
                {
                    // No shim node required, so tie the
                    Connect(from, to);
                }
            }
            else
            {
                Connect(from, to);
            }
        }

        /// <summary>
        /// Unbinds the two nodes, removing ONE link between them. Returns false if they were
        /// not connected directly in the first place. If called directly by the user, this
        /// API can result in an illegal PFC graph. Note that if they are connected by two sets
        /// of links, this will not completely disconnect them - for infallible disconnection,
        /// call this method repeatedly until it returns false.
        /// </summary>
        /// <param name="from">The upstream node of the unbinding.</param>
        /// <param name="to">The downstream node of the unbinding.</param>
        /// <param name="skipStructureUpdating">if set to <c>true</c> skips the UpdateStructure. Useful for optimizing bulk updates.</param>
        /// <returns></returns>
        public bool Unbind(IPfcNode from, IPfcNode to, bool skipStructureUpdating = false)
        {
            bool retval = false;

            //if (to.SuccessorNodes.Contains(from)) {
            //    IPfcNode tmp = from;
            //    from = to;
            //    to = tmp;
            //}

            if (from.SuccessorNodes.Contains(to))
            {
                var copyOfList = from.Successors.ToList(); // Avoid mutating a list under iteration.
                foreach (IPfcLinkElement link in copyOfList)
                {
                    if (link.Successor.Equals(to))
                    {
                        link.Detach();
                        from.Successors.Remove(link);
                        to.Predecessors.Remove(link);
                        retval = true;
                        break;
                    }
                }
            }

            if (!skipStructureUpdating)
                UpdateStructure();

            return retval;
        }

        /// <summary>
        /// Unbinds the node from the link. Returns false if they were not
        /// connected directly in the first place. If called directly by
        /// the user, this API can result in an illegal PFC graph.
        /// </summary>
        /// <param name="from">The upstream node of the unbinding.</param>
        /// <param name="to">The downstream link of the unbinding.</param>
        /// <param name="skipStructureUpdating">if set to <c>true</c> skips the UpdateStructure. Useful for optimizing bulk updates.</param>
        /// <returns>True, if successful, otherwise, false.</returns>
        public bool Unbind(IPfcNode from, IPfcLinkElement to, bool skipStructureUpdating = false)
        {
            if (to.Predecessor == null || !to.Predecessor.Equals(from))
            {
                return false;
            }
            Disconnect(from, to);

            if (!skipStructureUpdating)
                UpdateStructure();

            return true;
        }

        /// <summary>
        /// Unbinds the link from the node. Returns false if they were not
        /// connected directly in the first place. If called directly by
        /// the user, this API can result in an illegal PFC graph.
        /// </summary>
        /// <param name="from">The upstream link of the unbinding.</param>
        /// <param name="to">The downstream node of the unbinding.</param>
        /// <param name="skipStructureUpdating">if set to <c>true</c> skips the UpdateStructure. Useful for optimizing bulk updates.</param>
        /// <returns>True, if successful, otherwise, false.</returns>
        public bool Unbind(IPfcLinkElement from, IPfcNode to, bool skipStructureUpdating = false)
        {
            if (!from.Successor.Equals(to))
            {
                return false;
            }
            Disconnect(from, to);

            if (!skipStructureUpdating)
                UpdateStructure();

            return true;
        }

        /// <summary>
        /// Prunes the specified node. If it has no predecessors, it is removed from its successors,
        /// and they are pruned back. If it has no successors, it is removed from its predecessors,
        /// and they are pruned back.
        /// </summary>
        /// <param name="node">The starting node for pruning.</param>
        private void Prune(IPfcNode node)
        {
            SuspendNodeSorting();
            if (node.PredecessorNodes.Count == 0)
            {
                foreach (PfcNode succ in node.SuccessorNodes)
                {
                    Unbind(node, succ);
                    Prune(succ);
                }
            }

            if (node.SuccessorNodes.Count == 0)
            {
                foreach (PfcNode pred in node.PredecessorNodes)
                {
                    Unbind(pred, node);
                    Prune(pred);
                }
            }
            ResumeNodeSorting();
        }

        public StartStepResolver StartStepResolver
        {
            get; set;
        }

        /// <summary>
        /// Gets the start steps in this ProcedureFunctionChart.
        /// </summary>
        /// <returns>The start steps.</returns>
        public List<IPfcStepNode> GetStartSteps()
        {
            List<IPfcStepNode> retval = new List<IPfcStepNode>();
            foreach (IPfcStepNode step in Steps)
            {
                if (step.PredecessorNodes.Count == 0)
                {
                    retval.Add(step);
                }
            }
            return retval;
        }

        /// <summary>
        /// Gets the finish steps in this ProcedureFunctionChart.
        /// </summary>
        /// <returns>The finish steps.</returns>
        [Obsolete("There can only be a finish transition.")]
        public List<IPfcStepNode> GetFinishSteps()
        {
            List<IPfcStepNode> retval = new List<IPfcStepNode>();
            foreach (IPfcStepNode step in Steps)
            {
                if (step.SuccessorNodes.Count == 0)
                {
                    retval.Add(step);
                }
            }
            return retval;
        }

        /// <summary>
        /// Gets the finish transition in this ProcedureFunctionChart.
        /// </summary>
        /// <returns>The finish transition.</returns>
        public IPfcTransitionNode GetFinishTransition()
        {
            foreach (IPfcTransitionNode trans in Transitions)
            {
                if (trans.SuccessorNodes.Count == 0)
                {
                    return trans;
                }
            }
            throw new ApplicationException("PFC " + Name + " has no finish transition.");
        }

        /// <summary>
        /// If the two linkable nodes match in type, it returns the opposite type. Otherwise it returns null.
        /// </summary>
        /// <param name="from">The upstream node of the intended binding.</param>
        /// <param name="to">The downstream node of the intended binding.</param>
        /// <returns>
        /// The necessary shim node, or none if no shim node is required.
        /// </returns>
        private IPfcNode RequiredShimNode(IPfcNode from, IPfcNode to)
        {
            IPfcNode retval = null;
            if (from.ElementType.Equals(to.ElementType))
            {
                if (from.ElementType.Equals(PfcElementType.Step))
                {
                    // There are two steps - we need to shim with a transition.
                    retval = CreateTransition();
                    // Transitions' nullness depends on their expressions.
                }
                else if (from.ElementType.Equals(PfcElementType.Transition))
                {
                    // There are two transitions - we need to shim with a step.
                    retval = CreateStep();
                    retval.IsNullNode = true;
                }
            }
            return retval;
        }

        private void Connect(IPfcLinkElement from, IPfcNode to)
        {
            ((PfcLink)from).Successor = to;
            to.AddPredecessor(from);
        }

        private void Connect(IPfcNode from, IPfcLinkElement to)
        {
            ((PfcLink)to).Predecessor = from;
            from.AddSuccessor(to);
        }

        private void Disconnect(IPfcLinkElement from, IPfcNode to)
        {
            ((PfcLink)from).Successor = null;
            to.RemovePredecessor(from);
        }

        private void Disconnect(IPfcNode from, IPfcLinkElement to)
        {
            ((PfcLink)to).Predecessor = null;
            from.RemoveSuccessor(to);
        }

        #endregion Simple Binding Constructs

        #region Complex Binding

        #region Bind Series Convergent

        /// <summary>
        /// Binds the inbound nodes to the outbound node through a series convergent link structure. Since the caller has indicated
        /// that they desire a SERIES convergent structure, appropriate shimming is done to ensure that the convergence node is a
        /// step.
        /// </summary>
        /// <param name="inbounds">The inbound linkables.</param>
        /// <param name="outbound">The outbound linkable.</param>
        public void BindSeriesConvergent(IPfcNode[] inbounds, IPfcNode outbound)
        {

            // The convergence node in a series convergence is a step. So if outbound isn't a step, we shim with one.
            if (!outbound.ElementType.Equals(PfcElementType.Step))
            {
                IPfcStepNode shimNode = CreateStep();
                shimNode.IsNullNode = true;
                Bind(shimNode, outbound);
                outbound = shimNode;
            }

            foreach (IPfcNode inbound in inbounds)
            {
                Bind(inbound, outbound);
            }
        }

        /// <summary>
        /// Binds the inbound nodes to the outbound node through a series convergent link structure. Since the caller has indicated
        /// that they desire a SERIES convergent structure, appropriate shimming is done to ensure that the convergence node is a
        /// step.
        /// </summary>
        /// <param name="inbound">The inbound linkables.</param>
        /// <param name="outbound">The outbound linkable.</param>
        public void BindSeriesConvergent(PfcNodeList inbound, IPfcNode outbound)
        {
            BindSeriesConvergent(inbound.ToArray(), outbound);
        }

        #endregion BindSeries Convergent

        #region Bind Parallel Convergent

        /// <summary>
        /// Binds the inbound nodes to the outbound node through a parallel convergent link structure. Since the caller has indicated
        /// that they desire a PARALLEL convergent structure, appropriate shimming is done to ensure that the convergence node is a
        /// step.
        /// </summary>
        /// <param name="inbounds">The inbound linkables.</param>
        /// <param name="outbound">The outbound linkable.</param>
        public void BindParallelConvergent(IPfcNode[] inbounds, IPfcNode outbound)
        {
            // The convergence node in a parallel convergence is a transition. So if outbound isn't a transition, we shim with one.
            if (!outbound.ElementType.Equals(PfcElementType.Transition))
            {
                IPfcTransitionNode shimNode = CreateTransition();
                Bind(shimNode, outbound);
                outbound = shimNode;
            }

            foreach (IPfcNode inbound in inbounds)
            {
                Bind(inbound, outbound);
            }
        }

        /// <summary>
        /// Binds the inbound nodes to the outbound node through a parallel convergent link structure. Since the caller has indicated
        /// that they desire a PARALLEL convergent structure, appropriate shimming is done to ensure that the convergence node is a
        /// step.
        /// </summary>
        /// <param name="inbound">The inbound linkables.</param>
        /// <param name="outbound">The outbound linkable.</param>
        public void BindParallelConvergent(PfcNodeList inbound, IPfcNode outbound)
        {
            BindParallelConvergent(inbound.ToArray(), outbound);
        }

        #endregion Bind Parallel Convergent

        #region Bind Parallel Divergent

        /// <summary>
        /// Binds the inbound node to the outbound nodes through a parallel divergent link structure. Since the caller has indicated
        /// that they desire a PARALLEL divergent structure, appropriate shimming is done to ensure that the divergence node is a
        /// transition.
        /// </summary>
        /// <param name="inbound">The inbound linkable.</param>
        /// <param name="outbounds">The outbound linkables.</param>
        public void BindParallelDivergent(IPfcNode inbound, IPfcNode[] outbounds)
        {
            // The convergence node in a parallel divergence is a transition. So if inbound isn't a transition, we shim with one.
            if (!inbound.ElementType.Equals(PfcElementType.Transition))
            {
                IPfcTransitionNode shimNode = CreateTransition();
                Bind(inbound, shimNode);
                inbound = shimNode;
            }

            foreach (IPfcNode outbound in outbounds)
            {
                Bind(inbound, outbound);
            }
        }

        /// <summary>
        /// Binds the inbound node to the outbound nodes through a parallel divergent link structure. Since the caller has indicated
        /// that they desire a PARALLEL divergent structure, appropriate shimming is done to ensure that the divergence node is a
        /// transition.
        /// </summary>
        /// <param name="inbound">The inbound linkable.</param>
        /// <param name="outbound">The outbound linkables.</param>
        public void BindParallelDivergent(IPfcNode inbound, PfcNodeList outbound)
        {
            BindParallelDivergent(inbound, outbound.ToArray());
        }

        #endregion Bind Parallel Divergent

        #region Bind Series Divergent

        /// <summary>
        /// Binds the inbound node to the outbound nodes through a series divergent link structure. Since the caller has indicated
        /// that they desire a SERIES divergent structure, appropriate shimming is done to ensure that the divergence node is a
        /// step.
        /// </summary>
        /// <param name="inbound">The inbound linkable.</param>
        /// <param name="outbounds">The outbound linkables.</param>
        public void BindSeriesDivergent(IPfcNode inbound, IPfcNode[] outbounds)
        {
            // The divergence node in a series divergence is a step. So if inbound isn't a step, we shim with one.
            if (!inbound.ElementType.Equals(PfcElementType.Step))
            {
                IPfcStepNode shimNode = CreateStep();
                shimNode.IsNullNode = true;
                Bind(inbound, shimNode);
                inbound = shimNode;
            }

            foreach (IPfcNode outbound in outbounds)
            {
                Bind(inbound, outbound);
            }
        }

        /// <summary>
        /// Binds the inbound node to the outbound nodes through a series divergent link structure. Since the caller has indicated
        /// that they desire a SERIES divergent structure, appropriate shimming is done to ensure that the divergence node is a
        /// step.
        /// </summary>
        /// <param name="inbound">The inbound linkable.</param>
        /// <param name="outbound">The outbound linkables.</param>
        public void BindSeriesDivergent(IPfcNode inbound, PfcNodeList outbound)
        {
            BindSeriesDivergent(inbound, outbound.ToArray());
        }

        #endregion Bind Series Divergent

        #endregion Complex Binding

        #region Bind through Synchronizer

        /// <summary>
        /// Binds the inbound elements to the outbound elements through a synchronizer construct. All elements in
        /// both arrays must be the same type (either all Steps or all Transitions), and null or empty arrays are
        /// illegal.
        /// </summary>
        /// <param name="inbound">The inbound elements.</param>
        /// <param name="outbound">The outbound elements.</param>
        public void Synchronize(IPfcNode[] inbound, IPfcNode[] outbound)
        {

            Type inboundType = ArrayType(inbound);
            Type outboundType = ArrayType(outbound);
            if (inboundType == null || outboundType == null)
            {
                throw new ApplicationException("The array of inbound and the array of outbound ILinkables must each consist of the same type elements (either all Steps or all Transitions.)");
            }

            if (!inboundType.Equals(outboundType))
            {
                throw new ApplicationException("The inbound and outbound arrays of ILinkables must both contain the same type of elements (either all Steps or all Transitions.)");
            }

            IPfcNode intermediary = CreateTransition();

            // Now create the synchronization construct.
            BindParallelConvergent(inbound, intermediary);
            BindParallelDivergent(intermediary, outbound);

        }

        /// <summary>
        /// Binds the inbound elements to the outbound elements through a synchronizer construct. Empty collections are illegal.
        /// </summary>
        /// <param name="inbound">The inbound elements.</param>
        /// <param name="outbound">The outbound elements.</param>
        public void Synchronize(PfcNodeList inbound, PfcNodeList outbound)
        {
            Synchronize(inbound.ToArray(), outbound.ToArray()); // TODO: Change from delegation to a better implementation.
        }

        /// <summary>
        /// Returns the type of the elements in this array. If they do not all match, returns null.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <returns>The type of the elements in this array. If they do not all match, returns null.</returns>
        private Type ArrayType(IPfcNode[] array)
        {
            Debug.Assert(array != null && array.Length > 0, "IPfcLinkable[] being bound must be non-null and non-zero length.");

            Type arrayType = array[0].GetType();
            foreach (IPfcNode linkable in array)
            {
                if (!linkable.GetType().Equals(arrayType))
                {
                    return null;
                }
            }
            return arrayType;
        }

        #endregion Bind through Synchronizer

        #endregion Binding

        public IComparer<IPfcLinkElement> LinkComparer
        {
            get
            {
                return m_linkComparer;
            }
            set
            {
                m_linkComparer = value;
            }
        }

        /// <summary>
        /// Makes the link primary, setting its priority to int.MaxValue.
        /// </summary>
        /// <param name="link">The link.</param>
        public void MakeLinkPrimary(IPfcLinkElement link)
        {
            link.Priority = int.MaxValue;
        }

        #region Node Sorting Suppression
        // Node Sorting Suppression
        // NSS is necessary because while doing intermediate graph manipulation, node sorting
        // is not helpful (i.e. it only matters after the last manipulation), and it consumes
        // a boatload of time. Might be O(n!).  :-(
        private bool _doNodeSorting = true;
        private int _nodeSortingSemaphore = 0;
        /// <summary>
        /// This is a performance enhancer - when making internal changes (i.e. changes that are a
        /// part of a larger process such as flattening a Pfc hierarchy), there is no point to doing
        /// node sorting on the entire graph, each time. So, prior to the start of the wholesale
        /// changes, suspend node sorting, and then resume once the changes are complete. Resuming
        /// also results in a call to UpdateStructure(...).
        /// </summary>
        public void ResumeNodeSorting()
        {
            if (--_nodeSortingSemaphore == 0)
            {
                _doNodeSorting = true;
                UpdateStructure();
                EliminateDupeNames();
            }
        }

        /// <summary>
        /// This is a performance enhancer - when making internal changes (i.e. changes that are a
        /// part of a larger process such as flattening a Pfc hierarchy), there is no point to doing
        /// node sorting on the entire graph, each time. So, prior to the start of the wholesale
        /// changes, suspend node sorting, and then resume once the changes are complete. Resuming
        /// also results in a call to UpdateStructure(...).
        /// </summary>
        public void SuspendNodeSorting()
        {
            _nodeSortingSemaphore++;
            _doNodeSorting = false;
        }

        #endregion

        /// <summary>
        /// Updates the structure of the PFC and sorts outbound links per their priority then their textual names, then
        /// their guids. Next, does a depth-first traversal to identify loopback links, then does a breadth-first traversal,
        /// assigning nodes a sequence number. Finally sorts node lists per their sequence numbers. Loop breaking then can 
        /// occur between the node with the higher sequence number and the *following* node with the lower number. This way,
        /// loop-break always occurs at the intuitively-correct place.
        /// </summary>
        public void UpdateStructure(bool breadthFirstOrdinalNumbers = true)
        {

            #region Remove any orphan steps, links or transitions.
            PruneOrphans<IPfcStepNode>(_stepNodeList);
            PruneOrphans<IPfcLinkElement>(_linkNodeList);
            PruneOrphans<IPfcTransitionNode>(_transitionNodeList);
            PruneOrphans<IPfcNode>(_nodeList);
            #endregion

            #region If we want to do NodeSorting, then ...
            if (_doNodeSorting)
            {
                #region Clear out all the PFC-structure-related data.
                _participantDirectory.Refresh(this);

                foreach (IPfcNode node in Nodes)
                {
                    node.UpdateStructure();
                    node.GraphOrdinal = -1;
                    ((PfcNode)node).ScratchPad = null;
                    ((PfcNode)node).NodeColor = NodeColor.White;
                }
                #endregion

                #region Ascertain the startSteps
                List<IPfcStepNode> startSteps = null;
                if (StartStepResolver != null)
                {
                    startSteps = new List<IPfcStepNode>();
                    IPfcStepNode startStep = StartStepResolver(this);
                    if (startStep != null)
                    {
                        startSteps.Add(startStep);
                    }
                }
                else
                {
                    startSteps = GetStartSteps();
                }
                #endregion

                if (startSteps.Count == 1)
                {

                    #region Clear out all the PFC-structure-related data.

                    Links.ForEach(n => n.IsLoopback = false);

                    MarkLoopbackLinks(startSteps[0], new Stack<IPfcNode>());
                    #endregion

                    // Now, do a breadth-first completion-front traversal through the chart.
                    int ordinal = 0;

                    if (breadthFirstOrdinalNumbers)
                    { // Do a breadth-first-traversal-order sequencing of node numbers. 
                        Queue<IPfcNode> queue = new Queue<IPfcNode>();
                        queue.Enqueue(startSteps[0]);

                        while (queue.Count > 0)
                        {
                            IPfcNode _node = queue.Dequeue();
                            bool readyToAcceptOrdinal = true;
                            foreach (IPfcLinkElement link in _node.Predecessors)
                            {
                                if (!link.IsLoopback && link.Predecessor.GraphOrdinal == -1)
                                {
                                    readyToAcceptOrdinal = false;
                                }
                            }
                            if (readyToAcceptOrdinal)
                            {
                                if (_node.GraphOrdinal == -1)
                                {
                                    _node.GraphOrdinal = (ordinal++);
                                    foreach (IPfcLinkElement link in _node.Successors)
                                    {
                                        if (!link.IsLoopback)
                                        {
                                            queue.Enqueue(link.Successor);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // 
                        Stack<IPfcNode> stack = new Stack<IPfcNode>();
                        stack.Push(startSteps[0]);

                        while (stack.Count > 0)
                        {
                            IPfcNode node = stack.Pop();
                            bool readyToAcceptOrdinal = true;
                            foreach (IPfcLinkElement link in node.Predecessors)
                            {
                                if (!link.IsLoopback && link.Predecessor.GraphOrdinal == -1)
                                {
                                    readyToAcceptOrdinal = false;
                                }
                            }
                            if (readyToAcceptOrdinal)
                            {
                                if (node.GraphOrdinal == -1)
                                {
                                    node.GraphOrdinal = (ordinal++);
                                    foreach (IPfcLinkElement link in node.Successors)
                                    {
                                        if (!link.IsLoopback)
                                        {
                                            stack.Push(link.Successor);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    // PFC has more than one start step. Problem. We'll leave it as-is, and let the Validator discover it.
                    // throw new PFCValidityException(this, string.Format("PFC {0} has more than one start step. This is a structural error.",Name));
                    // Console.WriteLine("The SFC being processed did not have a single start step. This is an error.");
                    Debugger.Break();
                }

                _nodeList.Sort(new PfcNode.NodeComparer());
                _stepNodeList.Sort(new PfcStep.StepComparer());
                _linkNodeList.Sort(new PfcLink.LinkComparer());
                _transitionNodeList.Sort(new PfcTransition.TransitionComparer());
            }
            #endregion

            ExecutionEngine = null;

        }

        /// <summary>
        /// Applies the naming cosmetics appropriate for the type of recipe being generated. This is currently
        /// hard-coded, and performs naming of transitions to T_001, T_002, ... T_00n, and null steps to 
        /// NULL_UP:0, NULL_UP:1, ... NULL_UP:n.
        /// </summary>
        public void ApplyNamingCosmetics()
        {
            // This is target-system-specific. Eventually, make this an externally-assigned delegate.

            List<IPfcStepNode> myNullSteps = new List<IPfcStepNode>();
            foreach (IPfcStepNode theStep in Steps)
            {
                if ((theStep.IsNullNode && theStep.PredecessorNodes.Count > 0 && theStep.SuccessorNodes.Count > 0)
                || (theStep.Name.StartsWith("NULL_UP:", StringComparison.Ordinal)))
                {
                    theStep.SetName(Guid.NewGuid().ToString());
                    myNullSteps.Add(theStep);
                }
            }

            int i = 1;
            myNullSteps.Sort(new StepsByGuidSorter());
            foreach (IPfcStepNode step in myNullSteps)
            {
                step.SetName("NULL_UP:" + i);
                i++;
            }

            // We do this so that it does not blow up if one or more Transition Names is in the ParticipantDirectory.
            List<IPfcTransitionNode> myTransitions = new List<IPfcTransitionNode>();
            foreach (IPfcTransitionNode trans in Transitions)
            {
                if (ElementFactory.IsCanonicallyNamed(trans))
                {
                    trans.SetName(Guid.NewGuid().ToString());
                    myTransitions.Add(trans);
                }
            }

            // Once all have been given over to temporary names, we can rename them to the ones we want them to be.
            i = 1;
            myTransitions.Sort(new TransitionsByGuidSorter());
            foreach (IPfcTransitionNode trans in myTransitions)
            {
                trans.SetName(string.Format("T_{0:d3}", (i++)));
            }
        }

        #region Public Properties

        /// <summary>
        /// Gets the root NodeGroup of this SFC.
        /// </summary>
        /// <value>The root.</value>
        public IPfcStepNode Parent
        {
            get
            {
                return _parent;
            }
            set
            {
                _parent = value;
            }
        }

        /// <summary>
        /// Gets the source PFC, if any, from which this PFC was cloned.
        /// </summary>
        /// <value>The source.</value>
        public IProcedureFunctionChart Source
        {
            get
            {
                return _source;
            }
        }

        /// <summary>
        /// A directory of participants in and below this Pfc, used in creation of expressions.
        /// </summary>
        public ParticipantDirectory ParticipantDirectory
        {
            get
            {
                if (_parent != null)
                {
                    return _parent.Parent.ParticipantDirectory;
                }
                else
                {
                    if (_participantDirectory == null)
                    {
                        _participantDirectory = new ParticipantDirectory();
                    }
                    return _participantDirectory;
                }
            }
        }

        public bool DoPruneOrphans
        {
            get; set;
        }

        #endregion Public Properties

        #region Element Lists

        /// <summary>
        /// Removes all elements that are not connected, from the list.
        /// </summary>
        /// <typeparam name="T">A list of the provided type elements, which must implement IPfcElement.</typeparam>
        /// <param name="list">The list itself.</param>
        private void PruneOrphans<T>(List<T> list) where T : IPfcElement
        {
            if (DoPruneOrphans)
            {
                list.RemoveAll(delegate (T t)
                {
                    return !t.IsConnected();
                });
            }
        }



        /// <summary>
        /// Gets the steps under management of this Procedure Function Chart. This is a
        /// read-only list.
        /// </summary>
        /// <value>The steps.</value>
        public PfcStepNodeList Steps
        {
            get
            {
                return _stepNodeList;
            }
        }

        /// <summary>
        /// Gets the transitions under management of this Procedure Function Chart. This is a
        /// read-only list.
        /// </summary>
        /// <value>The transitions.</value>
        public PfcTransitionNodeList Transitions
        {
            get
            {
                return _transitionNodeList;
            }
        }

        /// <summary>
        /// Gets all of the nodes (steps and transitions)under management of this Procedure Function Chart. This is a
        /// read-only collection.
        /// </summary>
        /// <value>The nodes.</value>
        public PfcNodeList Nodes
        {
            get
            {
                return _nodeList;
            }
        }

        /// <summary>
        /// Gets all of the elements that are contained in or under this Pfc, to a depth
        /// specified by the 'depth' parameter, and that pass the 'filter' criteria.
        /// </summary>
        /// <param name="depth">The depth to which retrieval is to be done.</param>
        /// <param name="filter">The filter predicate that dictates which elements are acceptable.</param>
        /// <param name="children">The children, treated as a return value.</param>
        public void GetChildren(int depth, Predicate<IPfcElement> filter, ref List<IPfcElement> children)
        {
            foreach (IPfcElement element in _nodeList)
            {
                if (filter(element))
                {
                    children.Add(element);
                    if (element.ElementType.Equals(PfcElementType.Step))
                    {
                        ((IPfcStepNode)element).GetChildren(depth - 1, filter, ref children);
                    }
                }
            }
        }

        /// <summary>
        /// Gets all of the edges (links) under management of this Procedure Function Chart. This is a
        /// read-only collection.
        /// </summary>
        /// <value>The edges (links).</value>
        public PfcLinkElementList Edges
        {
            get
            {
                PruneOrphans(_linkNodeList);
                return _linkNodeList;
            }
        }

        /// <summary>
        /// Gets all of the edges (links) under management of this Procedure Function Chart. This is a
        /// read-only collection.
        /// </summary>
        /// <value>The edges (links).</value>
        public PfcLinkElementList Links
        {
            get
            {
                PruneOrphans(_linkNodeList);
                return _linkNodeList;
            }
        }

        /// <summary>
        /// Gets the elements contained directly in this Pfc.
        /// </summary>
        /// <value>The elements.</value>
        public List<IPfcElement> Elements
        {
            get
            {
                List<IPfcElement> elements = new List<IPfcElement>();
                foreach (IPfcNode node in Nodes)
                {
                    elements.Add(node);
                }
                foreach (IPfcLinkElement link in Links)
                {
                    elements.Add(link);
                }
                return elements;
            }
        }

        #endregion Element Lists

        #region Deletion Support
        /// <summary>
        /// Deletes the specified node and its pair (preceding Step if it is a transition,
        /// succeeding transition if it is a step).
        /// <list type="bullet">
        /// <item>If either member of the pair being deleted
        /// has more than one predecessor and one successor, the delete attempt will fail - these
        /// other paths need to be deleted themselves first.</item>
        /// <item>If neither node has multiple inputs
        /// or outputs, then they are both deleted, and a link is added from the transition
        /// preceding the deleted step to the step following the deleted transition.</item>
        /// <item>If the node to be deleted is not connected to anything on either end, then the node is
        /// simply removed from Pfc data structures.</item>
        /// </list> 
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>True if the deletion was successful, false if it was not.</returns>
        public bool Delete(IPfcNode node)
        {

            if (node.PredecessorNodes.Count == 1 && node.SuccessorNodes.Count == 1)
            {
                IPfcNode partner = node.ElementType.Equals(PfcElementType.Step) ? node.SuccessorNodes[0] : node.PredecessorNodes[0];
                if (partner.Predecessors.Count == 1 || partner.Successors.Count == 1)
                {
                    IPfcNode precedent, successor;
                    if (node.PredecessorNodes[0] == partner)
                    {
                        precedent = partner.PredecessorNodes[0];
                        successor = node.SuccessorNodes[0];
                        Unbind(precedent, partner);
                        Unbind(partner, node);
                        Unbind(node, successor);
                    }
                    else
                    {
                        precedent = node.PredecessorNodes[0];
                        successor = partner.SuccessorNodes[0];
                        Unbind(precedent, node);
                        Unbind(node, partner);
                        Unbind(partner, successor);
                    }
                    Bind(precedent, successor);
                    PruneOrphans(_stepNodeList);
                    PruneOrphans(_nodeList);
                    PruneOrphans(_transitionNodeList);
                    return true;
                }
            }
            else if (node.PredecessorNodes.Count == 0 || node.SuccessorNodes.Count == 0)
            {
                PruneOrphans(_stepNodeList);
                PruneOrphans(_nodeList);
                PruneOrphans(_transitionNodeList);
            }
            return false;
        }

        /// <summary>
        /// Gets the nodes on all paths that proceed from the 'from' node to the 'to' node, through the 'through' node.
        /// </summary>
        /// <param name="from">The first node on the sought-for path.</param>
        /// <param name="through">A node in the middle that must be on the sought-for path.</param>
        /// <param name="to">The last node on the sought-for path.</param>
        /// <returns></returns>
        public List<IPfcNode> GetNodesOnPath(IPfcNode from, IPfcNode through, IPfcNode to)
        {
            List<IPfcNode> pathNodes = new List<IPfcNode>();
            LookBackwardForNodesOnPathStartingAt(from, through, ref pathNodes);
            LookForwardForNodesOnPathEndingAt(to, through, ref pathNodes);

            return pathNodes;
        }

        /// <summary>
        /// Marks the loopback links. Prerequisites - all nodes' colors are White, all links are
        /// not loopbacks.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="path">The path.</param>
        private void MarkLoopbackLinks(IPfcNode node, Stack<IPfcNode> path)
        {

            if (((PfcNode)node).NodeColor == NodeColor.Black)
                return;
            path?.Push(node);

            ((PfcNode)node).NodeColor = NodeColor.Gray;
            foreach (IPfcLinkElement link in node.Successors)
            {
                IPfcNode nextNode = link.Successor;
                if (((PfcNode)nextNode).NodeColor == NodeColor.Gray)
                {
                    link.IsLoopback = true;
                    if (_diagnostics)
                    {
                        Console.WriteLine("Loopback path to " + nextNode.Name + ":");
                        foreach (IPfcNode pathNode in path)
                        {
                            Console.WriteLine("\t{0}{1}", pathNode.Name, (pathNode.Equals(nextNode) ? "<--------------" : ""));
                        }
                    }
                }
                else
                {
                    MarkLoopbackLinks(nextNode, path);
                }
            }

            path?.Pop();
            ((PfcNode)node).NodeColor = NodeColor.Black;

        }

        /// <summary>
        /// Looks forward from node 'node' for a path ending at 'finish', and returns a list of nodes that are on all such paths.
        /// </summary>
        /// <param name="finish">The finish.</param>
        /// <param name="node">The node.</param>
        /// <param name="deletees">The nodes that were on such paths.</param>
        /// <returns>True if any paths were found</returns>
        public bool LookForwardForNodesOnPathEndingAt(IPfcNode finish, IPfcNode node, ref List<IPfcNode> deletees)
        {
            Stack<IPfcNode> visited = new Stack<IPfcNode>();

            #region Detach and save off any attached userdata, since we're going to use this field as a marker.
            Hashtable holdUserData = new Hashtable();
            foreach (IPfcNode n in node.Parent.Nodes)
            {
                if (n.UserData != null)
                {
                    holdUserData.Add(n, n.UserData);
                    n.UserData = null;
                }
            }

            #endregion

            bool result = lookForwardForNodesOnPathEndingAt(finish, node, ref deletees, ref visited);

            #region Reattach the previously attached userdata.
            foreach (IPfcNode n in holdUserData)
            {
                n.UserData = holdUserData[n];
            }
            #endregion

            return result;
        }

        /// <summary>
        /// Looks forward from node 'node' for a path ending at 'finish', and returns a list of nodes that are on all such paths.
        /// </summary>
        /// <param name="finish">The finish.</param>
        /// <param name="node">The node.</param>
        /// <param name="deletees">The nodes that were on such paths.</param>
        /// <param name="visited">The visited nodes stack.</param>
        /// <returns>True if any paths were found</returns>
        private bool lookForwardForNodesOnPathEndingAt(IPfcNode finish, IPfcNode node, ref List<IPfcNode> deletees, ref Stack<IPfcNode> visited)
        {
            bool retval = false;
            if (node.Equals(finish))
            {
                retval = true;
            }
            else
            {

                if (visited.Count > _pathLengthCap)
                {
                    return false;
                }
                if (node.UserData == null/* This is only true if it is not in the 'visited' stack/path */)
                {
                    node.UserData = this; // Marker to show it is in the 'visited' stack/path.
                    visited.Push(node);

                    foreach (IPfcLinkElement link in node.Successors)
                    {
                        IPfcNode nextNode = link.Successor;
                        if (nextNode != null && lookForwardForNodesOnPathEndingAt(finish, nextNode, ref deletees, ref visited))
                        {
                            retval = true;
                            //if (!deletees.Contains(node)) {
                            deletees.Add(node);
                            //}
                        }
                    }

                    visited.Pop();
                    node.UserData = null; // Marker to show it is no longer the 'visited' stack/path.
                }
            }

            return retval;
        }

        /// <summary>
        /// Looks backward from node 'node' for a path ending at 'start', and returns a list of nodes that are on all such paths.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="node">The node.</param>
        /// <param name="deletees">The nodes that were on such paths.</param>
        /// <returns>True if any paths were found</returns>
        public bool LookBackwardForNodesOnPathStartingAt(IPfcNode start, IPfcNode node, ref List<IPfcNode> deletees)
        {
            Stack<IPfcNode> visited = new Stack<IPfcNode>();
            return lookBackwardForNodesOnPathStartingAt(start, node, ref deletees, ref visited);
        }

        /// <summary>
        /// Looks backward from node 'node' for a path ending at 'start', and returns a list of nodes that are on all such paths.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="node">The node.</param>
        /// <param name="deletees">The nodes that were on such paths.</param>
        /// <param name="visited">The visited nodes.</param>
        /// <returns>True if any paths were found</returns>
        private bool lookBackwardForNodesOnPathStartingAt(IPfcNode start, IPfcNode node, ref List<IPfcNode> deletees, ref Stack<IPfcNode> visited)
        {
            bool retval = false;
            if (node.Equals(start))
            {
                retval = true;
            }
            else
            {
                if (!visited.Contains(node))
                {
                    visited.Push(node);

                    foreach (IPfcNode nextNode in node.PredecessorNodes)
                    {
                        if (lookBackwardForNodesOnPathStartingAt(start, nextNode, ref deletees, ref visited))
                        {
                            retval = true;
                            if (!deletees.Contains(node))
                            {
                                deletees.Add(node);
                            }
                        }
                    }

                    visited.Pop();
                }
            }

            return retval;
        }

        #endregion Deletion Support

        #region Implementation of IModelObject

        /// <summary>
        /// The user-friendly name for this Procedure Function Chart. Typically not required to be unique.
        /// </summary>
        /// <value>The Name.</value>
        public string Name
        {
            [DebuggerStepThrough]
            get
            {
                return _name;
            }
        }
        /// <summary>
        /// The Guid for this Procedure Function Chart. Typically required to be unique.
        /// </summary>
        /// <value>The Guid.</value>
        public Guid Guid
        {
            [DebuggerStepThrough]
            get
            {
                return _guid;
            }
        }
        /// <summary>
        /// The model that owns this Procedure Function Chart, or from which it gets time, etc. data.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model
        {
            [DebuggerStepThrough]
            get
            {
                return _model;
            }
        }
        /// <summary>
        /// The description for this Procedure Function Chart. Typically used for human-readable representations.
        /// </summary>
        /// <value>The Procedure Function Chart's description.</value>
        public string Description => (_description ?? ("No description for " + _name));

        /// <summary>
        /// Initializes the fields that feed the properties of this IModelObject identity.
        /// </summary>
        /// <param name="model">The IModelObject's new model value.</param>
        /// <param name="name">The IModelObject's new name value.</param>
        /// <param name="description">The IModelObject's new description value.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid)
        {
            IMOHelper.Initialize(ref _model, model, ref _name, name, ref _description, description, ref _guid, guid);
        }



        #endregion

        #region IXmlSerializable Members

        /// <summary>
        /// Gets the PFC schema.
        /// </summary>
        /// <returns>The PFC schema.</returns>
        public static XmlSchemaComplexType GetPfcSchema(XmlSchemaSet xs)
        {
            XmlSchema xmls = XmlSchema.Read(new XmlTextReader(_schema, XmlNodeType.Document, null), null);

            xs.Add(xmls);

            string tns = "http://tempuri.org/ProcedureFunctionChart.xsd";
            XmlQualifiedName name = new XmlQualifiedName("ProcedureFunctionChart", tns);
            XmlSchemaComplexType schemaType = (XmlSchemaComplexType)xmls.SchemaTypes[name];

            return schemaType;
        }

        /// <summary>
        /// This property is reserved, apply the <see cref="T:System.Xml.Serialization.XmlSchemaProviderAttribute"></see> to the class instead.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Xml.Schema.XmlSchema"></see> that describes the XML representation of the object that is produced by the <see cref="M:System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter)"></see> method and consumed by the <see cref="M:System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader)"></see> method.
        /// </returns>
        public XmlSchema GetSchema()
        {
            return null;
        }

        public void WriteXml(XmlWriter writer)
        {

            writer.WriteStartElement("ProcedureFunctionChart");
            writer.WriteElementString(_serializerVersionTag, XMLCONVERT.ToString(_currentSerializerVersion));
            WriteParticipantDirectory(writer);
            writer.WriteElementString("Name", _name);
            writer.WriteElementString("Description", _description);
            writer.WriteElementString("Guid", _guid.ToString());
            writer.WriteElementString("ElementFactoryType", _sfcElementFactory.GetType().FullName);

            foreach (IPfcStepNode step in _stepNodeList)
            {
                WriteStep(step, writer);
            }

            foreach (IPfcTransitionNode transition in _transitionNodeList)
            {
                WriteTransition(transition, writer);
            }

            foreach (IPfcLinkElement link in _linkNodeList)
            {
                WriteLink(link, writer);
            }

            writer.WriteEndElement();

            writer.Flush();
        }

        /// <summary>
        /// Generates an object from its XML representation.
        /// </summary>
        /// <param name="reader">The <see cref="T:System.Xml.XmlReader"></see> stream from which the object is deserialized.</param>
        public void ReadXml(XmlReader reader)
        {

            reader.ReadToFollowing("ProcedureFunctionChart");

            reader.ReadToFollowing(_serializerVersionTag);
            _currentlyDeserializingVersion = XMLCONVERT.ToDouble(reader.ReadString());

            if (_currentlyDeserializingVersion > 0.26)
            {
                reader.ReadToFollowing("ParticipantDirectory");
                ReadParticipantDirectory(reader);
            }

            reader.ReadToFollowing("Name");
            string name = reader.ReadString();

            reader.ReadToFollowing("Description");
            string description = reader.ReadString();

            reader.ReadToFollowing("Guid");
            Guid guid = new Guid(reader.ReadString());

            reader.ReadToFollowing("ElementFactoryType");
            string elementFactoryType = reader.ReadString();
            if (elementFactoryType.Contains(_libWas, StringComparison.Ordinal))
            {
                elementFactoryType = elementFactoryType.Replace(_libWas, _libIs, StringComparison.Ordinal);
            }

            //System.Reflection.ConstructorInfo[] cia = Type.GetType(elementFactoryType).GetConstructors();
            _sfcElementFactory = (IPfcElementFactory)Type.GetType(elementFactoryType).GetConstructor(new Type[] { typeof(IProcedureFunctionChart) }).Invoke(new object[] { this });

            IModel tmp = _model;
            _model = null;
            _guid = Guid.Empty; // ...so that the read-from-xml Guid is set into the new PFC.
            InitializeIdentity(tmp, name, description, guid);

            do
            {
                reader.Read();
                if (reader.IsStartElement())
                {
                    switch (reader.Name)
                    {
                        case "Step":
                            LoadStep(reader);
                            break;
                        case "Transition":
                            LoadTransition(reader);
                            break;
                        case "Link":
                            LoadLink(reader);
                            break;
                        default:
                            break;
                    }
                }
            } while (!(reader.Name.Equals("ProcedureFunctionChart", StringComparison.Ordinal) && !reader.IsStartElement()));
            reader.Read();
            _sfcElementFactory.OnPfcLoadCompleted(this);
            foreach (IPfcTransitionNode trans in _transitionNodeList)
            {
                trans.Expression.ResolveUnknowns(); // Forces resolution of unknown guid keys, now that the deserialization is complete.
            }
        }

        #region XmlSerialization Support Methods

        private static readonly string _libWas = "Highpoint.Sage.Graphs.PFC.";
        private static readonly string _libIs = "Highpoint.Sage.Graphs.PFC.";

        /// <summary>
        /// Creates an XML string representation of this Pfc.
        /// </summary>
        /// <returns>The newly-created Xml string.</returns>
        public string ToXmlString()
        {

            #region Create an XmlWriter
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            XmlTextWriter writer = new XmlTextWriter(sw);
            writer.Formatting = Formatting.Indented;

            #endregion Create an XmlWriter

            writer.WriteStartDocument();
            WriteXml(writer);
            writer.WriteEndDocument();
            writer.Flush();
            writer.Close();

            return sb.ToString();

        }

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Xml.XmlWriter"></see> stream to which the object is serialized.</param>
        private void WriteParticipantDirectory(XmlWriter writer)
        {
            writer.WriteStartElement("ParticipantDirectory");

            // Steps are intentionally omitted, as they are stored in the PFC itself.
            List<IPfcElement> steps = new List<IPfcElement>();
            GetChildren(int.MaxValue, PfcPredicates.StepsOnly, ref steps);
            List<Guid> guids = new List<Guid>();
            steps.ForEach(delegate (IPfcElement element)
            {
                guids.Add(element.Guid);
            });

            foreach (ExpressionElement ee in _participantDirectory)
            {
                if (!guids.Contains(ee.Guid))
                {
                    writer.WriteStartElement("ExpressionElement");
                    writer.WriteElementString("Name", ee.Name);
                    writer.WriteElementString("Type", ee.GetType().FullName);
                    writer.WriteElementString("Guid", XMLCONVERT.ToString(ee.Guid));
                    writer.WriteEndElement();
                }
            }
            writer.WriteEndElement();
        }

        private void WriteStep(IPfcStepNode step, XmlWriter writer)
        {
            writer.WriteStartElement("Step");
            writer.WriteElementString("Name", step.Name);
            writer.WriteElementString("Description", step.Description);
            writer.WriteElementString("Guid", step.Guid.ToString());
            writer.WriteElementString("Ordinal", step.GraphOrdinal.ToString());

            writer.WriteStartElement("PfcUnit");
            writer.WriteElementString("UnitName", step.UnitInfo != null ? step.UnitInfo.Name : "UnknownUnit");
            writer.WriteElementString("SequenceNumber", step.UnitInfo != null ? XMLCONVERT.ToString(step.UnitInfo.SequenceNumber) : "-1");
            writer.WriteEndElement();

            writer.WriteElementString("IsNull", XMLCONVERT.ToString(step.IsNullNode));
            writer.WriteStartElement("Actions");
            foreach (string key in step.Actions.Keys)
            {
                writer.WriteElementString("Key", key);
                step.Actions[key].WriteXml(writer);
            }
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        private void WriteTransition(IPfcTransitionNode transition, XmlWriter writer)
        {
            writer.WriteStartElement("Transition");
            writer.WriteElementString("Name", transition.Name);
            writer.WriteElementString("Description", transition.Description);
            writer.WriteElementString("Guid", transition.Guid.ToString());
            writer.WriteElementString("Ordinal", transition.GraphOrdinal.ToString());
            writer.WriteElementString("IsNull", XMLCONVERT.ToString(transition.IsNullNode));
            writer.WriteElementString("Expression", transition.Expression.ToString(ExpressionType.Hostile, transition));
            writer.WriteEndElement();
        }

        private void WriteLink(IPfcLinkElement link, XmlWriter writer)
        {
            writer.WriteStartElement("Link");
            writer.WriteElementString("Name", link.Name);
            writer.WriteElementString("Description", link.Description);
            writer.WriteElementString("Guid", link.Guid.ToString());
            writer.WriteElementString("Predecessor", link.Predecessor.Guid.ToString());
            writer.WriteElementString("Successor", link.Successor.Guid.ToString());
            if (link.Priority > 0)
            {
                writer.WriteElementString("Priority", link.Priority.ToString());
            }
            writer.WriteEndElement();
        }

        /// <summary>
        /// Creates a ProcedureFunctionChart with repeatable GUID generation characteristics from a properly-schemed XML string.
        /// </summary>
        /// <param name="xmlString">The XML string.</param>
        /// <param name="seed">The seed for the Guid Generator.</param>
        /// <param name="mask">The mask for the Guid Generator.</param>
        /// <param name="rotate">The rotation bit count for the Guid Generator.</param>
        /// <returns></returns>
        public static ProcedureFunctionChart FromXmlString(string xmlString, Guid seed, Guid mask, int rotate)
        {

            XmlReader reader = XmlReader.Create(new StringReader(xmlString));

            GuidGenerator guidGen = new GuidGenerator(seed, mask, rotate);
            PfcElementFactory pfcef = new PfcElementFactory(guidGen);

            ProcedureFunctionChart pfc = new ProcedureFunctionChart(null, null, null, guidGen.Next(), pfcef);

            try
            {
                pfc.ReadXml(reader);
            }
            catch (FormatException fe)
            {
                Console.WriteLine("FormatException at \"" + reader.ReadOuterXml() + "\" element. Probably a version mismatch.");
                throw fe;
            }

            return pfc;

        }

        /// <summary>
        /// Creates a ProcedureFunctionChart from a properly-schemed XML string.
        /// </summary>
        /// <param name="xmlString">The XML string.</param>
        /// <returns></returns>
        public static ProcedureFunctionChart FromXmlString(string xmlString)
        {

            XmlReader reader = XmlReader.Create(new StringReader(xmlString));

            ProcedureFunctionChart pfc = new ProcedureFunctionChart(null, null, null, Guid.Empty);
            try
            {
                pfc.ReadXml(reader);
            }
            catch (FormatException fe)
            {
                Console.WriteLine("FormatException at \"" + reader.ReadOuterXml() + "\" element. Probably a version mismatch.");
                throw fe;
            }
            return pfc;

        }

        private void ReadParticipantDirectory(XmlReader reader)
        {

            reader.Read();
            reader.Read();
            while (!(reader.Name.Equals("ParticipantDirectory", StringComparison.Ordinal) && !reader.IsStartElement()))
            {
                if (reader.Name.Equals("ExpressionElement", StringComparison.Ordinal) && reader.IsStartElement())
                {
                    reader.ReadToFollowing("Name");
                    string name = reader.ReadString();
                    reader.ReadToFollowing("Type");
                    string type = reader.ReadString();
                    reader.ReadToFollowing("Guid");
                    Guid guid = XMLCONVERT.ToGuid(reader.ReadString());

                    Type elementType = Type.GetType(type);
                    if (elementType == null)
                    {
                        // In-place upgrade from old data.
                        type = type.Replace(_libWas, _libIs, StringComparison.Ordinal);
                        elementType = Type.GetType(type);
                    }
                    if (typeof(Macro).IsAssignableFrom(elementType))
                    {
                        _participantDirectory.RegisterMacro(elementType);
                    }

                    if (elementType.Equals(typeof(DualModeString)))
                    {
                        _participantDirectory.RegisterMapping(name, guid);
                    }

                    while (!(reader.Name.Equals("ExpressionElement", StringComparison.Ordinal)))
                        reader.Read();
                }
                else
                {
                    Console.WriteLine(reader.Name + " := " + reader.Value);
                }
                reader.Read();
                if (reader.Name.Equals("ParticipantDirectory", StringComparison.Ordinal))
                    break;
                reader.Read();
            }

        }

        private void LoadStep(XmlReader reader)
        {
            //<Step>
            //<Name>A</Name> 
            //<Description /> 
            //<Guid>40c41631-fca4-4052-a07b-aedf77d70c9b</Guid> 
            //</Step>
            reader.ReadToFollowing("Name");
            string name = reader.ReadString();

            reader.ReadToFollowing("Description");
            string description = reader.ReadString();

            reader.ReadToFollowing("Guid");
            Guid guid = XMLCONVERT.ToGuid(reader.ReadString());

            int ordinal = 0;
            if (_currentlyDeserializingVersion >= 0.29)
            {
                reader.ReadToFollowing("Ordinal");
                ordinal = XMLCONVERT.ToInt32(reader.ReadString());
            }
            IPfcStepNode theStep = CreateStep(name, description, guid);
            theStep.Parent = this;
            theStep.GraphOrdinal = ordinal;

            if (_currentlyDeserializingVersion >= 0.28)
            {
                reader.ReadToFollowing("PfcUnit");
                reader.ReadToFollowing("UnitName");
                string unitName = reader.ReadString();
                reader.ReadToFollowing("SequenceNumber");
                int seqNum;
                if (!int.TryParse(reader.ReadString(), out seqNum))
                {
                    seqNum = -1;
                }

                theStep.UnitInfo = new PfcUnitInfo(unitName, seqNum);

            }

            if (_currentlyDeserializingVersion >= 0.25)
            {
                reader.ReadToFollowing("IsNull");
                string boolVal = reader.ReadString();
                theStep.IsNullNode = XMLCONVERT.ToBoolean(boolVal);
            }

            reader.ReadToFollowing("Actions");
            if (!reader.IsEmptyElement)
            {
                reader.ReadToFollowing("Key");
                string key = reader.ReadString();
                ProcedureFunctionChart action = new ProcedureFunctionChart();
                action.ReadXml(reader);
                theStep.AddAction(key, action);
                action.Parent = theStep;
            }
        }

        private void LoadTransition(XmlReader reader)
        {
            //<Transition>
            //<Name>T_124</Name> 
            //<Description /> 
            //<Guid>46008887-be34-47c5-8a85-70fc211dd2f9</Guid> 
            //</Transition>
            reader.ReadToFollowing("Name");
            string name = reader.ReadString();

            reader.ReadToFollowing("Description");
            string description = reader.ReadString();

            reader.ReadToFollowing("Guid");
            Guid guid = new Guid(reader.ReadString());

            int ordinal = 0;
            if (_currentlyDeserializingVersion >= 0.29)
            {
                reader.ReadToFollowing("Ordinal");
                ordinal = XMLCONVERT.ToInt32(reader.ReadString());
            }

            IPfcTransitionNode trans = CreateTransition(name, description, guid);
            trans.GraphOrdinal = ordinal;

            if (_currentlyDeserializingVersion >= 0.25)
            {
                reader.ReadToFollowing("IsNull");
                trans.IsNullNode = XMLCONVERT.ToBoolean(reader.ReadString());
            }

            if (_currentlyDeserializingVersion >= 0.26)
            {
                reader.ReadToFollowing("Expression");
                string uh = reader.ReadString();
                trans.ExpressionUHValue = uh;
                //Console.WriteLine(uh);
                //Console.WriteLine(trans.ExpressionExpandedValue);
            }


        }

        private void LoadLink(XmlReader reader)
        {

            //<Link>
            //<Name>L_098</Name> 
            //<Description /> 
            //<Guid>e7c26c60-5761-45bc-b649-91c7839bbb1a</Guid> 
            //<Predecessor>9b31db62-88c7-4bb0-ac04-d358b84629d8</Predecessor> 
            //<Successor>e81eed47-b6de-43d1-8672-ffbd7eb2757f</Successor> 
            //(Maybe)<Priority>14</Priority>
            //</Link>

            reader.ReadToFollowing("Name");
            string name = reader.ReadString();

            reader.ReadToFollowing("Description");
            string description = reader.ReadString();

            reader.ReadToFollowing("Guid");
            Guid guid = new Guid(reader.ReadString());

            reader.ReadToFollowing("Predecessor");
            Guid predGuid = new Guid(reader.ReadString());
            IPfcNode predecessor = _nodeList[predGuid];

            reader.ReadToFollowing("Successor");
            Guid succGuid = new Guid(reader.ReadString());
            IPfcNode successor = _nodeList[succGuid];

            int priority = 0;
            reader.Read();
            reader.Read();

            if (reader.Name.Equals("Priority", StringComparison.Ordinal))
            {
                reader.Read();
                int.TryParse(reader.ReadString(), out priority);
                reader.Read();
            }

            if (predecessor == null || successor == null)
            {
                string msg = string.Format("Application loading a link from an XML data store into a " +
                    "ProcedureFunctionChart, was unable to ascertain either the source or the destination of " +
                    "the link. Relevant data are:\r\n\tLinkName = {0}\r\n\tLinkGuid = {1}\r\n\tSrcGuid = {2}\r\n\tDestGuid = {3}",
                    name, guid, predGuid, succGuid);
                //throw new ApplicationException(msg);
                Console.WriteLine(msg);
            }
            else
            {
                IPfcLinkElement link = CreateLink(name, description, guid);
                link.Priority = priority;

                _linkNodeList.Add(link);
                Connect(predecessor, link);
                //Console.Write("Connecting " + predecessor.Name + " to " + link.Name + ", and ");
                Connect(link, successor);
                //Console.WriteLine("Connecting " + link.Name + " to " + successor.Name + ".");
            }
        }

        #endregion XmlSerialization Support Methods



        #endregion

        /// <summary>
        /// Finds the node at the specified path from this location. Currently, works only absolutely from this PFC.
        /// Paths are specified as &quot;parentStepName/childStepName/grandchildStepName&quot;
        /// <para></para>
        /// </summary>
        /// <param name="path">The path (e.g. ParentName/ChildName).</param>
        public IPfcNode FindNode(string path)
        {
            IPfcNode retval = null;
            if (path.Contains("/", StringComparison.Ordinal))
            {
                string[] s = path.Split(new char[] { '/' }, 2);
                IPfcStepNode child = Nodes[s[0]] as IPfcStepNode;
                if (child != null)
                {
                    foreach (IProcedureFunctionChart childAction in child.Actions.Values)
                    {
                        retval = childAction.FindNode(s[1]);
                        if (retval != null)
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                retval = Nodes[path];
            }
            return retval;
        }

        /// <summary>
        /// Finds the first node for which the predicate returns true.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public IPfcNode FindFirst(Predicate<IPfcNode> predicate)
        {
            foreach (IPfcNode node in DepthFirstIterator())
            {
                if (predicate(node))
                {
                    return node;
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieves a depth-first iterator over all nodes in this PFC that satisfy the predicate.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        /// <returns></returns>
        public IEnumerable<IPfcNode> FindAll(Predicate<IPfcNode> predicate)
        {
            foreach (IPfcNode node in DepthFirstIterator())
            {
                if (predicate(node))
                {
                    yield return node;
                }
            }
        }

        /// <summary>
        /// Retrieves a depth-first iterator over all nodes in this PFC.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPfcNode> DepthFirstIterator()
        {
            foreach (IPfcNode node in Nodes)
            {
                IPfcStepNode step = node as IPfcStepNode;
                if (step != null)
                {
                    foreach (ProcedureFunctionChart action in step.Actions.Values)
                    {
                        foreach (IPfcNode node2 in action.DepthFirstIterator())
                        {
                            yield return node2;
                        }
                    }
                }
                yield return node;
            }
        }

        /// <summary>
        /// Combines this PFC and all of its child PFCs (the actions associated with steps)
        /// into one flat PFC with no children. Steps that had children are replaced by their
        /// children, inserted inline into the parents' PFC structure, in place of the parent.
        /// </summary>
        public void Flatten()
        {
            SuspendNodeSorting();
            Flatten(0);
            ResumeNodeSorting();
        }

        private void Flatten(int level)
        {
            List<IPfcStepNode> steps = new List<IPfcStepNode>(Steps);

            foreach (IPfcStepNode step in steps)
            {
                foreach (ProcedureFunctionChart pfc in step.Actions.Values)
                {

                    // First off, we flatten any nodes below us. This means that we're flattening from the bottom, up.
                    pfc.Flatten(level + 1);

                    List<IPfcStepNode> startSteps = pfc.GetStartSteps();
                    List<IPfcNode> finishSteps = pfc.GetFinishTransition().PredecessorNodes;

                    // Now, move nodes (steps and transitions) up to the parent.
                    foreach (IPfcNode node in pfc.Nodes)
                    {
                        node.Parent = this;
                        AddElement(node);
                        if (node.ElementType.Equals(PfcElementType.Step))
                        {
                            ParticipantDirectory.RegisterMapping(node.Name, node.Guid);
                        }
                    }

                    // next, move links up to the parent.
                    foreach (IPfcLinkElement link in pfc.Links)
                    {
                        link.Parent = this;
                        AddElement(link);
                        //ParticipantDirectory.RegisterMapping(link.Name, link.Guid);
                    }

                    // Replace references to macros from below that have equivalent macros at this level,
                    // with references to the macros at this level.
                    foreach (IPfcTransitionNode trans in pfc.Transitions)
                    {
                        List<ExpressionElement> listOfEe = new List<ExpressionElement>(trans.Expression.Elements);
                        foreach (ExpressionElement ee in listOfEe)
                        {
                            if ((ee as Macro) != null)
                            {
                                string targetName = ee.Name.Trim('\'');
                                if (_participantDirectory.Contains(targetName))
                                {
                                    int ndx = trans.Expression.Elements.IndexOf(ee);
                                    trans.Expression.Elements.Remove(ee);
                                    trans.Expression.Elements.Insert(ndx, _participantDirectory[targetName]);
                                }
                            }
                        }
                    }

                    // bind predecessors of the now-replaced step to the start steps from that step's child PFC that
                    // have been moved up and have replaced that step.
                    foreach (IPfcTransitionNode predTrans in step.PredecessorNodes)
                    {
                        foreach (IPfcStepNode startStep in startSteps)
                        {
                            //Console.WriteLine("Binding " + predTrans.Name + " to " + startStep.Name + ".");
                            Bind(predTrans, startStep);
                        }
                    }

                    // bind successors of the now-replaced step to the finish steps from that step's child PFC that
                    // have been moved up and have replaced that step.
                    foreach (IPfcTransitionNode succTrans in step.SuccessorNodes)
                    {
                        foreach (IPfcStepNode finishStep in finishSteps)
                        {
                            //Console.WriteLine("Binding " + succTrans.Name + " to " + finishStep.Name + ".");
                            Bind(finishStep, succTrans);
                        }
                    }

                    // We now need to go into the (child) pfc's Participant directory, and for anything that's
                    // not referring to a step or transition, move it up to the parent's ParticipantDirectory.
                    foreach (ExpressionElement ee in pfc.ParticipantDirectory)
                    {
                        if (!ParticipantDirectory.Contains(ee.Guid) && !ParticipantDirectory.Contains(ee.Name))
                        {
                            ParticipantDirectory.RegisterMapping(ee.Name, ee.Guid);
                        }
                    }

                }

                // If we moved anything up to the level of this step, and therefore replaced this step, then unbind
                // it, and remove it from this PFC.
                if (step.Actions.Count > 0)
                {
                    foreach (IPfcNode pred in step.PredecessorNodes)
                    {
                        //Console.WriteLine("Unbinding " + pred.Name + " from " + step.Name + ".");
                        Unbind(pred, step);
                    }

                    foreach (IPfcNode succ in step.SuccessorNodes)
                    {
                        //Console.WriteLine("Unbinding " + step.Name + " from " + succ.Name + " to finish removing it from " + Name + ".");
                        Unbind(step, succ);
                    }

                    UpdateStructure(); // This will remove the step we've just excised from the PFC, from the PFC Data Structures.
                }
            }

            if (level == 0)
            {
                //Console.WriteLine(PfcDiagnostics.GetStructure(this));
                Reduce();
            }

        }

        private void EliminateDupeNames()
        {

            List<string> names = new List<string>();
            bool needsRenaming = false;

            foreach (IPfcLinkElement link in _linkNodeList)
            {
                if (names.Contains(link.Name))
                {
                    needsRenaming = true;
                    break;
                }
                else
                {
                    names.Add(link.Name);
                }
            }
            if (!needsRenaming)
            {
                names.Clear();
                foreach (IPfcStepNode step in _stepNodeList)
                {
                    if (names.Contains(step.Name))
                    {
                        needsRenaming = true;
                        break;
                    }
                    else
                    {
                        names.Add(step.Name);
                    }
                }
            }
            if (!needsRenaming)
            {
                names.Clear();
                foreach (IPfcTransitionNode trans in _transitionNodeList)
                {
                    if (names.Contains(trans.Name))
                    {
                        needsRenaming = true;
                        break;
                    }
                    else
                    {
                        names.Add(trans.Name);
                    }
                }
            }

            if (needsRenaming)
            {

                ApplyNamingCosmetics();

                HashtableOfLists htol = new HashtableOfLists();
                foreach (IPfcNode node in Steps)
                {
                    htol.Add(node.Name, node);
                }

                foreach (IPfcLinkElement link in Links)
                {
                    htol.Add(link.Name, link);
                }

                foreach (string key in htol.Keys)
                {
                    if (htol[key].Count > 1)
                    {
                        int i = 0;
                        foreach (IPfcElement element in htol[key])
                        {
                            while (ParticipantDirectory.Contains(key + "_" + (++i)))
                                ;
                            element.SetName(key + "_" + i);
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Removes the null nodes.
        /// </summary>
        public void Reduce()
        {
            SuspendNodeSorting();
            bool doneReducing = false;
            while (!doneReducing)
            {
                bool success1 = false;
                while (ReductionRule_RemoveNullNodesSeries(this))
                {
                    success1 = true;
                }
                bool success2 = false;
                while (ReductionRule_RemoveNullNodesParallel(this))
                {
                    success2 = true;
                }
                doneReducing = !(success1 | success2);
            }
            ResumeNodeSorting();
        }

        public static bool ReductionRule_RemoveNullNodesParallel(IProcedureFunctionChart pfc)
        {
            bool retval = false;
            List<IPfcStepNode> steps = new List<IPfcStepNode>(pfc.Steps);

            foreach (PfcStep s in steps)
            {

                if (s.IsNullNode
                    && s.PredecessorNodes.Count == 1
                    && s.SuccessorNodes.Count == 1)
                {

                    IPfcTransitionNode preTrans = (IPfcTransitionNode)s.Predecessors[0].Predecessor;
                    IPfcTransitionNode postTrans = (IPfcTransitionNode)s.Successors[0].Successor;

                    if (postTrans.PredecessorNodes.Count > 1)
                    {

                        foreach (IPfcLinkElement link in preTrans.Successors)
                        {
                            IPfcStepNode altStep = (IPfcStepNode)link.Successor;
                            if (altStep != null)
                            {
                                if (!altStep.Equals(s))
                                {
                                    List<IPfcNode> pathNodes = new List<IPfcNode>();
                                    pfc.LookForwardForNodesOnPathEndingAt(postTrans, altStep, ref pathNodes);

                                    if (pathNodes.Count > 0 && !pathNodes.Contains(s))
                                    {
                                        pfc.Unbind(preTrans, s);
                                        pfc.Unbind(s, postTrans);
                                        pfc.Delete(s);
                                        retval = true;
                                        //    Console.WriteLine("Removing " + S.ToString() + ".\r\n\tOld Predecessor is " + preTrans.ToString() +
                                        //        ", which now has " + preTrans.SuccessorNodes.Count + " successors.\r\n\tOld Successor is " +
                                        //        postTrans.ToString() + ", which now has " + postTrans.PredecessorNodes.Count + " predecessors.");
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return retval;
        }

        public static bool ReductionRule_RemoveNullNodesSeries(IProcedureFunctionChart pfc)
        {

            foreach (IPfcNode node in pfc.Nodes)
            {
                if (node.IsNullNode)
                {
                    IPfcNode nodeA = null, nodeB = null;
                    if (node.SuccessorNodes.Count == 1 && node.SuccessorNodes[0].IsNullNode)
                    {
                        nodeA = node;
                        nodeB = node.SuccessorNodes[0];
                    }
                    else if (node.PredecessorNodes.Count == 1 && node.PredecessorNodes[0].IsNullNode)
                    {
                        nodeA = node.PredecessorNodes[0];
                        nodeB = node;
                    }
                    else
                    {
                        continue;
                    }

                    if (!nodeA.IsStartNode && !nodeB.IsFinishNode && nodeA.SuccessorNodes.Count == 1 && nodeB.PredecessorNodes.Count == 1)
                    {

                        // We have found two nodes NodeA--->NodeB , where the first and the second nodes are
                        // both null, and connected only to each other in the interior.

                        // HOWEVER: There is one more constraint. The link that replaces these two nodes
                        // cannot connect a parallel divergence with a serial convergence, or a serial
                        // divergence with a parallel convergence. So we want to preent the case where a
                        // nodeA with multiple predecessors, and a nodeB with multiple successors, is taken
                        // out, connecting those multiple predecessors to the multiple successors. This 
                        // would result in all of the new links having multiple outputs from their pred,
                        // and multiple inputs to their succ.

                        int eventualPreSucc = 0;
                        nodeA.PredecessorNodes.ForEach(delegate (IPfcNode pnode)
                        {
                            eventualPreSucc += pnode.Successors.Count;
                        });

                        int eventualSuccPred = 0;
                        nodeB.SuccessorNodes.ForEach(delegate (IPfcNode snode)
                        {
                            eventualSuccPred += snode.Predecessors.Count;
                        });
                        if (!(eventualPreSucc > 1 && eventualSuccPred > 1))
                        {

                            //Console.WriteLine("Reducing the pair, {0}-->{1}", nodeA.Name, nodeB.Name);
                            List<IPfcNode> predecessors = new List<IPfcNode>(nodeA.PredecessorNodes);
                            List<IPfcNode> successors = new List<IPfcNode>(nodeB.SuccessorNodes);

                            // Create them
                            foreach (IPfcNode aPred in predecessors)
                            {
                                foreach (IPfcNode bSucc in successors)
                                {
                                    pfc.Bind(aPred, bSucc);
                                    //Console.WriteLine("\tBinding " + APred.Name + " to " + BSucc);
                                }
                            }

                            predecessors.ForEach(delegate (IPfcNode pred)
                            {
                                pfc.Unbind(pred, nodeA);
                            });
                            successors.ForEach(delegate (IPfcNode succ)
                            {
                                pfc.Unbind(nodeB, succ);
                            });
                            pfc.Unbind(nodeA, nodeB);
                            //Console.WriteLine("\tUnbinding " + nodeA.Name + " from " + nodeB.Name);


                            foreach (IPfcLinkElement link in pfc.Links)
                            {
                                if (link.Predecessor.SuccessorNodes.Count > 1 && link.Successor.PredecessorNodes.Count > 1)
                                {
                                    Console.WriteLine("Found a binding error between " + link.Predecessor.Name + " and " + link.Successor.Name + ".");
                                }
                            }


                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Gets a list of NewGuidHolder objects. After obtaining this list, go through it
        /// and for each NewGuidHolder, inspect the target object, determine the new Guid to
        /// be applied, and set it into the newGuidHolder.NewGuid property. After this, the
        /// entire list must be submitted to the ApplyGuidMap(myNewGuidHolderList); API, and
        /// the new guids will be applied.<para>
        /// </para>
        /// <B>Do not simply set the Guids on the objects.</B>
        /// If, after setting a new guid, you want not to change the object's guid, you can
        /// set it to NewGuidHolder.NO_CHANGE, a special guid that causes the engine to skip
        /// that object in the remapping of guids.
        /// </summary>
        /// <param name="deep">If true, steps' Action Pfc's will return their elements' guids, too.</param>
        /// <returns>A list of NewGuidHolder objects associated with the IPfcElements in this Pfc.</returns>
        public List<NewGuidHolder> GetCleanGuidMap(bool deep)
        {
            List<NewGuidHolder> newGuidHolders = new List<NewGuidHolder>();
            foreach (PfcElement element in Elements)
            {
                newGuidHolders.Add(new NewGuidHolder(element));

                if (element.ElementType.Equals(PfcElementType.Step) && deep)
                {
                    IPfcStepNode step = (IPfcStepNode)element;
                    if (step.Actions.Count > 0)
                    {
                        foreach (ProcedureFunctionChart pfc in step.Actions.Values)
                        {
                            newGuidHolders.AddRange(pfc.GetCleanGuidMap(deep));
                        }
                    }
                }
            }
            return newGuidHolders;
        }

        /// <summary>
        /// Applies the GUID map.
        /// </summary>
        /// <param name="newGuidHolders">The list of NewGuidHolders that serves as a new GUID map.</param>
        public void ApplyGuidMap(List<NewGuidHolder> newGuidHolders)
        {
            BindingFlags bindingAttr = BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.GetField | BindingFlags.NonPublic;
            FieldInfo fi = typeof(PfcElement).GetField("_guid", bindingAttr);
            List<IPfcElement> erroredObjects = new List<IPfcElement>();
            foreach (NewGuidHolder ngh in newGuidHolders)
            {
                if (!ngh.NewGuid.Equals(NewGuidHolder.NoChange))
                {
                    if (ngh.Target is PfcElement)
                    {
                        fi.SetValue(ngh.Target, ngh.NewGuid);
                    }
                    else
                    {
                        try
                        {
                            fi.SetValue(ngh.Target, ngh.NewGuid);
                        }
                        catch (ArgumentException)
                        {
                            erroredObjects.Add(ngh.Target);
                        }
                    }
                }
            }

            if (erroredObjects.Count > 0)
            {
                throw new ApplicationException("There were IPfcElement objects whose Guids were asked to be changed, which " +
                    "were not instances of PfcElement, and which did not have a member (private or otherwise) named, \"_guid\" " +
                    "in which its Guid was being stored. The objects that gave us trouble were " +
                    StringOperations.ToCommasAndAndedListOfNames<IPfcElement>(erroredObjects) + ".");
            }
        }

        /// <summary>
        /// Recursively collapses childrens' participant directories into the parent, renaming the
        /// absorbed child elements and Steps as necessary. Only the rootChart's ParticipantDirectory
        /// is left in existence. All others point up to the root.
        /// </summary>
        /// <param name="rootChart">The root chart.</param>
        public void CollapseParticipantDirectories(IProcedureFunctionChart rootChart)
        {
            foreach (PfcStep child in Steps)
            {

                foreach (IProcedureFunctionChart childPfc in child.Actions.Values)
                {

                    childPfc.CollapseParticipantDirectories(rootChart);

                    Console.WriteLine("Collapsing PD entries from " + childPfc.Name + " up into " + Name + ".");
                    #region Move this step's ParticipantDirectory entry up.

                    // 1.) Add the child PFC's non-Step elements into the parent ParticipantDirectory, unless
                    //        there's conflict. (duplicate macros, for example.)
                    // 2.) Eliminate naming conflicts between my new children, and my peers.
                    // 3.) Add the child's Step elements into the parent under their new names.
                    // 4.) Clear out the child pfc's ParticipantDirectory so that it references its parent.
                    List<ExpressionElement> firstPass = new List<ExpressionElement>(childPfc.ParticipantDirectory);
                    foreach (ExpressionElement ee in firstPass)
                    {
                        if (ee is RoteString)
                        {

                            #region If a roteString or a macro, bubble it up if possible.

                            if (!ParticipantDirectory.Contains(ee.Guid) && !ParticipantDirectory.Contains(ee.Name))
                            {
                                ParticipantDirectory.RegisterMapping(ee.Name, ee.Guid);
                            }
                            else if (ParticipantDirectory.Contains(ee.Guid) && ParticipantDirectory.Contains(ee.Name))
                            {
                                // This element is already registered.
                            }
                            else if (!ParticipantDirectory.Contains(ee.Guid) && ParticipantDirectory.Contains(ee.Name))
                            {
                                // The same rote string is registered under a different Guid - reconcile them by reassigning the
                                // existing expression element to the new guid, and skip registering the new element - this will
                                // make both ExpressionElement owners point to the EE that was there already.
                                ParticipantDirectory.ChangeGuid(ee.Name, ee.Guid);
                            }
                        }
                        else if (ee is Macro)
                        {
                            if (!ParticipantDirectory.Contains(ee.Guid))
                            {
                                ParticipantDirectory.RegisterMacro(ee.GetType());
                            }

                            #endregion

                        }
                        else if (ee is DualModeString)
                        {
                            // Might be a node, or a different entity representing something elsewhere in the system.
                            IPfcNode pfcNode = childPfc.FindFirst(delegate (IPfcNode node)
                            {
                                return node.Guid.Equals(ee.Guid);
                            });
                            if (pfcNode == null)
                            {

                                #region Not a node. Add it into the parent unless there's a string or a name conflict.

                                if (!ParticipantDirectory.Contains(ee.Guid) && !ParticipantDirectory.Contains(ee.Name))
                                {
                                    ParticipantDirectory.RegisterMapping(ee.Name, ee.Guid);
                                }
                                else
                                {
                                    int ndx = (ParticipantDirectory.Contains(ee.Guid) ? 1 : 0) << 1 + (ParticipantDirectory.Contains(ee.Name) ? 1 : 0);
                                    string msg = null;
                                    switch (ndx)
                                    {
                                        case 1:
                                            msg = "Participant directory contains {0} but not {1}";
                                            break;
                                        case 2:
                                            msg = "Participant directory contains {1} but not {0}";
                                            break;
                                        case 3:
                                            msg = "Participant directory contains both {0} and {1}";
                                            break;
                                        default:
                                            msg = "Failure to place mapping {0}-to-{1} into Participant directory.";
                                            break;
                                    }
                                    msg = string.Format(msg, ee.Name, ee.Guid);
                                    throw new ApplicationException(msg);
                                }

                                #endregion

                            }
                            else
                            {
                                // It's a node. We need to make sure it has no naming conflicts with the Parent Pfc's
                                // participant directory.
                                if (ParticipantDirectory.Contains(ee.Name))
                                {
                                    string oldName = pfcNode.Name;
                                    string newName = Name + PathSeparator + oldName;
                                    pfcNode.SetName(newName);
                                }
                                ParticipantDirectory.RegisterMapping(ee.Name, ee.Guid);
                            }
                        }
                    }

                    #endregion

                }
            }
        }

        /// <summary>
        /// A class that holds a reference to an IPfcElement and a new Guid for that element, and
        /// enables the ProcedureFunctionChart to change that guid via its' ApplyGuidMap API.
        /// </summary>
        public class NewGuidHolder
        {
            private Guid _newGuid;
            private IPfcElement _target;

            /// <summary>
            /// A Special Guid which, if set into the NewGuid property, causes the setting of that
            /// guid to be skipped.
            /// </summary>
            public static Guid NoChange = new Guid("50D9A752-C906-44de-9505-B5252C2D76E6");
            /// <summary>
            /// Gets the target element whose guid is to be reset as a result of this NewGuidHolder.
            /// </summary>
            /// <value>The target.</value>
            public IPfcElement Target
            {
                get
                {
                    return _target;
                }
            }
            /// <summary>
            /// Gets or sets the new GUID for the target. If it is NO_CHANGE, then no alteration will
            /// be attempted by the engine, even in the ApplyGuidMap API is called.
            /// </summary>
            /// <value>The new GUID.</value>
            public Guid NewGuid
            {
                get
                {
                    return _newGuid;
                }
                set
                {
                    _newGuid = value;
                }
            }
            /// <summary>
            /// Initializes a new instance of the <see cref="T:NewGuidHolder"/> class.
            /// </summary>
            /// <param name="target">The target pfc element.</param>
            public NewGuidHolder(IPfcElement target)
            {
                _target = target;
                _newGuid = NoChange;
            }
        }

        public static string PathSeparator = ".";

        private static readonly string _schema = // Read from ProcedureFunctionChart.xsd, and replace " with "" ...
        #region SCHEMA STRING

@"<?xml version=""1.0"" encoding=""utf-8""?>
<xs:schema id=""ProcedureFunctionChart"" targetNamespace=""http://tempuri.org/ProcedureFunctionChart.xsd"" elementFormDefault=""qualified"" xmlns=""http://tempuri.org/ProcedureFunctionChart.xsd"" xmlns:mstns=""http://tempuri.org/ProcedureFunctionChart.xsd"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:simpleType name=""Guid"">
    <xs:restriction base=""xs:string"">
      <xs:pattern value=""[0-9A-Fa-f]^8-[0-9A-Fa-f]^4-[0-9A-Fa-f]^4-[0-9A-Fa-f]^4-[0-9A-Fa-f]^12"" />
    </xs:restriction>
  </xs:simpleType>
  <xs:complexType name=""Condition"">
    <xs:sequence>
      <xs:element name=""Expression"" type=""xs:string"" />
      <xs:element name=""Dialect"" type=""xs:string"" />
    </xs:sequence>
  </xs:complexType>
  <xs:complexType name=""Link"">
    <xs:sequence>
      <xs:element name=""Name"" type=""xs:string"" />
      <xs:element name=""Description"" type=""xs:string"" />
      <xs:element name=""Guid"" type=""Guid"" />
      <xs:element name=""PredGuid"" type=""Guid"" />
      <xs:element name=""SuccGuid"" type=""Guid"" />
      <xs:element name=""Priority"" type=""xs:short"" />
    </xs:sequence>
  </xs:complexType>
  <xs:complexType name=""Step"">
    <xs:sequence>
      <xs:element name=""Name"" type=""xs:string"" />
      <xs:element name=""Description"" type=""xs:string"" />
      <xs:element name=""Guid"" type=""Guid"" />
    </xs:sequence>
  </xs:complexType>
  <xs:complexType name=""Transition"">
    <xs:sequence>
      <xs:element name=""Name"" type=""xs:string"" />
      <xs:element name=""Description"" type=""xs:string"" />
      <xs:element name=""Guid"" type=""Guid"" />
      <xs:element name=""Condition"" type=""Condition"" />
    </xs:sequence>
  </xs:complexType>
  <xs:complexType name=""ProcedureFunctionChart"">
    <xs:sequence minOccurs=""1"" maxOccurs=""1"">
      <xs:element name=""Name"" type=""xs:string"" />
      <xs:element name=""Description"" type=""xs:string"" />
      <xs:element name=""Guid"" type=""Guid"" />
      <xs:element name=""RootStepGuid"" type=""Guid"" />
      <xs:element name=""ElementFactoryType"" type=""xs:string"" />
      <xs:element name=""IsSfcCompliant"" type=""xs:string"" />
      <xs:sequence>
        <xs:element name=""Step"" type=""Step"" />
      </xs:sequence>
      <xs:sequence>
        <xs:element name=""Transition"" type=""Transition"" />
      </xs:sequence>
      <xs:sequence>
        <xs:element name=""Link"" type=""Link"" />
      </xs:sequence>
    </xs:sequence>
  </xs:complexType>
</xs:schema>
";

        #endregion SCHEMA STRING

        /// <summary>
        /// Prunes the PFC so that only the steps that return 'true' from the function are left in the PFC.
        /// </summary>
        /// <param name="keepThisStep">A function that returns true if the step is to remain in the pfc.</param>
        public void Prune(Func<IPfcStepNode, bool> keepThisStep)
        {

            List<PfcStep> toEliminate = new List<PfcStep>();
            foreach (PfcStep step in Steps)
            {
                if (!keepThisStep(step))
                {
                    toEliminate.Add(step);
                }
            }

            foreach (PfcStep step in toEliminate)
            {
                List<IPfcNode> tmp = new List<IPfcNode>(step.PredecessorNodes);
                tmp.ForEach(n => Unbind(n, step, skipStructureUpdating: true));
                tmp = new List<IPfcNode>(step.SuccessorNodes);
                tmp.ForEach(n => Unbind(step, n, skipStructureUpdating: true));
            }

            UpdateStructure();

        }

        #region ICloneable Members

        public event CloneHandler CloneEvent;

        public object Clone()
        {
            return Clone(Name + ".clone", "", Guid.NewGuid());
        }

        public virtual IProcedureFunctionChart Clone(string name, string description, Guid guid)
        {
            return _Clone(new ProcedureFunctionChart(Model, name, description, guid));
        }

        /// <summary>
        /// If you override Clone to create your own kind of PFC, call this from inside that override.
        /// </summary>
        /// <param name="newClone">The new clone.</param>
        /// <returns>IProcedureFunctionChart.</returns>
        public IProcedureFunctionChart _Clone(ProcedureFunctionChart newClone)
        {
            PopulateClone(newClone);

            newClone._source = this;

            OnCloneComplete(newClone);

            return newClone;
        }


        #endregion

        protected virtual void PopulateClone(ProcedureFunctionChart newClone)
        {

            Guid cloneHashGuid = GuidOps.XOR(newClone.Guid, Guid);

            Dictionary<IPfcNode, IPfcNode> originalToCloneMap = new Dictionary<IPfcNode, IPfcNode>();
            foreach (IPfcStepNode step in Steps)
            {
                IPfcStepNode cloneStep = newClone.CreateStep(step.Name, step.Description, GuidOps.XOR(cloneHashGuid, step.Guid));
                ((PfcElement)cloneStep).SEID = step.Guid;
                cloneStep.UnitInfo.Name = step.UnitInfo.Name;
                cloneStep.UnitInfo.SequenceNumber = step.UnitInfo.SequenceNumber;

                originalToCloneMap.Add(step, cloneStep);
            }

            Dictionary<IPfcTransitionNode, IPfcTransitionNode> originalToCloneTransitionMap = new Dictionary<IPfcTransitionNode, IPfcTransitionNode>();
            foreach (IPfcTransitionNode transition in Transitions)
            {
                IPfcTransitionNode cloneTransition = newClone.CreateTransition(transition.Name, transition.Description, GuidOps.XOR(cloneHashGuid, transition.Guid));
                ((PfcElement)cloneTransition).SEID = transition.Guid;
                originalToCloneMap.Add(transition, cloneTransition);
                cloneTransition.ExpressionExecutable = transition.ExpressionExecutable;
            }

            foreach (IPfcLinkElement link in Links)
            {

                IPfcNode predecessor = originalToCloneMap[link.Predecessor];
                IPfcNode successor = originalToCloneMap[link.Successor];

                IPfcLinkElement cloneLink = newClone.CreateLink(link.Name, link.Description, GuidOps.XOR(cloneHashGuid, link.Guid), predecessor, successor);
                ((PfcElement)cloneLink).SEID = link.Guid;

            }
        }

        protected void OnCloneComplete(ProcedureFunctionChart myClone)
        {
            if (CloneEvent != null)
            {
                CloneEvent(this, myClone);
            }
        }

        public ExecutionEngineConfiguration ExecutionEngineConfiguration
        {
            get
            {
                return _executionEngineConfiguration;
            }
            set
            {
                _executionEngineConfiguration = value;
            }
        }

        public void Run(IExecutive exec, object userData)
        {
            PfcExecutionContext pfcec = (PfcExecutionContext)userData;
            if (PfcStartRequested != null)
            {
                PfcStartRequested((PfcExecutionContext)userData, null);
            }
            GetPermissionToStart(pfcec, null);
            if (PfcStarting != null)
            {
                PfcStarting((PfcExecutionContext)userData, null);
            }
            ExecutionEngine.Run(exec, userData);
        }

        /// <summary>
        /// Occurs when PFC start requested, but before permission has been obtained to do so.
        /// </summary>
        public event PfcAction PfcStartRequested;

        /// <summary>
        /// Occurs when PFC is starting.
        /// </summary>
        public event PfcAction PfcStarting;

        /// <summary>
        /// Occurs when PFC is completing.
        /// </summary>
        public event PfcAction PfcCompleting;

        internal void FirePfcCompleting(PfcExecutionContext pfcec)
        {
            if (PfcCompleting != null)
            {
                PfcCompleting(pfcec, null);
            }
        }

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
        /// Gets permission from the step to transition to run.
        /// </summary>
        /// <param name="myPfcec">The PFC Execution context under which this PFC will run.</param>
        /// <param name="ssm">The step state machine of the step that is launching this pfc. Presently, this is passed in as null.</param>
        public virtual void GetPermissionToStart(PfcExecutionContext myPfcec, StepStateMachine ssm)
        {

            IExecutive exec = myPfcec.Model.Executive;
            Debug.Assert(exec.CurrentEventType == ExecEventType.Detachable);
            if (EarliestStart != null && EarliestStart > exec.Now)
            {
                exec.CurrentEventController.SuspendUntil(EarliestStart.Value);
            }

            if (_precondition != null)
            {
                _precondition(myPfcec, ssm);
            }

            List<IPfcStepNode> startSteps = GetStartSteps();
            if (startSteps.Count > 1)
            {
                Console.WriteLine("Pfc {0} has multiple start steps. This can cause race conditions in the PFC start permission acquisition process.", Name);
            }
            startSteps.ForEach(delegate (IPfcStepNode startStep)
            {
                startStep.GetPermissionToStart(myPfcec, startStep.MyStepStateMachine);
            });

        }

        private PfcAction _precondition = null;
        public PfcAction Precondition
        {
            set
            {
                _precondition = value;
            }
            get
            {
                return _precondition;
            }
        }

        internal ExecutionEngine ExecutionEngine
        {
            get
            {
                if (_executionEngine == null)
                {
                    if (_executionEngineConfiguration == null)
                    {
                        _executionEngineConfiguration = new ExecutionEngineConfiguration();
                    }
                    _executionEngine = new ExecutionEngine(this, _executionEngineConfiguration);
                    _executionEngine.StepStateChanged += new StepStateMachineEvent(executionEngine_StepStateChanged);
                    _executionEngine.TransitionStateChanged += new TransitionStateMachineEvent(executionEngine_TransitionStateChanged);
                }
                return _executionEngine;
            }
            set
            {
                _executionEngine = value;
            }
        }

        void executionEngine_TransitionStateChanged(TransitionStateMachine tsm, object userData)
        {
            if (TransitionStateChanged != null)
            {
                TransitionStateChanged(tsm, userData);
            }
        }

        void executionEngine_StepStateChanged(StepStateMachine ssm, object userData)
        {
            if (StepStateChanged != null)
            {
                StepStateChanged(ssm, userData);
            }
        }

        public event StepStateMachineEvent StepStateChanged;

        public event TransitionStateMachineEvent TransitionStateChanged;

    }

    internal class TransitionsByGuidSorter : IComparer<IPfcTransitionNode>
    {
        public int Compare(IPfcTransitionNode x, IPfcTransitionNode y)
        {
            return GuidOps.Compare(x.Guid, y.Guid);
        }
    }

    internal class StepsByGuidSorter : IComparer<IPfcStepNode>
    {
        public int Compare(IPfcStepNode x, IPfcStepNode y)
        {
            return GuidOps.Compare(x.Guid, y.Guid);
        }
    }
}
