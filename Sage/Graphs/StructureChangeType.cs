/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Graphs
{
    /// <summary>
    /// An enumeration, the members of which describe types of structure change.
    /// </summary>
    public enum StructureChangeType
    {
        /// <summary>
        /// A post edge was added.
        /// </summary>
        AddPostEdge,
        /// <summary>
        /// A predecessor edge was added.
        /// </summary>
        AddPreEdge,
        /// <summary>
        /// A predecessor edge was removed.
        /// </summary>
        RemovePreEdge,
        /// <summary>
        /// A post edge was removed.
        /// </summary>
        RemovePostEdge,
        /// <summary>
        /// A costart was added.
        /// </summary>
        AddCostart,
        /// <summary>
        /// A co-finish was added.
        /// </summary>
        AddCofinish,
        /// <summary>
        /// A co-start was removed.
        /// </summary>
        RemoveCostart,
        /// <summary>
        /// A co-finish was removed.
        /// </summary>
		RemoveCofinish,
        /// <summary>
        /// A child edge was added.
        /// </summary>
		AddChildEdge,
        /// <summary>
        /// A child edge was removed.
        /// </summary>
        RemoveChildEdge,
        /// <summary>
        /// A new synchronizer was added.
        /// </summary>
		NewSynchronizer,
        /// <summary>
        /// An unknown change was made.
        /// </summary>
        Unknown
    }
}
