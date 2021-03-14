/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.SimCore;
using System;

namespace Highpoint.Sage.ItemBased.SplittersAndJoiners
{
    /// <summary>
    /// This joiner places anything that appears on any of its input ports, onto
    /// its output port. Pulls and Peeks are not permitted, and if the downstream
    /// entity rejects the push, the (upstream) provider's push will be refused.
    /// </summary>
    public class PushJoiner : Joiner
    {
        public PushJoiner(IModel model, string name, Guid guid, int nIns) : base(model, name, guid, nIns) { }
        protected override DataArrivalHandler GetDataArrivalHandler(int i)
        {
            return new DataArrivalHandler(OnDataArrived);
        }
        protected override DataProvisionHandler GetPeekHandler()
        {
            return null;
        }
        protected override DataProvisionHandler GetTakeHandler()
        {
            return null;
        }
        protected bool OnDataArrived(object data, IInputPort ip)
        {
            return output.OwnerPut(data);
        }
    }
}