using System.Collections.Generic;

namespace Highpoint.Sage.SystemDynamics.Design
{
    public interface IEventPosterThreshold
    {
        IEnumerable<IEventPosterThresholdEvent> Events
        {
            get;
        }
        double Value
        {
            get;
        }
        EventPosterThresholdDirection Direction
        {
            get;
        }
        EventPosterThresholdRepeat Repeat
        {
            get;
        }
    }
}
