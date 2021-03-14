/* This source code licensed under the GNU Affero General Public License */

// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Materials.Chemistry.VaporPressure
{
    public interface IAntoinesCoefficients : IEmissionCoefficients
    {
        double GetPressure(double temperature, TemperatureUnits tu, PressureUnits resultUnits);
        double GetTemperature(double pressure, PressureUnits pu, TemperatureUnits resultUnits);
        /// <summary>
        /// Gets or sets the pressure units. Setter is ONLY for deserialization.
        /// </summary>
        /// <value>The pressure units.</value>
        PressureUnits PressureUnits
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the temperature units. Setter is ONLY for deserialization.
        /// </summary>
        /// <value>The temperature units.</value>
        TemperatureUnits TemperatureUnits
        {
            get;
            set;
        }
    }
}
