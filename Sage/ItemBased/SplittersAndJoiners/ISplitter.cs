/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.SimCore;

namespace Highpoint.Sage.ItemBased.SplittersAndJoiners
{
    public interface ISplitter : IModelObject
    {
        IInputPort Input
        {
            get;
        }
        IOutputPort[] Outputs
        {
            get;
        }
    }
}