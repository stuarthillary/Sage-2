/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Graphs.PFC
{
    /// <summary>
    /// Implemented by an object that filters SFC Nodes.
    /// </summary>
    public interface IPfcNodeFilter
    {
        /// <summary>
        /// Determines whether the specified element is acceptable to be used by whomever is employing the filter.
        /// </summary>
        /// <param name="element">The element under consideration.</param>
        /// <returns>
        /// 	<c>true</c> if the specified element is acceptable; otherwise, <c>false</c>.
        /// </returns>
        bool IsAcceptable(IPfcElement element);
    }
}
