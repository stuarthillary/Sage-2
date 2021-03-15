/* This source code licensed under the GNU Affero General Public License */
//#define PREANNOUNCE
using System;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable NonReadonlyMemberInGetHashCode
// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Utility
{
    public class TreeNodeCollection<T> : IEnumerable<ITreeNode<T>>
    {

        private readonly ITreeNode<T> _parent;
        private readonly List<ITreeNode<T>> _children;

        public TreeNodeCollection(ITreeNode<T> parent)
        {
            _parent = parent;
            _children = new List<ITreeNode<T>>();
        }

        #region Mutating members.
        /// <summary>
        /// Adds the specified new child to this collection.
        /// </summary>
        /// <param name="newChild">The new child.</param>
        /// <param name="skipStructuralChecking">if set to <c>true</c> addition of this child will be perforemd without structural checking.</param>
        /// <returns>
        /// The TreeNode that resulted from this addition - either the node to be added, or its TreeNode wrapper.
        /// </returns>
        public ITreeNode<T> Add(T newChild, bool skipStructuralChecking = false)
        {

            // If necessary, create a TreeNode wrapper.
            ITreeNode<T> tn = newChild as ITreeNode<T> ?? new TreeNode<T>(newChild);

            return AddNode(tn, skipStructuralChecking);

        }


        public void Insert(int where, T treeNode)
        {
            // If necessary, create a TreeNode wrapper.
            ITreeNode<T> tn = treeNode as ITreeNode<T> ?? new TreeNode<T>(treeNode);
            _children.Insert(where, tn);
        }

        public ITreeNode<T> AddNode(ITreeNode<T> tn, bool skipStructuralChecking = false)
        {
            if (_parent != null && (!skipStructuralChecking && _parent.IsChildOf(tn)))
            {
                throw new ArgumentException("Adding node " + tn.Payload + " as a child of " + _parent.Payload + " would create a circular tree structure.");
            }
            if (skipStructuralChecking || !_children.Contains(tn))
            {
#if PREANNOUNCE
                m_parent.MyEventController.OnAboutToGainChild(tn);
                tn.MyEventController.OnAboutToGainParent(m_parent);
#endif
                _children.Add(tn);
                if (_parent != null)
                {
                    _parent.Root.MyEventController.OnSubtreeChanged(SubtreeChangeType.GainedNode, tn);
                    _parent.MyEventController.OnGainedChild(tn);
                    tn.MyEventController.OnGainedParent(_parent);

                    if (!Equals(tn.Parent, _parent))
                    {
                        tn.SetParent(_parent, skipStructuralChecking, /*childAlreadyAdded =*/ true);
                    }
                }
            }
            return tn;
        }

        /// <summary>
        /// Removes the specified existing child from this collection.
        /// </summary>
        /// <param name="existingChild">The existing child node to be removed.</param>
        /// <returns>True if the removal was successful, otherwise, false.</returns>
        public bool Remove(T existingChild)
        {
            return (from node in _children where node.Payload.Equals(existingChild) select Remove(node)).FirstOrDefault();
        }

        /// <summary>
        /// Removes the specified existing child from this collection.
        /// </summary>
        /// <param name="existingChild">The existing child.</param>
        /// <returns>True if the removal was successful, otherwise, false.</returns>
        public bool Remove(ITreeNode<T> existingChild)
        {
#if PREANNOUNCE
            existingChild.MyEventController.OnAboutToLoseParent(m_parent);
            m_parent.MyEventController.OnAboutToLoseChild(existingChild);
#endif
            if (_children.Remove(existingChild))
            {
                existingChild.Parent = null;
                _parent.Root.MyEventController.OnSubtreeChanged(SubtreeChangeType.GainedNode, existingChild);
                _parent.MyEventController.OnLostChild(existingChild);
                existingChild.MyEventController.OnLostParent(_parent);
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        public ITreeNode<T> this[int index] => _children[index];

        public int IndexOf(T treeNode)
        {
            // If necessary, create a TreeNode wrapper.
            ITreeNode<T> tn = treeNode as ITreeNode<T> ?? new TreeNode<T>(treeNode);

            return _children.IndexOf(tn);
        }

        public bool Contains(T possibleChild)
        {
            ITreeNode<T> tn = possibleChild as ITreeNode<T> ?? new TreeNode<T>(possibleChild);
            return ContainsNode(tn);
        }

        public bool ContainsNode(ITreeNode<T> possibleChildNode)
        {
            return _children.Contains(possibleChildNode);
        }

        /// <summary>
        /// Gets the count of entries in this TreeNodeCollection.
        /// </summary>
        /// <value>The count.</value>
        public int Count => _children.Count;

        #region Sorting Handlers
        /// <summary>
        /// Sorts the specified list according to the provided comparison object.
        /// </summary>
        /// <param name="comparison">The comparison.</param>
        public void Sort(Comparison<ITreeNode<T>> comparison)
        {
            _children.Sort(comparison);
            _parent.MyEventController.OnChildrenResorted(_parent);
            _parent.Root.MyEventController.OnSubtreeChanged(SubtreeChangeType.ChildrenResorted, _parent);
        }

        /// <summary>
        /// Sorts the specified list according to the provided comparer implementation.
        /// </summary>
        /// <param name="comparer">The comparer.</param>
        public void Sort(IComparer<ITreeNode<T>> comparer)
        {
            _children.Sort(comparer);
            _parent.MyEventController.OnChildrenResorted(_parent);
            _parent.Root.MyEventController.OnSubtreeChanged(SubtreeChangeType.ChildrenResorted, _parent);
        }

        /// <summary>
        /// Sorts the specified list according to the provided comparer implementation.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="count">The count.</param>
        /// <param name="comparer">The comparer.</param>
        public void Sort(int index, int count, IComparer<ITreeNode<T>> comparer)
        {
            _children.Sort(index, count, comparer);
            _parent.MyEventController.OnChildrenResorted(_parent);
            _parent.Root.MyEventController.OnSubtreeChanged(SubtreeChangeType.ChildrenResorted, _parent);
        }
        #endregion

        #region IEnumerable<ITreeNode<T>> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"></see> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<ITreeNode<T>> GetEnumerator()
        {
            return ((IEnumerable<ITreeNode<T>>)_children).GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Foreaches the specified action.
        /// </summary>
        /// <param name="action">The action.</param>
        public void ForEach(Action<ITreeNode<T>> action)
        {
            _children.ForEach(action);
        }

        /// <summary>
        /// Finds all children for which the predicate returns true.
        /// </summary>
        /// <param name="match">The match.</param>
        /// <returns></returns>
        public List<ITreeNode<T>> FindAll(Predicate<ITreeNode<T>> match)
        {
            return _children.FindAll(match);
        }

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _children.GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Clears all children out of this collection.
        /// </summary>
        public void Clear()
        {
            List<ITreeNode<T>> children = new List<ITreeNode<T>>(_children);
            children.ForEach(delegate (ITreeNode<T> child)
            {
                Remove(child);
            });

        }
    }
}