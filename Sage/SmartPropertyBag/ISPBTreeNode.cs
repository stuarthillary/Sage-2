/* This source code licensed under the GNU Affero General Public License */
// ReSharper disable UnusedMember.Local
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable VirtualMemberNeverOverriden.Global

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// An interface that, when implemented by an element in a smart property bag,
    /// provides a hint to diagnostic routines, particularly with respect to generating output.
    /// </summary>
    public interface ISPBTreeNode {
		/// <summary>
		/// True if this entry in a SmartPropertyBag is a leaf node.
		/// </summary>
		bool IsLeaf { get; }
	}
}