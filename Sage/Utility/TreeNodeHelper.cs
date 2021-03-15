/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;

namespace Highpoint.Sage.Utility
{
    /// <summary>
    /// A class that manages parent &amp;child relationships in a tree. Objects that participate in the tree can
    /// delegate to this class to manage the relationships. This class provides an object with its parent,
    /// its child list, events on the addition &amp; removal of others and itself from the tree, enables sorting &amp;
    /// sequencing of the members of the tree, and will, if set up as an auto-indexing TreeNodeHelper, will
    /// perform indexing of the node's children based on their guids, if they implement IHasIdentity.
    /// </summary>
    public class TreeNodeHelper : ITreeNodeProxy
    {

        #region >>> Private Fields <<<
        private static readonly ArrayList _empty_List = ArrayList.ReadOnly(new ArrayList());
        private TreeNodeHelper _parent;
        private ArrayList _children;
        private Hashtable _childFinder;
        private readonly bool _autoIndex;
        #endregion

        #region >>> Protected Fields <<<
        protected void _OnAboutToGainChild(TreeNodeHelper newChild)
        {
            OnAboutToGainChild?.Invoke(this, newChild);
        }

        protected void _OnGainedChild(TreeNodeHelper newChild)
        {
            OnGainedChild?.Invoke(this, newChild);
        }

        protected void _OnAboutToLoseChild(TreeNodeHelper newChild)
        {
            OnAboutToLoseChild?.Invoke(this, newChild);
        }

        protected void _OnLostChild(TreeNodeHelper newChild)
        {
            OnLostChild?.Invoke(this, newChild);
        }

        protected void AboutToBeRemoved(TreeNodeHelper childInTheCrosshairs)
        {
            OnAboutToBeRemoved?.Invoke(this, childInTheCrosshairs);
        }

        protected void WasRemoved(TreeNodeHelper newOrphan)
        {
            OnWasRemoved?.Invoke(this, newOrphan);
        }

        #endregion

        /// <summary>
        /// Creates a TreeNodeHelper with the indicated object as its ward. It is read-only or auto-indexed according to the 
        /// </summary>
        /// <param name="ward">The object that this TreeNodeHelper is helping. If the ward is derived from TreeNodeHelper, use 'this'.</param>
        /// <param name="readOnly">True if this TreeNodeHelper cannot change the tree structure.</param>
        /// <param name="autoIndex">True if all children will be implementers of IHasIdentity, and should be indexed for the GetChild(Guid childID) API.</param>
        public TreeNodeHelper(object ward, bool readOnly, bool autoIndex)
        {
            Ward = ward;
            _parent = null;
            _children = null;
            _childFinder = null;
            IsReadOnly = readOnly;
            _autoIndex = autoIndex;
        }

        /// <summary>
        /// A TreeNode's Ward is the object it represents - the object that actually is
        /// conceptually a part of the tree being managed. If the object in the
        /// hierarchical tree inherits from TreeNodeHelper, then Ward is 'this'.
        /// </summary>
        public object Ward
        {
            get;
        }

        /// <summary>
        /// Creates a wrapper around the provided object that matches this one with respect to read-only and indexing settings.
        /// </summary>
        /// <param name="ward">The object whose parent/child relationships the new TreeNodeHelper is to manage.</param>
        /// <returns>A new instance of TreeNodeHelper.</returns>
        public ITreeNodeProxy CreateNodeWrapper(object ward)
        {
            return new TreeNodeHelper(ward, IsReadOnly, _autoIndex);
        }

        /// <summary>
        /// Sets this node's ReadOnly property to the new value.
        /// </summary>
        /// <param name="toWhat">The new value for the ReadOnly property.</param>
        public void SetReadOnly(bool toWhat)
        {
            IsReadOnly = toWhat;
        }

        #region ITreeNode Members
        /// <summary>
        /// True if the tree cannot be reconfigured through this implementer (no adding/removing parents or children.)
        /// </summary>
        public bool IsReadOnly
        {
            get; private set;
        }

        /// <summary>
        /// True if this implementer has no children.
        /// </summary>
        public bool IsLeaf => (_children == null || _children.Count == 0);

        /// <summary>
        /// True if this implementer has no parent.
        /// </summary>
        public bool IsRoot => (_parent == null);

        /// <summary>
        /// Gets the root node at or above this node.
        /// </summary>
        /// <returns>The root node at or above this node.</returns>
        public ITreeNode GetRoot()
        {
            return IsRoot ? this : _parent.GetRoot();
        }

        /// <summary>
        /// The parent of this object.
        /// </summary>
        public ITreeNode Parent
        {
            get
            {
                return _parent;
            }
            set
            {
                if (!IsReadOnly)
                {
                    if (Equals(value, _parent))
                        return;
                    TreeNodeHelper oldParent = _parent;
                    TreeNodeHelper newParent = (TreeNodeHelper)value; // TODO: Come up with a safer mechanism for this.
                    oldParent?._OnAboutToGainChild(this);
                    newParent?._OnAboutToGainChild(this);

                    _parent = (TreeNodeHelper)value;

                    oldParent?._OnGainedChild(this);
                    newParent?._OnGainedChild(this);
                }
                else
                {
                    ReadOnlyAccessViolation("Parent");
                }
            }
        }

        /// <summary>
        /// The children of this object.
        /// </summary>
        public IList Children
        {
            get
            {
                if (_children == null)
                    return ArrayList.ReadOnly(_empty_List);
                return ArrayList.ReadOnly(CreateChildList());
            }
        }

        private ArrayList CreateChildList()
        {
            ArrayList al = new ArrayList();
            foreach (object obj in _children)
            {
                //				if ( obj is ITreeNodeProxy) {
                //					al.Add(((ITreeNodeProxy)obj).Ward);
                //				} else {
                al.Add(obj);
                //				}
            }
            return al;
        }

        /// <summary>
        /// Adds a child to this object. If the TreeNodeHelper is set to autoIndex, the new child
        /// must be an implementer of IHasIdentity, and will be indexed into the list of children.
        /// </summary>
        /// <param name="newChild">The child to be added to this parent.</param>
        /// <returns>The TreeNodeHelper that wraps the new child.</returns>
        public ITreeNode AddChild(object newChild)
        {
            ITreeNode node = CreateNodeWrapper(newChild);
            if (!IsReadOnly)
            {
                OnAboutToGainChild?.Invoke(this, node);
                if (_children == null)
                    _children = new ArrayList();
                _children.Add(node);
                node.Parent = this;
                if (_autoIndex)
                {
                    if (_childFinder == null)
                    {
                        _childFinder = new Hashtable();
                    }
                    ITreeNodeProxy tnp = (ITreeNodeProxy)node;
                    SimCore.IHasIdentity ihi = (SimCore.IHasIdentity)tnp.Ward;

                    //if ( m_childFinder.Contains(ihi.Guid) ) Console.WriteLine();

                    _childFinder.Add(ihi.Guid, tnp);
                }
                OnGainedChild?.Invoke(this, node);
            }
            else
            {
                ReadOnlyAccessViolation("AddChild");
            }
            return node;
        }

        /// <summary>
        /// Removes a child from this object.
        /// </summary>
        /// <param name="child">The child to be removed from this parent.</param>
        public void RemoveChild(object child)
        {
            ITreeNode node = CreateNodeWrapper(child); // TODO: This might not remove correctly.
            if (!IsReadOnly)
            {
                if (_children == null || (!_children.Contains(node)))
                    return;
                OnAboutToLoseChild?.Invoke(this, node);
                ((TreeNodeHelper)node).AboutToBeRemoved(((TreeNodeHelper)node));
                _children.Remove(node);
                if (_autoIndex)
                {
                    ITreeNodeProxy tnp = (ITreeNodeProxy)node;
                    SimCore.IHasIdentity ihi = (SimCore.IHasIdentity)tnp.Ward;
                    _childFinder.Remove(ihi.Guid);
                }
                OnLostChild?.Invoke(this, node);
                ((TreeNodeHelper)node).WasRemoved(((TreeNodeHelper)node));
            }
            else
            {
                ReadOnlyAccessViolation("RemoveChild");
            }
        }

        /// <summary>
        /// Removes all children.
        /// </summary>
        public void ClearChildren()
        {
            if (!IsReadOnly)
            {
                foreach (TreeNodeHelper tnh in _children)
                    RemoveChild(tnh);
            }
            else
            {
                ReadOnlyAccessViolation("ClearChildren");
            }
        }

        /// <summary>
        /// Sorts children according to the supplied IComparer.
        /// </summary>
        /// <param name="sequencer">The supplied IComparer.</param>
        public void ResequenceChildren(IComparer sequencer)
        {
            IComparer tnhWrapper = new TnhComparerWrapper(sequencer);
            if (!IsReadOnly)
            {
                _children.Sort(tnhWrapper);
            }
            else
            {
                ReadOnlyAccessViolation("ResequenceChildren");
            }
        }

        /// <summary>
        /// Finds the child of this node that has the specified guid key.
        /// </summary>
        /// <param name="key">The key for the child being sought.</param>
        /// <returns>The child node that has the specified guid key.</returns>
        public ITreeNode GetChild(Guid key)
        {
            if (_autoIndex)
            {
                return (ITreeNode)_childFinder?[key];
            }
            else
            {
                throw new ApplicationException("Called \"GetChild(...)\" on a TreeNodeHelper that is not set to autoIndex.");
            }
        }

        /// <summary>
        /// Fires when this object is about to be removed from a parent's child list.
        /// </summary>
        public event TreeNodeInteractionEvent OnAboutToBeRemoved;

        /// <summary>
        /// Fires after this object has been removed from a parent's child list.
        /// </summary>
        public event TreeNodeInteractionEvent OnWasRemoved;

        /// <summary>
        /// Fires when this object is about to gain a new member of it's child list.
        /// </summary>
        public event TreeNodeInteractionEvent OnAboutToGainChild;

        /// <summary>
        /// Fires after this object has gained a new member of it's child list.
        /// </summary>
        public event TreeNodeInteractionEvent OnGainedChild;

        /// <summary>
        /// Fires when this object is about to lose a new member of it's child list.
        /// </summary>
        public event TreeNodeInteractionEvent OnAboutToLoseChild;

        /// <summary>
        /// Fires after this object has lost a new member of it's child list.
        /// </summary>
        public event TreeNodeInteractionEvent OnLostChild;

        #endregion

        /// <summary>
        /// True if this treeNode helper's ward is equal to another object or another ITreeNodeHelper's ward object.
        /// </summary>
        /// <param name="obj">The ward object or other ITreeNodeHelper implementer being tested.</param>
        /// <returns>True if this treeNode helper's ward is equal to another object or another ITreeNodeHelper's ward object.</returns>
        public override bool Equals(object obj)
        {
            return Ward.Equals(obj);
        }

        /// <summary>
        /// Returns the hashCode of this TreeNodeHelper's ward object.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Ward.GetHashCode();
        }

        /// <summary>
        /// Produces a string representation of the entire tree below this node.
        /// </summary>
        /// <returns>System.String.</returns>
        public string ToStringDeep()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            Dump(ref sb, 0, this);
            return sb.ToString();
        }

        private void ReadOnlyAccessViolation(string where)
        {
            throw new ArgumentException("Violation of read-only constraint on function " + where + " of TreeNodeHelper.");
        }

        private void Dump(ref System.Text.StringBuilder sb, int levels, ITreeNode parent)
        {
            for (int i = 0; i < levels; i++)
                sb.Append("\t");
            TreeNodeHelper helper = parent as TreeNodeHelper;
            if (helper != null)
            {
                sb.Append(helper.Ward + "\r\n");
            }
            else
            {
                sb.Append(parent + "\r\n");
            }
            foreach (ITreeNode child in parent.Children)
                Dump(ref sb, levels + 1, child);
        }

        class TnhComparerWrapper : IComparer
        {
            readonly IComparer _comparer;

            public TnhComparerWrapper(IComparer comparer)
            {
                _comparer = comparer;
            }
            #region IComparer Members

            public int Compare(object x, object y)
            {
                TreeNodeHelper tnhx = (TreeNodeHelper)x;
                TreeNodeHelper tnhy = (TreeNodeHelper)y;
                return _comparer.Compare(tnhx.Ward, tnhy.Ward);
            }

            #endregion

        }
    }
}
