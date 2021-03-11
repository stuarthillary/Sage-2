/* This source code licensed under the GNU Affero General Public License */
using System.Collections;

namespace Highpoint.Sage.Graphs
{
    /// <summary>
    /// Summary description for IEdgeReceiptManager.
    /// </summary>
    public interface IEdgeReceiptManager
    {
        void OnPreEdgeSatisfied(IDictionary graphContext, Edge edge);
    }
}
