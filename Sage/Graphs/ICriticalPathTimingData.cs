/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.Graphs
{
    public interface ICriticalPathTimingData
    {
        DateTime EarlyStart
        {
            get;
        }
        DateTime LateStart
        {
            get;
        }
        DateTime EarlyFinish
        {
            get;
        }
        DateTime LateFinish
        {
            get;
        }
        bool IsCritical
        {
            get;
        }
        double Criticality
        {
            get;
        }
    }


}
