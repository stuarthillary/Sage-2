/* This source code licensed under the GNU Affero General Public License */
//#define PREANNOUNCE
using System;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable NonReadonlyMemberInGetHashCode
// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Utility
{

    /// <summary>
    /// An event that pertains to some change in relationship between two nodes.
    /// </summary>
    /// <typeparam name="T">The payload type of the nodes.</typeparam>
    /// <param name="self">The node firing the event.</param>
    /// <param name="subject">The node to which the event refers.</param>
    public delegate void TreeNodeEvent<T>(ITreeNode<T> self, ITreeNode<T> subject);

    /// <summary>
    /// An event that pertains to some change in the tree underneath a given node.
    /// </summary>
    /// <typeparam name="T">The payload type of the nodes.</typeparam>
    /// <param name="changeType">The SubtreeChangeType.</param>
    /// <param name="where">The node to which the event refers.</param>
    public delegate void TreeChangeEvent<T>(SubtreeChangeType changeType, ITreeNode<T> where);

    /// <summary>
    /// Want to be able to use TreeNode as either a base class, a container or a wrapper.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TreeNode<T> : ITreeNode<T>
    {

        #region Private Fields
        private T _payload;
        private TreeNodeCollection<T> _children;
        private ITreeNode<T> _parent;
        private ITreeNodeEventController<T> _treeNodeEventController;
        private bool _isSelfReferential;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TreeNode&lt;T&gt;"/> class.
        /// </summary>
        // ReSharper disable once MemberCanBeProtected.Global Treenode can be delegated to, or contain, its payload.
        public TreeNode() : this(default(T)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeNode&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="payload">The payload.</param>
        public TreeNode(T payload)
        {
            _payload = payload;
        }

        #endregion

        /// <summary>
        /// Gets the children, if any, of this node. Return value will be an empty collection if there are no children.
        /// </summary>
        /// <value>The children.</value>
        public IEnumerable<ITreeNode<T>> Children => _children ?? (_children = new TreeNodeCollection<T>(this));

        public ITreeNode<T> AddChild(T newChild, bool skipStructuralChecking = false)
        {
            if (_children == null)
                _children = new TreeNodeCollection<T>(this);
            return _children.Add(newChild, skipStructuralChecking);
        }

        public ITreeNode<T> AddChild(ITreeNode<T> newNode, bool skipStructuralChecking = false)
        {
            if (_children == null)
                _children = new TreeNodeCollection<T>(this);
            return _children.AddNode(newNode, skipStructuralChecking);
        }


        public bool RemoveChild(T existingChild)
        {
            if (_children == null)
                _children = new TreeNodeCollection<T>(this);
            return _children.Remove(existingChild);
        }

        public bool RemoveChild(ITreeNode<T> existingChild)
        {
            if (_children == null)
                _children = new TreeNodeCollection<T>(this);
            return _children.Remove(existingChild);
        }

        public void SortChildren(Comparison<ITreeNode<T>> comparison)
        {
            if (_children == null)
                _children = new TreeNodeCollection<T>(this);
            _children.Sort(comparison);
        }

        public void SortChildren(IComparer<ITreeNode<T>> comparer)
        {
            if (_children == null)
                _children = new TreeNodeCollection<T>(this);
            _children.Sort(comparer);
        }

        public bool HasChild(ITreeNode<T> possibleChild)
        {
            if (_children == null)
                _children = new TreeNodeCollection<T>(this);
            return _children.ContainsNode(possibleChild);
        }

        public bool HasChild(T possibleChild)
        {
            if (_children == null)
                _children = new TreeNodeCollection<T>(this);
            return _children.Contains(possibleChild);
        }

        public void ForEachChild(Action<T> action)
        {
            if (_children == null)
                _children = new TreeNodeCollection<T>(this);
            _children.ForEach(n => action(n.Payload));
        }

        public void ForEachChild(Action<ITreeNode<T>> action)
        {
            if (_children == null)
                _children = new TreeNodeCollection<T>(this);
            _children.ForEach(action);
        }

        /// <summary>
        /// Provides an IEnumerable over the child nodes (i.e. the payloads of the children.)
        /// </summary>
        /// <value>The child nodes.</value>
        public IEnumerable<T> ChildNodes => Children.Cast<T>();

        /// <summary>
        /// Gets or sets the parent of this tree node.
        /// </summary>
        /// <value>The parent.</value>
        public ITreeNode<T> Parent
        {
            [System.Diagnostics.DebuggerStepThrough]
            get
            {
                return _parent;
            }
            set
            {
                SetParent(value, false); // By default, we do not skip structure checking.
            }
        }

        public void SetParent(ITreeNode<T> newParent, bool skipStructureChecking, bool childAlreadyAdded = false)
        {
            if (!Equals(_parent, newParent))
            {
                if (_parent != null)
                {
                    ITreeNode<T> tmpParent = _parent;
                    ITreeNode<T> tmpRoot = Root;
                    _parent = null;
                    tmpParent.RemoveChild(this);
                    tmpRoot.MyEventController.OnSubtreeChanged(SubtreeChangeType.LostNode, this);
                    MyEventController.OnLostParent(tmpParent);
                    tmpParent.MyEventController.OnLostChild(this);
                }
                _parent = newParent;
                if (_parent != null)
                {
                    _parent.Root.MyEventController.OnSubtreeChanged(SubtreeChangeType.GainedNode, this);
                    MyEventController.OnGainedParent(_parent);
                }
            }

            // Cannot skip structure checking with the 'ContainsNode' call because when the parent is set,
            // it sets the child, which sets the parent, which sets the ... . Something has to break this.
            // This is an opportunity for performance improvement.
            if (_parent != null && (skipStructureChecking || !_parent.HasChild(this)))
            {
                if (!childAlreadyAdded)
                {
                    _parent.AddChild((T)this, skipStructureChecking);
                }
            }

        }

        /// <summary>
        /// Gets the root node above this one.
        /// </summary>
        /// <value>The root.</value>
        public ITreeNode<T> Root => _parent == null ? this : _parent.Root;

        #region Enumerators and Enumerables
        /// <summary>
        /// Gets an enumerator over this node's siblings in the hierarchy.
        /// </summary>
        /// <param name="includeSelf">if set to <c>true</c> [include self].</param>
        /// <returns></returns>
        /// <value></value>
        public IEnumerable<T> Siblings(bool includeSelf)
        {
            if (Parent == null && includeSelf)
            {
                yield return Payload;
            }
            else
            {
                if (Parent == null)
                    yield break;
                foreach (ITreeNode<T> tn in Parent.Children)
                {
                    if (!Equals(tn, this) || includeSelf)
                    {
                        yield return tn.Payload;
                    }
                }
            }
        }

        /// <summary>
        /// Returns an iterator that traverses the descendant nodes breadth first, top down.
        /// </summary>
        /// <value>The descendant node iterator.</value>
        public IEnumerable<ITreeNode<T>> DescendantNodesBreadthFirst(bool includeSelf)
        {

            Queue<ITreeNode<T>> q = new Queue<ITreeNode<T>>();

            #region Prime the queue
            if (includeSelf)
            {
                q.Enqueue(this);
            }
            else
            {
                foreach (ITreeNode<T> child in Children)
                {
                    q.Enqueue(child);
                }
            }
            #endregion

            // For every node in the queue, dequeue it, enqueue all of its children, and yield-return it.
            while (q.Count > 0)
            {
                ITreeNode<T> node = q.Dequeue();
                node.ForEachChild(delegate (ITreeNode<T> tn)
                {
                    q.Enqueue(tn);
                });
                yield return node;
            }
        }

        /// <summary>
        /// Returns an iterator that traverses the descendant nodes depth first, top down.
        /// </summary>
        /// <value>The descendant node iterator.</value>
        public IEnumerable<ITreeNode<T>> DescendantNodesDepthFirst(bool includeSelf)
        {
            if (includeSelf)
            {
                yield return this;
            }

            foreach (ITreeNode<T> child in Children)
            {
                foreach (ITreeNode<T> itnt in child.DescendantNodesDepthFirst(true))
                {
                    yield return itnt;
                }
            }
        }

        /// <summary>
        /// Returns an IEnumerable that traverses the descendant payloads breadth first.
        /// </summary>
        /// <value>The descendant payloads iterator.</value>
        public IEnumerable<T> DescendantsBreadthFirst(bool includeSelf) => DescendantNodesBreadthFirst(includeSelf).Select(itnt => itnt.Payload);

        /// <summary>
        /// Returns an iterator that traverses the descendant payloads depth first.
        /// </summary>
        /// <value>The descendant payloads iterator.</value>
        public IEnumerable<T> DescendantsDepthFirst(bool includeSelf) => DescendantNodesDepthFirst(includeSelf).Select(itnt => itnt.Payload);

        #endregion

        #region ITreeNode<T> Members

#if PREANNOUNCE
        internal void _OnAboutToLoseParent(ITreeNode<T> parent) { if (AboutToLoseParent != null) AboutToLoseParent(this, parent); }

        internal void _OnAboutToGainParent(ITreeNode<T> parent) { if (AboutToGainParent != null) AboutToGainParent(this, parent); }

        internal void _OnAboutToLoseChild(ITreeNode<T> child) { if (AboutToLoseChild != null) AboutToLoseChild(this, child); }

        internal void _OnAboutToGainChild(ITreeNode<T> child) { if (AboutToGainChild != null) AboutToGainChild(this, child); }

        public event TreeNodeEvent<T> AboutToLoseParent;

        public event TreeNodeEvent<T> AboutToGainParent;

        public event TreeNodeEvent<T> AboutToLoseChild;

        public event TreeNodeEvent<T> AboutToGainChild;
#endif
        internal void _OnLostParent(ITreeNode<T> parent)
        {
            LostParent?.Invoke(this, parent);
        }

        internal void _OnGainedParent(ITreeNode<T> parent)
        {
            GainedParent?.Invoke(this, parent);
        }

        internal void _OnLostChild(ITreeNode<T> child)
        {
            LostChild?.Invoke(this, child);
        }

        internal void _OnGainedChild(ITreeNode<T> child)
        {
            GainedChild?.Invoke(this, child);
        }

        internal void _OnGainedDescendant(ITreeNode<T> descendant)
        {
            GainedDescendant?.Invoke(this, descendant);
            ((TreeNode<T>)Parent)?._OnGainedDescendant(descendant);
        }

        internal void _OnLostDescendant(ITreeNode<T> descendant)
        {
            LostDescendant?.Invoke(this, descendant);
            ((TreeNode<T>)Parent)?._OnLostDescendant(descendant);
        }

        // ReSharper disable once UnusedParameter.Global // Has to fit the signature.
        internal void _OnChildrenResorted(ITreeNode<T> where)
        {
            ChildrenResorted?.Invoke(this, this);
        }

        internal void _OnSubtreeChanged(SubtreeChangeType changeType, ITreeNode<T> where)
        {
            SubtreeChanged?.Invoke(changeType, where);
        }

        public event TreeNodeEvent<T> LostParent;

        public event TreeNodeEvent<T> GainedParent;

        public event TreeNodeEvent<T> LostChild;

        public event TreeNodeEvent<T> GainedChild;

        public event TreeNodeEvent<T> LostDescendant;

        public event TreeNodeEvent<T> GainedDescendant;

        public event TreeNodeEvent<T> ChildrenResorted;

        public event TreeChangeEvent<T> SubtreeChanged;

        public T Payload
        {
            [System.Diagnostics.DebuggerStepThrough]
            get
            {
                return _payload;
            }
        }

        protected bool IsSelfReferential
        {
            get
            {
                return _isSelfReferential;
            }
            set
            {
                _isSelfReferential = value;
                if (_isSelfReferential)
                {
                    // This is necessary because if we try to directly set m_payload to this,
                    // the static type converter just returns m_payload, and we are setting 
                    // m_payload to m_payload (which is already null). Thus, payload is null
                    // in a self-referential TreeNode if we don't do this.
                    object obj = this;
                    _payload = (T)obj;
                }
            }
        }

        protected void SetPayload(T payload)
        {

            if (_isSelfReferential && !Equals(payload as TreeNode<T>, this))
            {
                string msg = string.Format("Instances of {0} are self-referential, and therefore their payloads cannot be set to other than themselves.", GetType().Name);
                throw new ApplicationException(msg);
            }
            _payload = payload;
        }

        #endregion

        public bool IsChildOf(ITreeNode<T> possibleParentNode)
        {
            ITreeNode<T> cursor = Parent;
            while (cursor != null)
            {
                if (cursor.Equals(possibleParentNode))
                {
                    return true;
                }
                else
                {
                    cursor = cursor.Parent;
                }
            }
            return false;
        }

        public static explicit operator T(TreeNode<T> treeNode)
        {
            return treeNode.Payload;
        }

        public ITreeNodeEventController<T> MyEventController
        {
            get
            {
                return _treeNodeEventController ?? (_treeNodeEventController = new TreeNodeEventController(this));
            }

            protected set
            {
                _treeNodeEventController = value;
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"></see> to compare with the current <see cref="T:System.Object"></see>.</param>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false; // Since I'm not null.

            TreeNode<T> that = obj as TreeNode<T>;
            if (that == null)
            { // It's not a treenode, so compare it to my payload.
                return obj.Equals(_payload);
            }

            // The other object is a TreeNode<T>.
            return GetHashCode() == that.GetHashCode() && Children.Equals(that.Children);

        }

        /// <summary>
        /// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override int GetHashCode()
        {

            if (_payload != null && !Equals(this, _payload))
            {
                return _payload.GetHashCode();
            }

            // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
            return base.GetHashCode();
        }

        private class TreeNodeEventController : ITreeNodeEventController<T>
        {

            private readonly TreeNode<T> _me;
            public TreeNodeEventController(TreeNode<T> me)
            {
                _me = me;
            }

            #region ITreeNodeEventController<T> Members
#if PREANNOUNCE
            public void OnAboutToLoseParent(ITreeNode<T> parent) {
                m_me._OnAboutToLoseParent(parent);
            }

            public void OnAboutToGainParent(ITreeNode<T> parent) {
                m_me._OnAboutToGainParent(parent);
            }

            public void OnAboutToLoseChild(ITreeNode<T> child) {
                m_me._OnAboutToLoseChild(child);
            }

            public void OnAboutToGainChild(ITreeNode<T> child) {
                m_me._OnAboutToGainChild(child);
            }
#endif
            public void OnLostParent(ITreeNode<T> parent)
            {
                _me._OnLostParent(parent);
            }

            public void OnGainedParent(ITreeNode<T> parent)
            {
                _me._OnGainedParent(parent);
            }

            public void OnLostChild(ITreeNode<T> child)
            {
                _me._OnLostChild(child);
                _me._OnLostDescendant(child);
            }

            public void OnGainedChild(ITreeNode<T> child)
            {
                _me._OnGainedChild(child);
                _me._OnGainedDescendant(child);
            }

            public void OnChildrenResorted(ITreeNode<T> self)
            {
                _me._OnChildrenResorted(self);
            }

            public void OnSubtreeChanged(SubtreeChangeType changeType, ITreeNode<T> where)
            {
                _me._OnSubtreeChanged(changeType, where);
            }
            #endregion
        }

        // ReSharper disable once UnusedMember.Local
        private class MuteTreeNodeEventController : ITreeNodeEventController<T>
        {

            // ReSharper disable once UnusedParameter.Local
            public MuteTreeNodeEventController(TreeNode<T> me)
            {
            }

            #region ITreeNodeEventController<T> Members
#if PREANNOUNCE
            public void OnAboutToLoseParent(ITreeNode<T> parent) {}

            public void OnAboutToGainParent(ITreeNode<T> parent) {}

            public void OnAboutToLoseChild(ITreeNode<T> child) {}

            public void OnAboutToGainChild(ITreeNode<T> child) {}
#endif
            public void OnLostParent(ITreeNode<T> parent)
            {
            }

            public void OnGainedParent(ITreeNode<T> parent)
            {
            }

            public void OnLostChild(ITreeNode<T> child)
            {
            }

            public void OnGainedChild(ITreeNode<T> child)
            {
            }

            public void OnChildrenResorted(ITreeNode<T> self)
            {
            }

            public void OnSubtreeChanged(SubtreeChangeType changeType, ITreeNode<T> where)
            {
            }
            #endregion
        }
    }
}