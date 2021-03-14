/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.Materials.Thermodynamics
{
    /// <summary>
    /// Describes a temperature ramp rate in degrees kelvin per time period. Note that
    /// since the degreesKelvin parameter describes a delta-T rather than an absolute,
    /// degrees Celsius per time period as well. Only degrees Fahrenheit per time period
    /// is wrong.
    /// </summary>
    public class TemperatureRampRate
    {
        private double _degreesKelvin;
        private TimeSpan _perTimePeriod;
        /// <summary>
        /// Creates a temperature ramp rate object representing a ramp rate of a specified
        /// number of degrees kelvin (or celsius) per given time period.
        /// </summary>
        /// <param name="degreesKelvin">The number of degrees kelvin per the specified time period.</param>
        /// <param name="perTimePeriod">The time period over which the specified temperature change takes place.</param>
        public TemperatureRampRate(double degreesKelvin, TimeSpan perTimePeriod)
        {
            _degreesKelvin = degreesKelvin;
            _perTimePeriod = perTimePeriod;
        }
        /// <summary>
        /// The number of degrees kelvin per the specified time period.
        /// </summary>
        public double DegreesKelvin
        {
            get
            {
                return _degreesKelvin;
            }
            set
            {
                _degreesKelvin = value;
            }
        }
        /// <summary>
        /// The time period over which the specified temperature change takes place.
        /// </summary>
        public TimeSpan PerTimePeriod
        {
            get
            {
                return _perTimePeriod;
            }
            set
            {
                _perTimePeriod = value;
            }
        }

        public override string ToString()
        {
            return string.Format("{0} degrees K per second", _degreesKelvin / _perTimePeriod.TotalSeconds);
        }
    }
}
