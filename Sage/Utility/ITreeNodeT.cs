/* This source code licensed under the GNU Affero General Public License */
//#define PREANNOUNCE
using System;
using System.Collections.Generic;
// ReSharper disable NonReadonlyMemberInGetHashCode
// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Utility
{
    /// <summary>
    /// The ITreeNode interface is implemented by any object that participates in a tree data structure.
    /// An object may derive from TreeNode&lt;T&gt; or implement ITreeNode&lt;T&gt;.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ITreeNode<T>
    {

        #region Events
#if PREANNOUNCE
        event TreeNodeEvent<T> AboutToLoseParent;
        event TreeNodeEvent<T> AboutToGainParent;
        event TreeNodeEvent<T> AboutToLoseChild;
        event TreeNodeEvent<T> AboutToGainChild;
#endif
        /// <summary>
        /// Fired when this node is detached from a parent.
        /// </summary>
        event TreeNodeEvent<T> LostParent;

        /// <summary>
        /// Fired when this node is attached to a parent.
        /// </summary>
        event TreeNodeEvent<T> GainedParent;

        /// <summary>
        /// Fired when this node has lost a child.
        /// </summary>
        event TreeNodeEvent<T> LostChild;

        /// <summary>
        /// Fired when this node has gained a child.
        /// </summary>
        event TreeNodeEvent<T> GainedChild;

        /// <summary>
        /// <summary>
        /// Fired when this node's child list has been resorted.
        /// </summary>
        /// </summary>
        event TreeNodeEvent<T> ChildrenResorted;

        /// <summary>
        /// Fired when a change (Gain, Loss or Child-Resorting) in this node's subtree has occurred.
        /// </summary>
        event TreeChangeEvent<T> SubtreeChanged;
        #endregion Events

        /// <summary>
        /// Gets the root node above this one.
        /// </summary>
        /// <value>The root.</value>
        ITreeNode<T> Root
        {
            get;
        }

        /// <summary>
        /// Gets the payload of this node. The payload is the node itself, if the subject nodes inherit from TreeNode&lt;T&gt;.
        /// If the Payload is null, and you inherit from TreeNode&lt;T&gt;, you need to set SelfReferential to true in the ctor.
        /// </summary>
        /// <value>The payload.</value>
        T Payload
        {
            get;
        }

        /// <summary>
        /// Gets or sets the parent of this tree node.
        /// </summary>
        /// <value>The parent.</value>
        ITreeNode<T> Parent
        {
            get; set;
        }

        /// <summary>
        /// Sets the parent of this node, but does not then set this node as a child to that parent if childAlreadyAdded is set to <c>true</c>.
        /// </summary>
        /// <param name="newParent">The new parent.</param>
        /// <param name="skipStructureChecking">if set to <c>true</c> [skip structure checking].</param>
        /// <param name="childAlreadyAdded">if set to <c>true</c> [child already added].</param>
        void SetParent(ITreeNode<T> newParent, bool skipStructureChecking, bool childAlreadyAdded = false);

        #region Enumerables
        /// <summary>
        /// Gets an enumerable over this node's siblings in the hierarchy.
        /// </summary>
        IEnumerable<T> Siblings(bool includeSelf);

        /// <summary>
        /// Returns an iterator that traverses the descendant nodes breadth first, top down.
        /// </summary>
        /// <value>The descendant node iterator.</value>
        IEnumerable<ITreeNode<T>> DescendantNodesBreadthFirst(bool includeSelf);

        /// <summary>
        /// Returns an iterator that traverses the descendant nodes depth first, top down.
        /// </summary>
        /// <value>The descendant node iterator.</value>
        IEnumerable<ITreeNode<T>> DescendantNodesDepthFirst(bool includeSelf);

        /// <summary>
        /// Returns an IEnumerable that traverses the descendant payloads breadth first.
        /// </summary>
        /// <value>The descendant payloads iterator.</value>
        IEnumerable<T> DescendantsBreadthFirst(bool includeSelf);

        /// <summary>
        /// Returns an IEnumerable that traverses the descendant payloads depth first.
        /// </summary>
        /// <value>The descendant payloads iterator.</value>
        IEnumerable<T> DescendantsDepthFirst(bool includeSelf);

        #endregion Enumerables

        /// <summary>
        /// Determines whether this node is a child of the specified 'possible parent' node.
        /// </summary>
        /// <param name="possibleParentNode">The possible parent node.</param>
        /// <returns>
        /// 	<c>true</c> if this node is a child of the specified 'possible parent' node; otherwise, <c>false</c>.
        /// </returns>
        bool IsChildOf(ITreeNode<T> possibleParentNode);

        /// <summary>
        /// Gets the children, if any, of this node. Return value will be an empty collection if there are no children.
        /// </summary>
        /// <value>The children.</value>
        IEnumerable<ITreeNode<T>> Children
        {
            get;
        }

        ITreeNode<T> AddChild(T newChild, bool skipStructuralChecking = false);
        ITreeNode<T> AddChild(ITreeNode<T> tn, bool skipStructuralChecking = false);
        bool RemoveChild(T existingChild);
        bool RemoveChild(ITreeNode<T> existingChild);
        void SortChildren(Comparison<ITreeNode<T>> comparison);
        void SortChildren(IComparer<ITreeNode<T>> comparer);
        bool HasChild(ITreeNode<T> existingChild);
        bool HasChild(T possibleChild);
        void ForEachChild(Action<T> action);
        void ForEachChild(Action<ITreeNode<T>> action);

        /// <summary>
        /// Provides an IEnumerable over the child nodes (i.e. the payloads of the children.)
        /// </summary>
        /// <value>The child nodes.</value>
        IEnumerable<T> ChildNodes
        {
            get;
        }

        /// <summary>
        /// Gets the tree node event controller. This should only be obtained by a descendant
        /// or parent TreeNode or TreeNodeCollection to report changes that are taking place
        /// with respect to the subject TreeNode so that it may report its own changes.
        /// </summary>
        /// <value>The tree node event controller.</value>
        ITreeNodeEventController<T> MyEventController
        {
            get;
        }

    }
}