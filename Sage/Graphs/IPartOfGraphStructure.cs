/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Graphs
{
    /// <summary>
    /// Implemented by an object that is a part of a graph structure.
    /// </summary>
    public interface IPartOfGraphStructure
    {
        /// <summary>
        /// Fired when the structure of the graph changes.
        /// </summary>
		event StructureChangeHandler StructureChangeHandler;
        //void PropagateStructureChange(object obj, StructureChangeType sct, bool isPropagated);
    }
}
