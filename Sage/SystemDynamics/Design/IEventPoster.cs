using System.Collections.Generic;

namespace Highpoint.Sage.SystemDynamics.Design
{
    public interface IEventPoster
    {
        double Min
        {
            get;
        }
        double Max
        {
            get;
        }
        IEnumerable<IEventPosterThreshold> Thresholds
        {
            get;
        }
    }
}
