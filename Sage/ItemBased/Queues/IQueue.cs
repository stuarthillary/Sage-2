/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.SimCore;

namespace Highpoint.Sage.ItemBased.Queues
{
    public delegate void QueueLevelChangeEvent(int previous, int current, IQueue queue);
    public delegate void QueueOccupancyEvent(IQueue hostQueue, object serviceItem);
    public delegate void QueueMilestoneEvent(IQueue queue);

    public interface IQueue : IPortOwner, IModelObject
    {
        IInputPort Input
        {
            get;
        }
        IOutputPort Output
        {
            get;
        }
        int Count
        {
            get;
        }
        int MaxDepth
        {
            get;
        }
        event QueueMilestoneEvent QueueFullEvent;
        event QueueMilestoneEvent QueueEmptyEvent;
        event QueueLevelChangeEvent LevelChangedEvent;
        event QueueOccupancyEvent ObjectEnqueued;
        event QueueOccupancyEvent ObjectDequeued;
    }
}