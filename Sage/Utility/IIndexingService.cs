/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Utility
{
    /// <summary>
    /// Implemented by a class that is capable of allocating &amp; assigning index slots in a population of ISupportsIndexes objects.
    /// </summary>
    public interface IIndexingService
    {
        /// <summary>
        /// Acquires a slot in the index number array for the caller's use.
        /// </summary>
        /// <param name="tgts">An array of the objects that are to be included in this index - they must all implement the ISupportsIndexes interface.</param>
        /// <returns></returns>
        uint GetIndexSlot(object[] tgts);

        /// <summary>
        /// Acquires a slot in the index number array for the caller's use.
        /// </summary>
        /// <param name="tgts">The objects for whom an index slot is desired.</param>
        /// <returns></returns>
		uint GetIndexSlot(ISupportsIndexes[] tgts);
    }
}
