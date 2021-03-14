/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Mathematics;
using Highpoint.Sage.SimCore;
using System;

namespace Highpoint.Sage.ItemBased
{

    public class Periodicity : IPeriodicity
    {
        public enum Units
        {
            Seconds,
            [DefaultValue] Minutes,
            Hours,
            Days
        };

        private double _seconds;
        private IDoubleDistribution _distribution;
        public Periodicity(IDoubleDistribution distribution, Units units)
        {
            _distribution = distribution;
            SetPeriod(distribution, ((long)(TimeSpan.TicksPerSecond * SecondsFromUnits(units))));
        }

        private double SecondsFromUnits(Units units)
        {
            double retval = 0.0;
            switch (units)
            {
                case Units.Minutes:
                    retval = 60.0;
                    break;
                case Units.Hours:
                    retval = 3600.0;
                    break;
                case Units.Days:
                    retval = 8640.0;
                    break;
            }

            return retval;
        }

        public Periodicity(IDoubleDistribution distribution, long ticks)
        {
            SetPeriod(distribution, ticks);
        }

        public void SetPeriod(IDoubleDistribution distribution, long ticks)
        {
            _seconds = TimeSpan.FromTicks(ticks).TotalSeconds;
            _distribution = distribution;
        }

        public TimeSpan GetNext()
        {
            double d = _distribution.GetNext();
            return TimeSpan.FromSeconds(d * _seconds);

        }
    }
}