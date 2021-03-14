/* This source code licensed under the GNU Affero General Public License */

using System.Collections;

namespace Highpoint.Sage.ItemBased.Queues
{
    public interface ISelectionStrategy
    {
        object GetNext(object context);
        ICollection Candidates
        {
            get; set;
        }
    }
}