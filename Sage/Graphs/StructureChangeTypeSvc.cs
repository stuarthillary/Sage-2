/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Graphs
{
    /// <summary>
	/// Implemented by events that are fired when graph structure changes.
	/// </summary>
	public delegate void StructureChangeHandler(object obj, StructureChangeType sct, bool isPropagated);

    /// <summary>
    /// A class that holds a collection of static methods which provide abstraced data about StructureChangeTypes.
    /// </summary>
	public class StructureChangeTypeSvc
    {
        private StructureChangeTypeSvc()
        {
        }
        /// <summary>
        /// Determines whether StructureChangeType was a pre-edge change.
        /// </summary>
        /// <param name="sct">The StructureChangeType.</param>
        /// <returns>true if the StructureChangeType signifies a change in a predecessor-edge.
        /// </returns>
		public static bool IsPreEdgeChange(StructureChangeType sct)
            => sct.Equals(StructureChangeType.AddPreEdge) || sct.Equals(StructureChangeType.RemovePreEdge);
        /// <summary>
        /// Determines whether StructureChangeType was a post-edge change.
        /// </summary>
        /// <param name="sct">The StructureChangeType.</param>
        /// <returns>true if the StructureChangeType signifies a change in a successor-edge.</returns>
		public static bool IsPostEdgeChange(StructureChangeType sct)
            => sct.Equals(StructureChangeType.AddPostEdge) || sct.Equals(StructureChangeType.RemovePostEdge);

        /// <summary>
        /// Determines whether the StructureChangeType signifies a co-start change.
        /// </summary>
        /// <param name="sct">The StructureChangeType.</param>
        /// <returns>true if the StructureChangeType signifies a change in a co-start.</returns>
        public static bool IsCostartChange(StructureChangeType sct)
            => sct.Equals(StructureChangeType.AddCostart) || sct.Equals(StructureChangeType.RemoveCostart);
        /// <summary>
        /// Determines whether the StructureChangeType signifies a co-finish change.
        /// </summary>
        /// <param name="sct">The StructureChangeType.</param>
        /// <returns>true if the StructureChangeType signifies a change in a co-finish.</returns>
        public static bool IsCofinishChange(StructureChangeType sct)
            => sct.Equals(StructureChangeType.AddCofinish) || sct.Equals(StructureChangeType.RemoveCofinish);
        /// <summary>
        /// Determines whether the StructureChangeType signifies a change in a child.
        /// </summary>
        /// <param name="sct">The StructureChangeType.</param>
        /// <returns>true if the StructureChangeType  signifies a change in a child.</returns>
        public static bool IsChildChange(StructureChangeType sct)
            => sct.Equals(StructureChangeType.AddChildEdge) || sct.Equals(StructureChangeType.RemoveChildEdge);
        /// <summary>
        /// Determines whether the StructureChangeType signifies a change in a synchronizer.
        /// </summary>
        /// <param name="sct">The StructureChangeType.</param>
        /// <returns>true if the StructureChangeType  signifies a change in a synchronizer.</returns>
        public static bool IsSynchronizerChange(StructureChangeType sct)
            => sct.Equals(StructureChangeType.NewSynchronizer);

        /// <summary>
        /// Determines whether the StructureChangeType signifies an addition.
        /// </summary>
        /// <param name="sct">The StructureChangeType.</param>
        /// <returns>true if the StructureChangeType  signifies an addition.</returns>
        public static bool IsAdditionChange(StructureChangeType sct)
            => sct.Equals(StructureChangeType.AddPostEdge)
                || sct.Equals(StructureChangeType.AddPreEdge)
                || sct.Equals(StructureChangeType.AddCostart)
                || sct.Equals(StructureChangeType.AddCofinish)
                || sct.Equals(StructureChangeType.NewSynchronizer)
                || sct.Equals(StructureChangeType.AddChildEdge);

        /// <summary>
        /// Determines whether the StructureChangeType signifies a removal.
        /// </summary>
        /// <param name="sct">The StructureChangeType.</param>
        /// <returns>true if the StructureChangeType  signifies a removal.</returns>
        public static bool IsRemovalChange(StructureChangeType sct)
            => sct.Equals(StructureChangeType.RemovePostEdge)
                || sct.Equals(StructureChangeType.RemovePreEdge)
                || sct.Equals(StructureChangeType.RemoveCostart)
                || sct.Equals(StructureChangeType.RemoveCofinish)
                || sct.Equals(StructureChangeType.RemoveChildEdge);
    }
}
