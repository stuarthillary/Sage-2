/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.Materials.Chemistry.VaporPressure
{

    /// <summary>
    /// Extended Antoine Coefficients are always, and only, specified in °C and mmHg.
    /// </summary>
	public class AntoinesCoefficientsExt : IAntoinesCoefficientsExt
    {

        #region Public Constants
        public static readonly double DEFAULT_C3 = 0.0;
        public static readonly double DEFAULT_C4 = 0.0;
        public static readonly double DEFAULT_C5 = 0.0;
        public static readonly double DEFAULT_C6 = 0.0;
        public static readonly double DEFAULT_C7 = 0.0;
        public static readonly double DEFAULT_C8 = 0.0;
        public static readonly double DEFAULT_C9 = 1000.0;
        #endregion

        #region Private Fields
        private readonly double _c1;
        private readonly double _c2;
        private readonly double _c3 = DEFAULT_C3;
        private readonly double _c4 = DEFAULT_C4;
        private readonly double _c5 = DEFAULT_C5;
        private readonly double _c6 = DEFAULT_C6;
        private readonly double _c7 = DEFAULT_C7;
        private readonly double _c8 = DEFAULT_C8;
        private readonly double _c9 = DEFAULT_C9;
        private PressureUnits _pu;
        private TemperatureUnits _tu;
        #endregion

        #region Constructors
        public AntoinesCoefficientsExt()
        {
            _pu = PressureUnits.mmHg;
            _tu = TemperatureUnits.Celsius;
            _c1 = Double.NaN;
            _c2 = Double.NaN;
        }

        public AntoinesCoefficientsExt(double c1, double c2, PressureUnits pu, TemperatureUnits tu)
        {
            _pu = pu;
            _tu = tu;
            _c1 = c1;
            _c2 = c2;
        }

        public AntoinesCoefficientsExt(double c1, double c2, double c3, double c4, double c5, double c6, double c7, double c8, double c9, PressureUnits pu, TemperatureUnits tu)
        {
            _pu = pu;
            _tu = tu;
            _c1 = c1;
            _c2 = c2;
            if (!double.IsNaN(c3))
                _c3 = c3;
            if (!double.IsNaN(c4))
                _c4 = c4;
            if (!double.IsNaN(c5))
                _c5 = c5;
            if (!double.IsNaN(c6))
                _c6 = c6;
            if (!double.IsNaN(c7))
                _c7 = c7;
            if (!double.IsNaN(c8))
                _c8 = c8;
            if (!double.IsNaN(c9))
                _c9 = c9;
        }
        #endregion

        #region IEmissionCoefficients Members
        public bool IsSufficientlySpecified(double temperature)
        {
            // c8 & c9 are in degrees C
            double ctemp = temperature + Constants.KELVIN_TO_CELSIUS;
            return (ctemp > _c8 && ctemp < _c9) && !(Double.IsNaN(_c1) || Double.IsNaN(_c2));
        }
        #endregion

        #region IAntoinesCoefficientsExt Members

        public Double C1
        {
            get
            {
                return _c1;
            }
        }

        public Double C2
        {
            get
            {
                return _c2;
            }
        }

        public Double C3
        {
            get
            {
                return _c3;
            }
        }

        public Double C4
        {
            get
            {
                return _c4;
            }
        }

        public Double C5
        {
            get
            {
                return _c5;
            }
        }

        public Double C6
        {
            get
            {
                return _c6;
            }
        }

        public Double C7
        {
            get
            {
                return _c7;
            }
        }

        public Double C8
        {
            get
            {
                return _c8;
            }
        }

        public Double C9
        {
            get
            {
                return _c9;
            }
        }

        #endregion

        #region IAntoinesCoefficients Members

        public double GetPressure(double temperature, TemperatureUnits tu, PressureUnits resultUnits)
        {
            double retval = double.NaN;
            temperature = ConvertTemperature(temperature, tu, TemperatureUnits.Kelvin);
            if (IsSufficientlySpecified(temperature))
            {
                double pressure = Math.Exp(C1 + (C2 / (temperature + C3)) + (C4 * temperature)
                    + (C5 * Math.Log(temperature, Math.E)) + (C6 * Math.Pow(temperature, C7)));

                retval = ConvertPressure(pressure, PressureUnits.Pascals, resultUnits);
            }
            return retval;
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

        public double GetTemperature(double pressure, PressureUnits pu, TemperatureUnits resultUnits)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

