/* This source code licensed under the GNU Affero General Public License */
//#define PREANNOUNCE
// ReSharper disable NonReadonlyMemberInGetHashCode
// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Utility
{
    /// <summary>
    /// The types of subtree changes reported by the Tree Support classes.
    /// </summary>
    public enum SubtreeChangeType
    {
        /// <summary>
        /// A node was added somewhere in the tree.
        /// </summary>
        GainedNode,
        /// <summary>
        /// A node was removed somewhere in the tree.
        /// </summary>
        LostNode,
        /// <summary>
        /// A node's children were resorted somewhere in the tree.
        /// </summary>
        ChildrenResorted
    }
}