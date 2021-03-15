/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;

namespace Highpoint.Sage.Utility
{
    // TODO: Need templated treenode classes.

    /// <summary>
    /// Delegate TreeNodeInteractionEvent is used for any event on subject treenode, concerning obj treenode.
    /// </summary>
    /// <param name="subject">The subject.</param>
    /// <param name="obj">The object.</param>
    public delegate void TreeNodeInteractionEvent(ITreeNode subject, ITreeNode obj);

    /// <summary>
    /// ITreeNode is implemented by something that is a node in a tree - it has zero or one parent and
    /// zero or more children.
    /// </summary>
    public interface ITreeNode
    {
        /// <summary>
        /// True if the tree cannot be reconfigured through this implementer (no adding/removing parents or children.)
        /// </summary>
        bool IsReadOnly
        {
            get;
        }

        /// <summary>
        /// True if this implementer has no children.
        /// </summary>
        bool IsLeaf
        {
            get;
        }

        /// <summary>
        /// True if this implementer has no parent.
        /// </summary>
        bool IsRoot
        {
            get;
        }

        /// <summary>
        /// Gets the root node at or above this node.
        /// </summary>
        /// <returns>The root node at or above this node.</returns>
        ITreeNode GetRoot();

        /// <summary>
        /// The parent of this object.
        /// </summary>
        ITreeNode Parent
        {
            get; set;
        }

        /// <summary>
        /// The children of this object.
        /// </summary>
        IList Children
        {
            get;
        }

        /// <summary>
        /// Adds a child to this object.
        /// </summary>
        /// <param name="child">The child to be added to this parent.</param>
        /// <returns>The new ITreeNode that represents the child.</returns>
        ITreeNode AddChild(object child);

        /// <summary>
        /// Removes a child from this object.
        /// </summary>
        /// <param name="child">The child to be removed from this parent.</param>
        void RemoveChild(object child);

        /// <summary>
        /// Removes all children.
        /// </summary>
        void ClearChildren();

        /// <summary>
        /// Sorts children according to the supplied IComparer.
        /// </summary>
        /// <param name="sequencer">The supplied IComparer.</param>
        void ResequenceChildren(IComparer sequencer);

        /// <summary>
        /// Finds the child of this node that has the specified guid key.
        /// </summary>
        /// <param name="key">The key for the child being sought.</param>
        /// <returns>The child node that has the specified guid key.</returns>
        ITreeNode GetChild(Guid key);

        /// <summary>
        /// Produces a string representation of the entire tree below this node.
        /// </summary>
        /// <returns></returns>
        string ToStringDeep();

        /// <summary>
        /// Fires when this object is about to be removed from a parent's child list.
        /// </summary>
        event TreeNodeInteractionEvent OnAboutToBeRemoved;

        /// <summary>
        /// Fires after this object has been removed from a parent's child list.
        /// </summary>
        event TreeNodeInteractionEvent OnWasRemoved;

        /// <summary>
        /// Fires when this object is about to gain a new member of it's child list.
        /// </summary>
        event TreeNodeInteractionEvent OnAboutToGainChild;

        /// <summary>
        /// Fires after this object has gained a new member of it's child list.
        /// </summary>
        event TreeNodeInteractionEvent OnGainedChild;

        /// <summary>
        /// Fires when this object is about to lose a new member of it's child list.
        /// </summary>
        event TreeNodeInteractionEvent OnAboutToLoseChild;

        /// <summary>
        /// Fires after this object has lost a new member of it's child list.
        /// </summary>
        event TreeNodeInteractionEvent OnLostChild;
    }
}
