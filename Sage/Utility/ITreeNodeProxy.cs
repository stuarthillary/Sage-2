/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Utility
{
    /// <summary>
    /// Implemented by an object that will implement ITreeNode, but act as a proxy
    /// for another object that actually owns the parent/child relationships.
    /// </summary>
    public interface ITreeNodeProxy : ITreeNode
    {

        /// <summary>
        /// The object that owns the parent/child relationships that this object is managing.
        /// </summary>
        object Ward
        {
            get;
        }

        /// <summary>
        /// Creates a wrapper (that implements this kind of ITreeNodeProxy) for the provided object.
        /// </summary>
        /// <param name="ward">The object that actually has the parents and children that are
        /// managed by the ITreeNodeProxy implementer.</param>
        /// <returns>The ITreeNodeProxy implementer.</returns>
        ITreeNodeProxy CreateNodeWrapper(object ward);
    }
}
