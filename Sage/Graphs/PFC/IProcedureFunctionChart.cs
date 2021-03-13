/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Graphs.PFC.Execution;
using Highpoint.Sage.Graphs.PFC.Expressions;
using Highpoint.Sage.SimCore;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Highpoint.Sage.Graphs.PFC
{
    /// <summary>
    /// IProcedureFunctionChart is implemented by a type that provides overall management, including creation,
    /// running and modification of an SFC graph.
    /// <para></para><b>IMPORTANT NOTE: Any class implementing IProcedureFunctionChart must have a constructor
    /// that accepts a IProcedureFunctionChart, in order for serialization to work properly.</b>
    /// </summary>
    public interface IProcedureFunctionChart : IModelObject, IXmlSerializable, SimCore.ICloneable
    {

        /// <summary>
        /// Gets or sets the element factory in use by this ProcedureFunctionChart.
        /// </summary>
        /// <value>The element factory.</value>
        IPfcElementFactory ElementFactory
        {
            get; set;
        }

        /// <summary>
        /// Creates a new link. It must later be bound to a predecessor and a successor.
        /// Throws an exception if the Guid is already known to this ProcedureFunctionChart.
        /// </summary>
        /// <returns>The <see cref="T:IPfcLinkElement"/>.</returns>
        IPfcLinkElement CreateLink();

        /// <summary>
        /// Creates a new link. It must later be bound to a predecessor and a successor.
        /// Throws an exception if the Guid is already known to this ProcedureFunctionChart.
        /// </summary>
        /// <param name="name">The name of the new link.</param>
        /// <param name="guid">The GUID of the new link.</param>
        /// <param name="description">The description of the new link.</param>
        /// <returns>The <see cref="T:IPfcLinkElement"/>.</returns>
        IPfcLinkElement CreateLink(string name, string description, Guid guid);

        /// <summary>
        /// Creates a link with the specified name, guid, predecessor &amp; successor.
        /// </summary>
        /// <param name="name">The name of the new link.</param>
        /// <param name="description">The description of the new link.</param>
        /// <param name="guid">The GUID of the new link.</param>
        /// <param name="predecessor">The predecessor of the new link.</param>
        /// <param name="successor">The successor of the new link.</param>
        /// <returns>The <see cref="T:IPfcLinkElement"/>.</returns>
        IPfcLinkElement CreateLink(string name, string description, Guid guid, IPfcNode predecessor, IPfcNode successor);

        /// <summary>
        /// Creates and adds a step with default information. Throws an exception if the Guid is already in use.
        /// </summary>
        /// <returns>The <see cref="T:IPfcStepNode"/>.</returns>
        IPfcStepNode CreateStep();

        /// <summary>
        /// Creates and adds a step with the specified information. Throws an exception if the Guid is already in use.
        /// </summary>
        /// <param name="name">The name of the step.</param>
        /// <param name="description">The description of the step.</param>
        /// <param name="guid">The GUID of the step.</param>
        /// <returns>The <see cref="T:IPfcStepNode"/>.</returns>
        IPfcStepNode CreateStep(string name, string description, Guid guid);

        /// <summary>
        /// Creates and adds a transition with default information. Throws an exception if the Guid is already in use.
        /// </summary>
        /// <returns>The <see cref="T:IPfcTransitionNode"/>.</returns>
        IPfcTransitionNode CreateTransition();

        /// <summary>
        /// Creates and adds a transition with the specified information. Throws an exception if the Guid is already in use.
        /// </summary>
        /// <param name="name">Name of the transition.</param>
        /// <param name="description">The transition description.</param>
        /// <param name="guid">The transition GUID.</param>
        /// <returns>The <see cref="T:IPfcTransitionNode"/>.</returns>
        IPfcTransitionNode CreateTransition(string name, string description, Guid guid);

        /// <summary>
        /// Binds the specified predecessor to the specified successor.
        /// </summary>
        /// <param name="predecessor">The predecessor.</param>
        /// <param name="successor">The successor.</param>
        void Bind(IPfcNode predecessor, IPfcLinkElement successor);

        /// <summary>
        /// Binds the specified predecessor to the specified successor.
        /// </summary>
        /// <param name="predecessor">The predecessor.</param>
        /// <param name="successor">The successor.</param>
        void Bind(IPfcLinkElement predecessor, IPfcNode successor);

        /// <summary>
        /// Binds the two nodes. If both are steps, it inserts a transition between them, and if both are 
        /// transitions, it inserts a step between them - in both cases, creating links between the 'from'
        /// node, the shim node, and the 'to' node. Piggybacking is allowed by default. Use the full-featured
        /// API to disallow piggybacking.
        /// </summary>
        /// <param name="from">The node from which a connection is being established.</param>
        /// <param name="to">The node to which a connection is being established.</param>
        void Bind(IPfcNode from, IPfcNode to);

        /// <summary>
        /// Binds the two linkables. If both are steps, it inserts a transition between them, and if both are
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
        void Bind(IPfcNode from, IPfcNode to, out IPfcLinkElement iPfcLink1, out IPfcNode shimNode, out IPfcLinkElement iPfcLink2, bool allowPiggybacking);

        /// <summary>
        /// Unbinds the two nodes, removing the link between them. Returns false if they were
        /// not connected directly in the first place. If called directly by the user, this
        /// API can result in an illegal PFC graph.
        /// </summary>
        /// <param name="from">The upstream node of the unbinding.</param>
        /// <param name="to">The downstream node of the unbinding.</param>
        ///<param name="skipStructureUpdating">if set to <c>true</c> skips the UpdateStructure. Useful for optimizing bulk updates.</param>
        /// <returns></returns>
        bool Unbind(IPfcNode from, IPfcNode to, bool skipStructureUpdating = false);

        /// <summary>
        /// Unbinds the node from the link. Returns false if they were not
        /// connected directly in the first place. If called directly by
        /// the user, this API can result in an illegal PFC graph.
        /// </summary>
        /// <param name="from">The upstream node of the unbinding.</param>
        /// <param name="to">The downstream link of the unbinding.</param>
        ///<param name="skipStructureUpdating">if set to <c>true</c> skips the UpdateStructure. Useful for optimizing bulk updates.</param>
        /// <returns>True, if successful, otherwise, false.</returns>
        bool Unbind(IPfcNode from, IPfcLinkElement to, bool skipStructureUpdating = false);

        /// <summary>
        /// Unbinds the link from the node. Returns false if they were not
        /// connected directly in the first place. If called directly by
        /// the user, this API can result in an illegal PFC graph.
        /// </summary>
        /// <param name="from">The upstream link of the unbinding.</param>
        /// <param name="to">The downstream node of the unbinding.</param>
        /// <param name="skipStructureUpdating">if set to <c>true</c> skips the UpdateStructure. Useful for optimizing bulk updates.</param>
        /// <returns>True, if successful, otherwise, false.</returns>
        bool Unbind(IPfcLinkElement from, IPfcNode to, bool skipStructureUpdating = false);


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
        bool Delete(IPfcNode node);

        /// <summary>
        /// Binds the inbound elements to the outbound elements through a synchronizer construct. All elements in
        /// both arrays must be the same type (either all Steps or all Transitions), and null or empty arrays are
        /// illegal.
        /// </summary>
        /// <param name="predecessors">The predecessor elements.</param>
        /// <param name="successors">The successor elements.</param>
        void Synchronize(IPfcNode[] predecessors, IPfcNode[] successors);

        /// <summary>
        /// Binds the inbound elements to the outbound elements through a synchronizer construct. Empty collections are illegal.
        /// </summary>
        /// <param name="inbound">The inbound elements.</param>
        /// <param name="outbound">The outbound elements.</param>
        void Synchronize(PfcNodeList inbound, PfcNodeList outbound);

        ///// <summary>
        ///// Initializes the SFC with the specified steps, transitions and links. Any pre-existent nodes are cleared out.
        ///// </summary>
        ///// <param name="steps">The steps.</param>
        ///// <param name="transitions">The transitions.</param>
        ///// <param name="links">The links.</param>
        //void Initialize(PfcStepNodeList steps, PfcTransitionNodeList transitions, PfcLinkElementList links);

        /// <summary>
        /// A directory of participants in and below this Pfc, used in creation of expressions.
        /// </summary>
        ParticipantDirectory ParticipantDirectory
        {
            get;
        }

        /// <summary>
        /// By default, this orders a node's downstream links' priorities and thereby graph ordinals as GOOBER
        /// </summary>
        IComparer<IPfcLinkElement> LinkComparer
        {
            get; set;
        }
        /// <summary>
        /// Gets the parent step node for this SFC.
        /// </summary>
        /// <value>The parent step node.</value>
        IPfcStepNode Parent
        {
            get; set;
        }

        /// <summary>
        /// Gets the source PFC, if any, from which this PFC was cloned.
        /// </summary>
        /// <value>The source.</value>
        IProcedureFunctionChart Source
        {
            get;
        }

        /// <summary>
        /// Gets all of the edges (links) under management of this Procedure Function Chart. This is a
        /// read-only collection.
        /// </summary>
        /// <value>The edges (links).</value>
        PfcLinkElementList Edges
        {
            get;
        }

        /// <summary>
        /// Gets all of the edges (links) under management of this Procedure Function Chart. This is a
        /// read-only collection.
        /// </summary>
        /// <value>The edges (links).</value>
        PfcLinkElementList Links
        {
            get;
        }

        /// <summary>
        /// Gets the steps under management of this Procedure Function Chart. This is a
        /// read-only collection.
        /// </summary>
        /// <value>The steps.</value>
        PfcStepNodeList Steps
        {
            get;
        }

        /// <summary>
        /// Gets the transitions under management of this Procedure Function Chart. This is a
        /// read-only collection.
        /// </summary>
        /// <value>The transitions.</value>
        PfcTransitionNodeList Transitions
        {
            get;
        }

        /// <summary>
        /// Gets all of the nodes (steps and transitions)under management of this Procedure Function Chart. This is a
        /// read-only collection.
        /// </summary>
        /// <value>The nodes.</value>
        PfcNodeList Nodes
        {
            get;
        }

        /// <summary>
        /// Gets the elements contained directly in this Pfc.
        /// </summary>
        /// <value>The elements.</value>
        List<IPfcElement> Elements
        {
            get;
        }

        /// <summary>
        /// Gets all of the elements that are contained in or under this Pfc, to a depth
        /// specified by the 'depth' parameter, and that pass the 'filter' criteria.
        /// </summary>
        /// <param name="depth">The depth to which retrieval is to be done.</param>
        /// <param name="filter">The filter predicate that dictates which elements are acceptable.</param>
        /// <param name="children">The children, treated as a return value.</param>
        /// <returns></returns>
        void GetChildren(int depth, Predicate<IPfcElement> filter, ref List<IPfcElement> children);

        /// <summary>
        /// This is a performance enhancer - when making internal changes (i.e. changes that are a
        /// part of a larger process such as flattening a Pfc hierarchy), there is no point to doing
        /// node sorting on the entire graph, each time. So, prior to the start of the wholesale
        /// changes, suspend node sorting, and then resume once the changes are complete. Resuming
        /// also results in a call to UpdateStructure(...).
        /// </summary>
        void ResumeNodeSorting();

        /// <summary>
        /// This is a performance enhancer - when making internal changes (i.e. changes that are a
        /// part of a larger process such as flattening a Pfc hierarchy), there is no point to doing
        /// node sorting on the entire graph, each time. So, prior to the start of the wholesale
        /// changes, suspend node sorting, and then resume once the changes are complete. Resuming
        /// also results in a call to UpdateStructure(...).
        /// </summary>
        void SuspendNodeSorting();

        /// <summary>
        /// Updates the structure of the PFC and sorts outbound links per their priority then their textual names, then
        /// their guids. Then does a breadth-first traversal, assigning nodes a sequence number. Finally sorts node lists
        /// per their sequence numbers. Loop breaking then can occur between the node with the higher sequence number and
        /// the *following* node with the lower number. This way, loop-break always occurs at the intuitively-correct place.
        /// </summary>
        /// <param name="breadthFirstOrdinalNumbers">if set to <c>false</c> assigns ordinals in a depth-first order.</param>
        void UpdateStructure(bool breadthFirstOrdinalNumbers = true);

        /// <summary>
        /// Creates an XML string representation of this Pfc.
        /// </summary>
        /// <returns>The newly-created Xml string.</returns>
        string ToXmlString();

        /// <summary>
        /// Finds the node at the specified path from this location. Currently, works only absolutely from this PFC.
        /// <para></para>
        /// </summary>
        /// <param name="path">The path (e.g. ParentName/ChildName).</param>
        IPfcNode FindNode(string path);

        /// <summary>
        /// Finds the first node for which the predicate returns true.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        IPfcNode FindFirst(Predicate<IPfcNode> predicate);

        /// <summary>
        /// Retrieves a depth-first iterator over all nodes in this PFC that satisfy the predicate.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        /// <returns></returns>
        IEnumerable<IPfcNode> FindAll(Predicate<IPfcNode> predicate);

        /// <summary>
        /// Retrieves a depth-first iterator over all nodes in this PFC.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IPfcNode> DepthFirstIterator();

        /// <summary>
        /// Transmogrifies this PFC and all of its child PFCs (the actions associated with steps)
        /// into one flat PFC with no children. Steps that had children are replaced by their
        /// children, inserted inline into the parents' PFC structure, in place of the parent.
        /// </summary>
        void Flatten();

        /// <summary>
        /// Gets the start steps in this ProcedureFunctionChart.
        /// </summary>
        /// <returns>The start steps.</returns>
        List<IPfcStepNode> GetStartSteps();

        /// <summary>
        /// Gets the finish steps in this ProcedureFunctionChart.
        /// </summary>
        /// <returns>The finish steps.</returns>
        List<IPfcStepNode> GetFinishSteps();

        /// <summary>
        /// Gets the finish transition in this ProcedureFunctionChart.
        /// </summary>
        /// <returns>The finish transition.</returns>
        IPfcTransitionNode GetFinishTransition();
        /// <summary>
        /// Adds the element to the PFC.
        /// </summary>
        /// <param name="element">The element to be added to the PFC.</param>
        void AddElement(IPfcElement element);

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
        List<ProcedureFunctionChart.NewGuidHolder> GetCleanGuidMap(bool deep);

        /// <summary>
        /// Applies the GUID map.
        /// </summary>
        /// <param name="newGuidHolders">The list of NewGuidHolders that serves as a new GUID map.</param>
        void ApplyGuidMap(List<ProcedureFunctionChart.NewGuidHolder> newGuidHolders);

        /// <summary>
        /// Recursively collapses childrens' participant directories into the parent, renaming the
        /// absorbed child elements and Steps as necessary. Only the rootChart's ParticipantDirectory
        /// is left in existence. All others point up to the root.
        /// </summary>
        /// <param name="rootChart">The root chart.</param>
        void CollapseParticipantDirectories(IProcedureFunctionChart rootChart);

        /// <summary>
        /// Reduces this procedure function chart, applying reduction rules until the PFC is no longer reduceable.
        /// </summary>
        void Reduce();

        /// <summary>
        /// Looks forward from the node for nodes on path ending at the finish node.
        /// </summary>
        /// <param name="finish">The finish.</param>
        /// <param name="node">The node.</param>
        /// <param name="deletees">The deletees.</param>
        /// <returns></returns>
        bool LookForwardForNodesOnPathEndingAt(IPfcNode finish, IPfcNode node, ref List<IPfcNode> deletees);


        /// <summary>
        /// Applies the naming cosmetics appropriate for the type of recipe being generated. This is currently
        /// hard-coded, and performs naming of transitions to T_001, T_002, ... T_00n, and null steps to 
        /// NULL_UP:0, NULL_UP:1, ... NULL_UP:n.
        /// </summary>
        void ApplyNamingCosmetics();


        void Prune(Func<IPfcStepNode, bool> keepThisStep);

        /// <summary>
        /// Runs the PFC under control of the specified executive.
        /// </summary>
        /// <param name="exec">The exec.</param>
        /// <param name="userData">The user data.</param>
        void Run(IExecutive exec, object userData);

        DateTime? EarliestStart
        {
            get; set;
        }

        void GetPermissionToStart(PfcExecutionContext myPfcec, StepStateMachine ssm);

        PfcAction Precondition
        {
            get; set;
        }

        /// <summary>
        /// Occurs when PFC start requested, but before permission has been obtained to do so.
        /// </summary>
        event PfcAction PfcStartRequested;

        /// <summary>
        /// Occurs when PFC is starting.
        /// </summary>
        event PfcAction PfcStarting;

        /// <summary>
        /// Occurs when PFC is completing.
        /// </summary>
        event PfcAction PfcCompleting;

        event StepStateMachineEvent StepStateChanged;

        event TransitionStateMachineEvent TransitionStateChanged;

    }
}
