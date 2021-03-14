/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.ItemBased.Ports;

namespace Highpoint.Sage.ItemBased.Channels
{
    public interface IRoute
    {
        IInputPort Entry
        {
            get;
        }
        IOutputPort Exit
        {
            get;
        }
    }
}
