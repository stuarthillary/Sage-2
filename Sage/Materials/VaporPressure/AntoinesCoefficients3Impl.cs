/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.Materials.Chemistry.VaporPressure
{
    /// <summary>
    /// Antoine's coefficients which are expressed in °C and mmHg.
    /// </summary>
    public class AntoinesCoefficients3Impl : IAntoinesCoefficients3
    {

        #region Private Fields
        private readonly double _a;
        private readonly double _b;
        private readonly double _c;
        private PressureUnits _pu;
        private TemperatureUnits _tu;
        #endregion

        #region Constructors
        public AntoinesCoefficients3Impl()
        {
            _a = double.NaN;
            _b = double.NaN;
            _c = double.NaN;
            _pu = PressureUnits.mmHg;
            _tu = TemperatureUnits.Celsius;

        }

        public AntoinesCoefficients3Impl(double a, double b, double c, PressureUnits spu, TemperatureUnits stu)
        {
            _a = a;
            _b = b;
            _c = c;
            _pu = PressureUnits.mmHg;
            _tu = TemperatureUnits.Celsius;
        }

        #endregion

        #region IEmissionCoefficients Members
        public bool IsSufficientlySpecified(double temperature)
        {
            return !(Double.IsNaN(_a) || Double.IsNaN(_b) || Double.IsNaN(_c));
        }
        #endregion

        #region IAntoinesCoefficients Members

        public double GetPressure(double temperature, TemperatureUnits tu, PressureUnits resultUnits)
        {
            temperature = ConvertTemperature(temperature, tu, _tu);
            double pressure = Math.Pow(10, (A - (B / (temperature + C))));
            return ConvertPressure(pressure, _pu, resultUnits);
        }

        public double GetTemperature(double pressure, PressureUnits pu, TemperatureUnits resultUnits)
        {
            pressure = ConvertPressure(pressure, pu, _pu);
            double temperature = B / (A - Math.Log10(pressure)) - C;
            return ConvertTemperature(temperature, _tu, resultUnits);
        }



        private double ConvertPressure(double pressure, PressureUnits srcUnits, PressureUnits resultUnits)
        {
            if (srcUnits != resultUnits)
            {
                switch (srcUnits)
                { // Convert it to mmHg : ref: http://physics.nist.gov/Pubs/SP811/appenB9.html#PRESSURE
                    case PressureUnits.Bar:
                        pressure *= (133.3224/*mmHg-per-Pascal*/ / 100000/*Bar-per-Pascal*/ );
                        break;
                    case PressureUnits.mmHg:
                        break;
                    case PressureUnits.Atm:
                        pressure *= (101325/*Atm-per-Pascal*/ / 133.3224/*mmHg-per-Pascal*/);
                        break;
                    case PressureUnits.Pascals:
                        pressure /= 133.3224/*mmHg-per-Pascal*/;
                        break;
                }
                switch (resultUnits)
                {// Convert it from mmHg
                    case PressureUnits.Bar:
                        pressure /= (133.3224/*mmHg-per-Pascal*/ / 100000/*Bar-per-Pascal*/ );
                        break;
                    case PressureUnits.mmHg:
                        break;
                    case PressureUnits.Atm:
                        pressure /= (101325/*Atm-per-Pascal*/ / 133.3224/*mmHg-per-Pascal*/);
                        break;
                    case PressureUnits.Pascals:
                        pressure *= 133.3224/*mmHg-per-Pascal*/;
                        break;
                }
            }
            return pressure;
        }

        private double ConvertTemperature(double temperature, TemperatureUnits srcUnits, TemperatureUnits resultUnits)
        {
            if (srcUnits != resultUnits)
            {
                switch (srcUnits)
                { // Convert to celsius
                    case TemperatureUnits.Celsius:
                        break;
                    case TemperatureUnits.Kelvin:
                        temperature -= 273.15;
                        break;
                }
                switch (resultUnits)
                { // Convert from celsius
                    case TemperatureUnits.Celsius:
                        break;
                    case TemperatureUnits.Kelvin:
                        temperature += 273.15;
                        break;
                }
            }
            return temperature;
        }

        /// <summary>
        /// Gets or sets the pressure units. Setter is ONLY for deserialization.
        /// </summary>
        /// <value>The pressure units.</value>
        public PressureUnits PressureUnits
        {
            get
            {
                return _pu;
            }
            set
            {
                _pu = value;
            }
        }

        /// <summary>
        /// Gets or sets the temperature units. Setter is ONLY for deserialization.
        /// </summary>
        /// <value>The temperature units.</value>
        public TemperatureUnits TemperatureUnits
        {
            get
            {
                return _tu;
            }
            set
            {
                _tu = value;
            }
        }

        #endregion

        #region IAntoinesCoefficients3 Members

        public Double A
        {
            get
            {
                return _a;
            }
        }

        public Double B
        {
            get
            {
                return _b;
            }
        }

        public Double C
        {
            get
            {
                return _c;
            }
        }

        #endregion

    }
}

