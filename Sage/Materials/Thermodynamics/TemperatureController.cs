/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Materials.Chemistry;
using Highpoint.Sage.Mathematics;
using Highpoint.Sage.SimCore;
using System;
using System.Collections.Generic;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Materials.Thermodynamics
{
    public class TemperatureController : ITemperatureController
    {

        /// <summary>
        /// if precision is set to 0.01, then the mixture will be considered
        /// to have reached its setpoint when at 78.99, or at 79.01.
        /// </summary>
        public static double DefaultPrecision = 0.01;

        #region >>> Private Fields <<<
        private static readonly bool diagnostics = Diagnostics.DiagnosticAids.Diagnostics("TemperatureController");
        private string _lastMessage = null;
        private static TemperatureControllerMode defaultTcMode = TemperatureControllerMode.ConstantT;
        private readonly IContainer _container;
        private readonly SmallDoubleInterpolable _thCond;
        private readonly SmallDoubleInterpolable _thCondAmb;
        private bool _tcEnabled;
        private double _tcSetpoint;
        private double _tcSourceTemp;
        private double _tcMinSourceTemp;
        private double _tcMaxSourceTemp;
        private double _tcDelta;
        private double _ambientTemp;
        //private double m_errorBand;
        private TemperatureControllerMode _tcMode;
        private TemperatureRampRate _temperatureRampRate;
        private double _precision = DefaultPrecision;
        #endregion

        /// <summary>
        /// The temperature controller's precision is a measure of how close to the setpoint the mixture
        /// needs to be in actuality before the controller will consider the mixture to have reached the
        /// setpoint. This is necessary in order to represent the physical case where the temperature
        /// controller mode is at, say, a constant 79 degrees, and the setpoint is also at 79 degrees -
        /// theoretically, the mixture, unless already at precisely 79 degrees, will not reach the desired
        /// setpoint temperature. However, if precision is set to 0.01, then the mixture will be considered
        /// to have reached its setpoint when at 78.99, or at 79.01, if being driven up from below, or 
        /// down from above, respectively. This value defaults to the value of TemperatureController.DEFAULT_PRECISION,
        /// which can itself be changed, but starts at 0.01.
        /// </summary>
        public double Precision
        {
            get
            {
                return _precision;
            }
            set
            {
                _precision = value;
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="T:TemperatureController"/> class.
        /// </summary>
        /// <param name="icontainer">The container on which this <see cref="T:TemperatureController"/> operates.</param>
        public TemperatureController(IContainer icontainer)
        {
            _container = icontainer;
            _thCond = new SmallDoubleInterpolable(4); // (0,0) plus the 30%, 60%, and 90% values.
            _thCondAmb = new SmallDoubleInterpolable(4);
            _tcEnabled = false;
            _tcSetpoint = double.NaN;
            _ambientTemp = double.NaN;
            _tcDelta = double.NaN;
            _tcMode = defaultTcMode;
            _thCond.SetYValue(0, 0);
            _thCondAmb.SetYValue(0, 0);
            _temperatureRampRate = new TemperatureRampRate(1, TimeSpan.FromMinutes(1));
            _tcSourceTemp = 0.0;
            _tcMinSourceTemp = -20.0;
            _tcMaxSourceTemp = 120.0;
        }

        #region Implementation of ITemperatureController

        /// <summary>
        /// The temperature control system will predict the amount of time required for it to reach
        /// the setpoint temperature (plus or minus the temperature controller's precision) in the
        /// mixture. <para><B>This will throw an exception if the system cannot ever drive the mixture
        /// to the target temperature.</B></para>
        /// </summary>
        /// <returns></returns>
        public TimeSpan TimeNeededToReachTargetTemp()
        {

            Mixture mixture = _container.Mixture;

            if (mixture.Mass == 0.0)
            {
                if (diagnostics)
                {
                    _lastMessage = "Computing time to reach target temperature, but the mixture mass was zero, so the computed time is 0.0 seconds.";
                    _Debug.WriteLine(_lastMessage);
                }
                return TimeSpan.Zero;
            }

            if (mixture.Temperature == TCSetpoint)
            {
                if (diagnostics)
                {
                    _lastMessage = "Computing time to reach target temperature, but the mixture is already at the setpoint, so the computed time is 0.0 seconds.";
                    _Debug.WriteLine(_lastMessage);
                }
                return TimeSpan.Zero;
            }

            if (TCSetpoint < TCMinSrcTemperature)
            {
                throw new ThermalRangeEndSpecificationException(this, ThermalRangeEndSpecificationException.RangeEndError.Low);
            }

            if (TCSetpoint > TCMaxSrcTemperature)
            {
                throw new ThermalRangeEndSpecificationException(this, ThermalRangeEndSpecificationException.RangeEndError.High);
            }

            // First gather the relevant pieces of data.

            double tMix = mixture.Temperature;  // The mixture temperature.
            double cMix = mixture.SpecificHeat; // The mixture specific heat.

            double tSrc = GetTSrc(tMix);

            //                                  // The heat conductance appropriate to the vessel level.
            double pctFull = (mixture.Volume / _container.Capacity);
            double kMix = _tcEnabled ? _thCond.GetYValue(pctFull) : _thCondAmb.GetYValue(pctFull);

            double mMix = mixture.Mass;        // The mass of mixture.

            double nSeconds = 0;

            // Only if the source temperature is outside the m_precision of the mixture temperature or if we are
            // in Constant_Ramp_Rate mode, do we need to calculate the number of seconds it will take to reach
            // the target temperature. Otherwise we can assume that it will be zero seconds.
            if (Math.Abs(tSrc - tMix) > _precision || _tcMode.Equals(TemperatureControllerMode.Constant_RampRate))
            {
                if (!_tcEnabled)
                {
                    // Equivalent to constant T, with tcEnabled = true.
                    nSeconds = (mMix * cMix / kMix) * Math.Log((tMix - tSrc) / (_tcSetpoint - tSrc));
                }
                else
                {
                    if (_tcMode.Equals(TemperatureControllerMode.ConstantDeltaT))
                    {
                        nSeconds = ((TCSetpoint - tMix) * mMix * cMix) / (kMix * (tSrc - tMix));
                    }
                    else if (_tcMode.Equals(TemperatureControllerMode.ConstantT))
                    {
                        nSeconds = (mMix * cMix / kMix) * Math.Log((tMix - tSrc) / (_tcSetpoint - tSrc));
                    }
                    else if (_tcMode.Equals(TemperatureControllerMode.Constant_RampRate))
                    {
                        double rampRatePeriodInSeconds = _temperatureRampRate.PerTimePeriod.Ticks /
                                                         TimeSpan.TicksPerSecond;
                        nSeconds = (TCSetpoint - tMix) * rampRatePeriodInSeconds / _temperatureRampRate.DegreesKelvin;
                        nSeconds = Math.Abs(nSeconds);
                    }
                    else
                    {
                        // tcmode is unknown.
                        throw new ApplicationException("Temperature control system is in unknown mode " + _tcMode);
                    }
                }
            } // else we're already AT the target temperature, so ZERO is the right answer.

            TimeSpan retval = TimeSpan.MaxValue;
            if (Double.IsInfinity(nSeconds) || double.IsNaN(nSeconds))
            {
                if (_tcEnabled)
                {
                    if (_tcMode.Equals(TemperatureControllerMode.Constant_RampRate))
                    {
                        throw new IncalculableTimeToSetpointException(this, nSeconds, kMix, tSrc);
                    }
                    else
                    {
                        throw new IncalculableTimeToSetpointException(this, nSeconds, kMix, tSrc);
                    }
                }
            }
            else
            {
                retval = TimeSpan.FromSeconds(nSeconds);
            }

            #region >>> Create a diagnostic message <<<
            if (diagnostics)
            {
                string technique;

                if (_tcMode.Equals(TemperatureControllerMode.ConstantDeltaT))
                {
                    technique = " a constant delta T of " + (tSrc - tMix) + " degrees C ";
                }
                else if (_tcMode.Equals(TemperatureControllerMode.ConstantT))
                {
                    technique = " a driving temperature of " + tSrc + " degrees C ";
                }
                else if (_tcMode.Equals(TemperatureControllerMode.Constant_RampRate))
                {
                    technique = " a constant ramp rate of " + (_temperatureRampRate.PerTimePeriod.Ticks / TimeSpan.TicksPerSecond) + " degrees C per second ";
                }
                else
                { // tcmode is unknown.
                    technique = " an unknown method ";
                }

                string kRange = string.Format("active = [@0%:{0:F1}/@30%:{1:F1}/@60%:{2:F1}/@90%:{3:F1}/@100%:{4:F1}], ambient = [@0%:{5:F1}/@30%:{6:F1}/@60%:{7:F1}/@90%:{8:F1}/@100%:{9:F1}]",
                    _thCond.GetYValue(0.0), _thCond.GetYValue(0.3), _thCond.GetYValue(0.6), _thCond.GetYValue(0.9), _thCond.GetYValue(1.0),
                    _thCondAmb.GetYValue(0.0), _thCondAmb.GetYValue(0.3), _thCondAmb.GetYValue(0.6), _thCondAmb.GetYValue(0.9), _thCondAmb.GetYValue(1.0));

                _lastMessage = string.Format("Computing time to reach target temperature {0}.\r\n"
                    + "\tVessel is {1:F2}% full, and therefore has a heat capacity of {2:F1} Watts per second.\r\nVessel heat capacities are {8}\r\n"
                    + "\tMixture mass is {3:F1} kg, temperature is {4:F1} deg C, and it has a specific heat of {5:F1} Joules per Kilogram-degree Kelvin\r\n"
                    + "\tUsing {6} to reach the target temperature will require {7}.",
                    TCSetpoint, ((pctFull * 100)), kMix, mMix, tMix, cMix, technique, TimeSpan.FromSeconds(nSeconds), kRange);

                _Debug.WriteLine(_lastMessage);
            }
            #endregion

            return retval;
        }

        private double GetTSrc(double mixtureTemp)
        {
            if (!_tcEnabled)
                return _ambientTemp;

            if (_tcMode.Equals(TemperatureControllerMode.ConstantDeltaT))
            {
                if (_tcSetpoint > mixtureTemp)
                {
                    return mixtureTemp + _tcDelta;
                }
                else if (_tcSetpoint < mixtureTemp)
                {
                    return mixtureTemp - _tcDelta;
                }
                else
                {
                    return mixtureTemp;
                }
            }
            else
            {
                return _tcSourceTemp;
            }
        }

        /// <summary>
        /// Changes the mixture temperature relative to the current parameters,
        /// over the specified timespan. Assumes no changes in volume, etc, but
        /// adjusts TSetpoint afterwards if the system is in constant delta-T mode.
        /// </summary>
        /// <param name="elapsedSinceLastUpdate">How much time has elapsed since the last time this method was called.</param>
        public void ImposeEffectsOfDuration(TimeSpan elapsedSinceLastUpdate)
        {

            Mixture mixture = _container.Mixture;

            double mMix = mixture.Mass;                             // The mass of mixture.
            if (mMix == 0.0)
            {
                mixture.Temperature = _tcSetpoint;
                return; // No mass, we can reach any temperature instantly.
            }
            if (elapsedSinceLastUpdate.Ticks == 0)
                return;		// No time, and non-zero mass, we can't go anywhere.

            // First gather the relevant pieces of data.
            double tMix = mixture.Temperature;                      // The mixture temperature.
            double cMix = mixture.SpecificHeat;                     // The mixture specific heat.

            if (cMix == 0)
                throw new ApplicationException("Mixture's 'specific heat' property is zero. This is in error.");

            double tSrc = GetTSrc(tMix);

            //                                                      // The heat conductance appropriate to the vessel level.
            double pctFull = (mixture.Volume / _container.Capacity);
            double kMix = _tcEnabled ? _thCond.GetYValue(pctFull) : _thCondAmb.GetYValue(pctFull);

            if (double.IsNaN(kMix))
            {
                ThermalConductanceSpecificationException.ThermalDriveType driveType =
                    _tcEnabled ?
                    ThermalConductanceSpecificationException.ThermalDriveType.Driven :
                    ThermalConductanceSpecificationException.ThermalDriveType.Ambient;
                throw new ThermalConductanceSpecificationException(this, driveType);
            }

            // Now calculate for this delta-t, the hypothetical resultant mixture temperature.
            double tMixFinal;
            double t = elapsedSinceLastUpdate.TotalSeconds;
            if (_tcMode.Equals(TemperatureControllerMode.ConstantDeltaT) && TCEnabled)
            {
                tMixFinal = tMix + (kMix * (tSrc - tMix) * t) / (mMix * cMix);
            }
            else if (_tcMode.Equals(TemperatureControllerMode.ConstantT) || !TCEnabled)
            {
                tMixFinal = tMix + ((tSrc - tMix) * (1 - Math.Exp(-(kMix * t) / (mMix * cMix))));
            }
            else if (_tcMode.Equals(TemperatureControllerMode.Constant_RampRate) && TCEnabled)
            {
                double deltaTemp = _tcSetpoint - tMix;
                double timeToSetpointInSeconds = TimeNeededToReachTargetTemp().TotalSeconds;
                if (timeToSetpointInSeconds == 0.0)
                {
                    tMixFinal = tMix;
                }
                else
                {
                    tMixFinal = tMix + (deltaTemp * (t / timeToSetpointInSeconds));
                }
            }
            else
            { // tcmode is unknown.
                throw new ApplicationException("Temperature control system is in unknown mode " + _tcMode);
            }

            // If we've got temp ctrl enabled, make sure we haven't passed the setpoint. If we have,
            // then we set tMixFinal to a random temperature within the error band. We leave m_tcSetpoint
            // where it is, since a reaction or influent mass could change temperature, requiring a new
            // adjustment.
            if (TCEnabled)
            {
                if (tMixFinal > tMix)
                { // We raised temperature
                    if (tMixFinal > TCSetpoint)
                    { // And we overshot.
                        tMixFinal = _tcSetpoint;
                    }
                    if (!_tcMode.Equals(TemperatureControllerMode.ConstantT))
                        TCSrcTemperature = tMixFinal + TCSrcDelta;
                }
                else
                { // We lowered temperature.
                    if (tMixFinal < TCSetpoint)
                    { // And we undershot.
                        tMixFinal = _tcSetpoint;
                    }
                    if (!_tcMode.Equals(TemperatureControllerMode.ConstantT))
                        TCSrcTemperature = tMixFinal - TCSrcDelta;
                }
            }

            mixture.Temperature = tMixFinal;

        }

        /// <summary>
        /// Replaces a thermal conductance value already in the controller.
        /// </summary>
        /// <param name="level">The tank level associated with the newly-specified thermal conductance.</param>
        /// <param name="val">The new thermal conductance value.</param>
        public void SetThermalConductance(double level, double val)
        {
            _thCond.SetYValue(level, val);
        }

        /// <summary>
        /// Gets the value of thermal conductance between the compartment containing the
        /// heating/cooling medium and the compartment containing the mixture, when a
        /// mixture fills a vessel to the level specified. This value is a linear interpolation
        /// based on the discrete data points provided.
        /// </summary>
        /// <param name="level">The percentage full (0.30 is 30%) that the vessel is, at the data point of interest.</param>
        /// <returns>
        /// The value of the specified thermal conductance.
        /// </returns>
        public double GetThermalConductance(double level)
        {
            return _thCond.GetYValue(level);
        }

        /// <summary>
        /// This sets a value for thermal conductance between the outside environment (ambient)
        /// and the compartment containing the mixture in a vessl at a certain level. For example,
        /// SetAmbientThermalConductance(0.50,20) sets the thermal conductance to 20 Watts per degree
        /// kelvin difference between the outside air and the mixture, when the mixture fills the
        /// vessel to it's '30% FULL' line.
        /// </summary>
        /// <param name="level">Describes at what level (0.30 equates to 30% FULL) the datum is correct.</param>
        /// <param name="val">Thermal conductance, in Watts per degree kelvin.</param>
        public void SetAmbientThermalConductance(double level, double val)
        {
            _thCondAmb.SetYValue(level, val);
        }

        /// <summary>
        /// Gets the value of thermal conductance between the outside environment (ambient)
        /// and the compartment containing the mixture, when a mixture fills a vessel to the
        /// level specified. This value is a linear interpolation based on the discrete data
        /// points provided.
        /// </summary>
        /// <param name="level">The percentage full (0.30 is 30%) that the vessel is, at the
        /// data point of interest.</param>
        /// <returns>
        /// The value of the specified thermal conductance.
        /// </returns>
        public double GetAmbientThermalConductance(double level)
        {
            return _thCondAmb.GetYValue(level);
        }

        /// <summary>
        /// Sets and gets a boolean that represents whether the Temperature Control System is
        /// enabled (true - the TCSrcTemperature is relevant, but ambient temperature is ignored,
        /// or false - TCSrcTemperature is ignored, and temperature is allowed to drif toward ambient.
        /// </summary>
        /// <value></value>
        public bool TCEnabled
        {
            get
            {
                return _tcEnabled;
            }
            set
            {
                _tcEnabled = value;
            }
        }

        /// <summary>
        /// The target temperature for the temperature control system. This is the temperature that
        /// the system will seek and maintain (+/- the specified error band), if it is enabled.
        /// </summary>
        /// <value></value>
        public double TCSetpoint
        {
            get
            {
                return _tcSetpoint;
            }
            set
            {
                _tcSetpoint = value;
            }
        }

        /// <summary>
        /// This is the temperature of the heating/cooling medium for the system, if it is in constant_T mode.
        /// </summary>
        /// <value></value>
        public double TCSrcTemperature
        {
            get
            {
                return _tcSourceTemp;
            }
            set
            {
                _tcSourceTemp = value;
            }
        }

        /// <summary>
        /// This is the maximum temperature of the heating/cooling medium for the system, important in constant
        /// delta T and constant ramp rate modes.
        /// </summary>
        public double TCMaxSrcTemperature
        {
            get
            {
                return _tcMaxSourceTemp;
            }
            set
            {
                _tcMaxSourceTemp = value;
            }
        }

        /// <summary>
        /// This is the minimum temperature of the heating/cooling medium for the system, important in constant
        /// delta T and constant ramp rate modes.
        /// </summary>
        public double TCMinSrcTemperature
        {
            get
            {
                return _tcMinSourceTemp;
            }
            set
            {
                _tcMinSourceTemp = value;
            }
        }

        /// <summary>
        /// This is the difference in temperature between the heating/cooling medium and the mixture, if the
        /// system is in constant_DeltaT mode.
        /// </summary>
        /// <value></value>
        public double TCSrcDelta
        {
            get
            {
                return _tcDelta;
            }
            set
            {
                _tcDelta = value;
            }
        }

        /// <summary>
        /// This is the "outside temperature", for example, the atmospheric temperature in the plant.
        /// </summary>
        /// <value></value>
        public double AmbientTemperature
        {
            get
            {
                return _ambientTemp;
            }
            set
            {
                _ambientTemp = value;
            }
        }

        /// <summary>
        /// The mode of the system (Constant delta-T, constant TSrc or Constant_RampRate).
        /// </summary>
        /// <value></value>
        public TemperatureControllerMode TCMode
        {
            get
            {
                return _tcMode;
            }
            set
            {
                _tcMode = value;
            }
        }

        ///// <summary>
        ///// The acceptable deviation from Tset, plus or minus, of the mixture 
        ///// temperature at final steady-state conditions. If this is 3.0, and
        ///// Tset is 20.0, then the temperature control system will have a dead
        ///// band from 34.0 to 40.0.
        ///// </summary>
        //public double ErrorBand
        //{
        //    get
        //    {
        //        return m_errorBand;
        //    }
        //    set
        //    {
        //        m_errorBand = value;
        //    }
        //}

        /// <summary>
        /// This is the ramp rate that the temperature control system will maintain if it is set to
        /// Constant_RampRate mode. Note that it does not have meaning if the temperature control system 
        /// is not set to Constant_RampRate mode. It defaults to 0 degrees per minute.
        /// </summary>
        public TemperatureRampRate TCTemperatureRampRate
        {
            set
            {
                _temperatureRampRate = value;
            }
            get
            {
                return _temperatureRampRate;
            }
        }

        /// <summary>
        /// Returns the current power available from the temperature controller, given existing settings
        /// of TCMode, Tsrc, Tambient, thermal conductivity and mixture levels. Positive power implies
        /// ability to heat a mixture, negative power implies ability to cool. If the Temperature control
        /// mode is Constant_DeltaT, then the power will be positive, even though the temperature controller
        /// could heat or cool a mixture with equivalent power.
        /// </summary>
        /// <returns>Thermal power currrently available, in watts.</returns>
        public double GetCurrentThermalPower()
        {
            double pctFull = (_container.Mixture.Volume / _container.Capacity);
            return GetThermalPower(pctFull);
        }

        private double GetThermalPower(double pctFull)
        {

            Mixture mixture = _container.Mixture;
            double mMix = mixture.Mass;
            double power;
            // Power is going to be thermal conductance (W/degK) x Temperature Difference if the difference
            // is known (all but the constant ramp rate). With Constant Ramp Rate, power will be Specific
            // Heat * ramp rate (degK/Sec) * mass.

            if (!TCEnabled)
            {
                power = GetAmbientThermalConductance(pctFull) * (AmbientTemperature - mixture.Temperature);
            }
            else if (_tcMode.Equals(TemperatureControllerMode.ConstantDeltaT))
            {
                power = GetThermalConductance(pctFull) * TCSrcDelta;
            }
            else if (_tcMode.Equals(TemperatureControllerMode.ConstantT))
            {
                power = GetThermalConductance(pctFull) * (TCSrcTemperature - mixture.Temperature);
            }
            else if (_tcMode.Equals(TemperatureControllerMode.Constant_RampRate))
            {
                power = mixture.SpecificHeat * TCTemperatureRampRate.DegreesKelvin * mMix / TCTemperatureRampRate.PerTimePeriod.TotalSeconds;
            }
            else
            { // tcmode is unknown.
                throw new ApplicationException("Temperature control system is in unknown mode " + _tcMode);
            }

            return power;

        }

        #region Temperature Controller Settings Validation API
        /// <summary>
        /// Validates the current settings of this Temperature Controller. If warnings and errors are encountered,
        /// then the method adds those errors and warnings to the model. If the model reference is null, then this
        /// method ignores warnings and throws an exception with the first error encountered.
        /// </summary>
        /// <param name="model">The model in whose context this temperature controller is running.</param>
        public void Validate(IModel model)
        {

            #region Look for a nonsensical Temperature Ramp Rate
            if (_temperatureRampRate.DegreesKelvin == 0.0 ||
                double.IsInfinity(_temperatureRampRate.PerTimePeriod.TotalSeconds) ||
                double.IsNaN(_temperatureRampRate.PerTimePeriod.TotalSeconds))
            {

                if (model != null)
                {
                    PurgeErrors<BogusTemperatureRampRateException>(model, delegate (BogusTemperatureRampRateException trre)
                    {
                        return trre.TemperatureController == this;
                    });
                    model.AddError(new BogusTemperatureRampRateException(this));
                }
                else
                {
                    throw new BogusTemperatureRampRateException(this);
                }

            }
            #endregion

            #region Look for thermal conductance specification errors.
            bool wasError = false;
            for (double lvl = 0.0; lvl <= 1.0; lvl += 0.1)
            {
                if (double.IsNaN(GetAmbientThermalConductance(lvl)))
                {
                    wasError = true;
                    break;
                }
            }

            if (wasError)
            {
                if (model != null)
                {
                    model.AddError(new ThermalConductanceSpecificationException(this, ThermalConductanceSpecificationException.ThermalDriveType.Ambient));
                }
                else
                {
                    throw new TemperatureControllerException(this, "Thermal Conductance (Ambient) is improperly specified.");
                }
            }
            else
            {
                PurgeErrors<ThermalConductanceSpecificationException>(model, delegate (ThermalConductanceSpecificationException tcse)
                {
                    return tcse.TemperatureController == this && tcse.DriveType.Equals(ThermalConductanceSpecificationException.ThermalDriveType.Ambient);
                });
            }

            wasError = false;
            for (double lvl = 0.0; lvl <= 1.0; lvl += 0.1)
            {
                if (double.IsNaN(GetThermalConductance(lvl)))
                {
                    wasError = true;
                    break;
                }
            }

            if (wasError)
            {
                if (model != null)
                {
                    model.AddError(new ThermalConductanceSpecificationException(this, ThermalConductanceSpecificationException.ThermalDriveType.Driven));
                }
                else
                {
                    throw new TemperatureControllerException(this, "Thermal Conductance (Driven) is improperly specified.");
                }
            }
            else
            {
                PurgeErrors<ThermalConductanceSpecificationException>(model, delegate (ThermalConductanceSpecificationException tcse)
                {
                    return tcse.DriveType.Equals(ThermalConductanceSpecificationException.ThermalDriveType.Driven);
                });
            }

            #endregion

            #region Look for target temperature outside the capabilities of the heating/cooling system.
            if (TCSetpoint < TCMinSrcTemperature)
            {
                model.AddError(new ThermalRangeEndSpecificationException(this, ThermalRangeEndSpecificationException.RangeEndError.Low));
            }

            if (TCSetpoint > TCMaxSrcTemperature)
            {
                model.AddError(new ThermalRangeEndSpecificationException(this, ThermalRangeEndSpecificationException.RangeEndError.High));
            }

            #endregion

        }

        private void PurgeErrors<TErrorType>(IModel model, Predicate<TErrorType> predicate) where TErrorType : class
        {
            List<IModelError> removals = new List<IModelError>();
            foreach (IModelError ime in model.Errors)
            {
                TErrorType et = ime as TErrorType;
                if (et != null)
                {
                    if (predicate(et))
                        removals.Add(ime);
                }
            }

            removals.ForEach(delegate (IModelError ime)
            {
                model.RemoveError(ime);
            });
        }
        #endregion

        #endregion

        #region Temperature Controller Errors/Exceptions

        /// <summary>
        /// A TemperatureControllerException is both an ApplicationException and an IModelError. It is thrown from the
        /// temperature controller, in most cases, and may be caught at a higher level, and then either provisioned
        /// with more high-level target &amp; subject information, or logged into the model as a model error.
        /// </summary>
        public class TemperatureControllerException : Exception, IModelError
        {

            #region Protected Fields
            protected string name = "Unspecified Settings Error";
            protected string narrative = "There was an unspecified error in the settings of a temperature controller.";
            protected object target = null;
            protected object subject = null;
            protected bool autoClear = true;
            #endregion

            #region Private Fields
            private double _capacity;
            private Mixture _mixture;
            private SmallDoubleInterpolable _thCond;
            private SmallDoubleInterpolable _thCondAmb;
            private bool _tcEnabled;
            private double _tcSetpoint;
            private double _tcSourceTemp;
            private double _tcMinSourceTemp;
            private double _tcMaxSourceTemp;
            private double _tcDelta;
            private double _ambientTemp;
            private TemperatureControllerMode _tcMode;
            private TemperatureRampRate _temperatureRampRate;
            private double _priority = 0.0;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="TemperatureControllerException"/> class.
            /// </summary>
            /// <param name="tc">The temperature controller that initiated the exception/error.</param>
            /// <param name="msg">A textual message that the thrower provides.</param>
            public TemperatureControllerException(TemperatureController tc, string msg)
                : base(msg)
            {
                Initialize(tc);
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="TemperatureControllerException"/> class.
            /// </summary>
            /// <param name="tc">The temperature controller that initiated the exception/error.</param>
            public TemperatureControllerException(TemperatureController tc)
                : base()
            {
                Initialize(tc);
            }
            #endregion

            #region Public Fields
            /// <summary>
            /// Gets the temperature controller's container's capacity in liters.
            /// </summary>
            /// <value>The temperature controller's container's capacity in liters.</value>
            public double TemperatureControllerContainerCapacity
            {
                get
                {
                    return _capacity;
                }
            }
            /// <summary>
            /// Gets the temperature controller's container's mixture at the time of the exception/error.
            /// </summary>
            /// <value>The temperature controller container mixture.</value>
            public Mixture TemperatureControllerContainerMixture
            {
                get
                {
                    return _mixture;
                }
            }
            /// <summary>
            /// Gets a double interpolable holding the value of thermal conductance between the 
            /// compartment containing the heating/cooling medium and the compartment containing the 
            /// mixture, when a mixture fills a vessel to the level specified. This value is a linear 
            /// interpolation based on the discrete data points provided.
            /// </summary>
            /// <value>The temperature controller thermal conductance.</value>
            public SmallDoubleInterpolable TemperatureControllerThermalConductance
            {
                get
                {
                    return _thCond;
                }
            }
            /// <summary>
            /// Gets a double interpolable holding the value of thermal conductance between the outside
            /// environment (ambient) and the compartment containing the mixture, when a mixture fills
            /// a vessel to the level specified. This value is a linear interpolation based on the discrete
            /// data points provided.
            /// </summary>
            /// <value>The temperature controller ambient thermal conductance.</value>
            public SmallDoubleInterpolable TemperatureControllerAmbientThermalConductance
            {
                get
                {
                    return _thCondAmb;
                }
            }
            /// <summary>
            /// Gets a boolean that represents whether the Temperature Control System is
            /// enabled (true - the TCSrcTemperature is relevant, but ambient temperature is ignored, 
            /// or false - TCSrcTemperature is ignored, and temperature is allowed to drif toward ambient.)
            /// </summary>
            /// <value>
            /// 	<c>true</c> if [temperature controller enabled]; otherwise, <c>false</c>.
            /// </value>
            public bool TemperatureControllerEnabled
            {
                get
                {
                    return _tcEnabled;
                }
            }
            /// <summary>
            /// The target temperature for the temperature control system. This is the temperature that
            /// the system will seek and maintain (+/- the specified error band), if it is enabled.
            /// </summary>
            /// <value>The temperature controller setpoint.</value>
            public double TemperatureControllerSetpoint
            {
                get
                {
                    return _tcSetpoint;
                }
            }
            /// <summary>
            /// This is the temperature of the heating/cooling medium for the system, if it is in constant_T mode.
            /// </summary>
            /// <value>The temperature controller source temp.</value>
            public double TemperatureControllerSourceTemp
            {
                get
                {
                    return _tcSourceTemp;
                }
            }
            /// <summary>
            /// This is the minimum temperature of the heating/cooling medium for the system, important in constant
            /// delta T and constant ramp rate modes.
            /// </summary>
            /// <value>The temperature controller min source temp.</value>
            public double TemperatureControllerMinSourceTemp
            {
                get
                {
                    return _tcMinSourceTemp;
                }
            }
            /// <summary>
            /// This is the maximum temperature of the heating/cooling medium for the system, important in constant
            /// delta T and constant ramp rate modes.
            /// </summary>
            /// <value>The temperature controller max source temp.</value>
            public double TemperatureControllerMaxSourceTemp
            {
                get
                {
                    return _tcMaxSourceTemp;
                }
            }
            /// <summary>
            /// This is the difference in temperature between the heating/cooling medium and the mixture, if the 
            /// system is in constant_DeltaT mode.
            /// </summary>
            /// <value>The temperature controller delta T.</value>
            public double TemperatureControllerDeltaT
            {
                get
                {
                    return _tcDelta;
                }
            }
            /// <summary>
            /// This is the "outside temperature", for example, the atmospheric temperature in the plant.
            /// </summary>
            /// <value>The ambient temperature.</value>
            public double AmbientTemperature
            {
                get
                {
                    return _ambientTemp;
                }
            }
            /// <summary>
            /// The mode of the system (Constant delta-T, constant TSrc or Constant_RampRate).
            /// </summary>
            /// <value>The temperature controller mode.</value>
            public TemperatureControllerMode TemperatureControllerMode
            {
                get
                {
                    return _tcMode;
                }
            }
            /// <summary>
            /// This is the ramp rate that the temperature control system will maintain if it is set to
            /// Constant_RampRate mode. Note that it does not have meaning if the temperature control system 
            /// is not set to Constant_RampRate mode. It defaults to 0 degrees per minute.
            /// </summary>
            /// <value>The temperature ramp rate.</value>
            public TemperatureRampRate TemperatureRampRate
            {
                get
                {
                    return _temperatureRampRate;
                }
            }
            /// <summary>
            /// Gets a message that describes the current exception.
            /// </summary>
            /// <value></value>
            /// <returns>The error message that explains the reason for the exception, or an empty string("").</returns>
            public new string Message
            {
                get
                {
                    return narrative;
                }
            }

            /// <summary>
            /// Gets the temperature controller.
            /// </summary>
            /// <value>The temperature controller.</value>
            public TemperatureController TemperatureController
            {
                get
                {
                    return (TemperatureController)target;
                }
            }

            #endregion

            #region INotification Members

            /// <summary>
            /// The name of the notification.
            /// </summary>
            /// <value></value>
            public string Name
            {
                get
                {
                    return name;
                }
            }

            /// <summary>
            /// A descriptive text that describes what happened.
            /// </summary>
            /// <value></value>
            public string Narrative
            {
                get
                {
                    return narrative;
                }
            }

            /// <summary>
            /// Target is the place that the notification occurred.
            /// </summary>
            /// <value></value>
            public object Target
            {
                get
                {
                    return target;
                }
                set
                {
                    target = value;
                }
            }

            /// <summary>
            /// Subject is the thing that (probably) caused the notification.
            /// </summary>
            /// <value></value>
            public object Subject
            {
                get
                {
                    return subject;
                }
                set
                {
                    subject = value;
                }
            }

            /// <summary>
            /// Gets or sets the priority of the notification.
            /// </summary>
            /// <value>The priority.</value>
            public double Priority
            {
                get
                {
                    return _priority;
                }
                set
                {
                    _priority = value;
                }
            }

            public bool AutoClear
            {
                get
                {
                    return autoClear;
                }
            }
            #endregion

            private void Initialize(TemperatureController tc)
            {
                target = tc;
                _capacity = tc._container.Capacity;
                _mixture = (Mixture)tc._container.Mixture.Clone();
                _thCond = tc._thCond;
                _thCondAmb = tc._thCondAmb;
                _tcEnabled = tc._tcEnabled;
                _tcSetpoint = tc._tcSetpoint;
                _tcSourceTemp = tc._tcSourceTemp;
                _tcMinSourceTemp = tc._tcMinSourceTemp;
                _tcMaxSourceTemp = tc._tcMaxSourceTemp;
                _tcDelta = tc._tcDelta;
                _ambientTemp = tc._ambientTemp;
                _tcMode = tc._tcMode;
                _temperatureRampRate = tc._temperatureRampRate;
                _thCondAmb = tc._thCondAmb;
                _thCondAmb = tc._thCondAmb;
            }
        }

        /// <summary>
        /// An IncalculableTimeToSetpointException is both an ApplicationException and an IModelError.
        /// It is thrown when the temperature controller cannot calculate the amount of time it will
        /// take to reach the requested setpoint.
        /// </summary>
        public class IncalculableTimeToSetpointException : TemperatureControllerException
        {

            #region Private fields
            private readonly double _nSecs;
            private readonly double _tSrc;
            private readonly double _kMix;
            #endregion

            /// <summary>
            /// Initializes a new instance of the <see cref="IncalculableTimeToSetpointException"/> class.
            /// </summary>
            /// <param name="tc">The temperature controller.</param>
            /// <param name="nSeconds">The number of seconds to setpoint. Will be double.NaN or double.Infinity.</param>
            /// <param name="kMix">The heat conductance of the mixture.</param>
            /// <param name="tSrc">The temperature of the driving heat source or sink.</param>
            public IncalculableTimeToSetpointException(TemperatureController tc, double nSeconds, double kMix, double tSrc) : base(tc)
            {

                name = "Incalculable Time To Setpoint";
                _nSecs = nSeconds;
                _tSrc = tSrc;
                _kMix = kMix;

                string sDirection = (TemperatureControllerContainerMixture.Temperature < TemperatureControllerSetpoint) ? "raised" : "lowered";
                string heatSrcName = TemperatureControllerEnabled ? "the heat transfer medium" : "ambient";

                if (TemperatureControllerEnabled && TemperatureControllerMode.Equals(TemperatureControllerMode.Constant_RampRate))
                {

                    narrative = "Application has requested that the mixture, which is at " + TemperatureControllerContainerMixture.Temperature +
                        " °C, be " + sDirection + " to " + TemperatureControllerSetpoint + " °C at a ramp rate of " +
                        TemperatureRampRate.DegreesKelvin + " °C per " + TemperatureRampRate.PerTimePeriod +
                        ", which it can never accomplish.";// Stack trace is : " + new System.Diagnostics.StackTrace().ToString();

                }
                else
                {
                    narrative = "Application has requested that the mixture, which is at " + TemperatureControllerContainerMixture.Temperature +
                        " °C, be " + sDirection + " to " + TemperatureControllerSetpoint + " °C, across a heat conductance of " + _kMix + " W/sec - but " +
                        heatSrcName + " is at " + _tSrc + " °C, and cannot ever drive the mixture to" +
                        " the target temperature.";// Stack trace is : " + new System.Diagnostics.StackTrace().ToString();
                }

            }

            /// <summary>
            /// Gets the number of seconds that was calculated for time to reach setpoint. This will be double.NaN, or double.Infinity(pos or neg).
            /// </summary>
            /// <value>The number of seconds.</value>
            public double NumberOfSeconds
            {
                get
                {
                    return _nSecs;
                }
            }
            /// <summary>
            /// Gets the temperature of the heat source/sink that was used in calculating the duration.
            /// </summary>
            /// <value>The source temperature.</value>
            public double SourceTemperature
            {
                get
                {
                    return _tSrc;
                }
            }
            /// <summary>
            /// Gets the heat conductance of the vessel at the specific fullness of the vessel.
            /// </summary>
            /// <value>The heat conductance.</value>
            public double HeatConductance
            {
                get
                {
                    return _kMix;
                }
            }

        }

        /// <summary>
        /// An ThermalConductanceSpecificationException is both an ApplicationException and an IModelError.
        /// It is thrown when the <see cref="Highpoint.Sage.Mathematics.SmallDoubleInterpolable"/> that holds
        /// the thermal conductance of the containing vessel cannot determine conductance for some level of
        /// mixture in the vessel.
        /// </summary>
        public class ThermalConductanceSpecificationException : TemperatureControllerException
        {

            public enum ThermalDriveType
            {
                /// <summary>
                /// Thermal conductance in error is the non-ambient conductance.
                /// </summary>
                Driven,
                /// <summary>
                /// Thermal conductance in error is the ambient conductance.
                /// </summary>
                Ambient
            }

            private ThermalDriveType _driveType;

            /// <summary>
            /// Initializes a new instance of the <see cref="ThermalConductanceSpecificationException"/> class.
            /// </summary>
            /// <param name="tc">The tc.</param>
            /// <param name="dt">The dt.</param>
            public ThermalConductanceSpecificationException(TemperatureController tc, ThermalDriveType dt)
                : base(tc)
            {

                _driveType = dt;
                name = "Thermal Conductance Specification Error";
                narrative = "The \'" + dt + "\' Thermal Conductance is improperly specified.";
            }

            /// <summary>
            /// Gets the type of thermal drive for which the conductance cannot be determined.
            /// </summary>
            /// <value>The type of the drive.</value>
            public ThermalDriveType DriveType
            {
                get
                {
                    return _driveType;
                }
            }
        }

        /// <summary>
        /// A ThermalRangeEndSpecificationException is both an ApplicationException and an IModelError.
        /// It is thrown when the <see cref="TemperatureController"/> has been configured to, for example,
        /// cool a mixture to -40C, but the minimum coolant temperature is at 0C. or conversely, the
        /// <see cref="TemperatureController"/> has been configured to raise a mixture to +100C and the
        /// maximum heat source temperature is +80C.
        /// </summary>
        public class ThermalRangeEndSpecificationException : TemperatureControllerException
        {
            /// <summary>
            /// Communicates which end (high or low) of the temperature range is not properly specified.
            /// </summary>
            public enum RangeEndError
            {
                /// <summary>
                /// The high end of the temperature range is not properly specified.
                /// </summary>
                High,
                /// <summary>
                /// The low end of the temperature range is not properly specified.
                /// </summary>
                Low
            }

            private RangeEndError _rangeEndError;

            /// <summary>
            /// Initializes a new instance of the <see cref="ThermalRangeEndSpecificationException"/> class.
            /// </summary>
            /// <param name="tc">The tc.</param>
            /// <param name="ree">The ree.</param>
            public ThermalRangeEndSpecificationException(TemperatureController tc, RangeEndError ree)
                : base(tc)
            {

                _rangeEndError = ree;
                name = "Thermal Range End Specification Error";

                string rasiedOrLowered = ree.Equals(RangeEndError.High) ? "raised" : "lowered";
                string maxOrMin = ree.Equals(RangeEndError.High) ? "maximum" : "minimum";
                double boundaryTemp = ree.Equals(RangeEndError.High) ? TemperatureControllerMaxSourceTemp : TemperatureControllerMinSourceTemp;

                narrative = "Application has requested that the mixture be " + rasiedOrLowered + " to " + TemperatureControllerSetpoint + " °C, where the " +
                        maxOrMin + " heat sink temperature is " + boundaryTemp + " °C. This cannot be accomplished.";

            }

            /// <summary>
            /// Communicates which end (hogh or low) of the temperature range is not properly specified.
            /// </summary>
            /// <value>The range end.</value>
            public RangeEndError RangeEnd
            {
                get
                {
                    return _rangeEndError;
                }
            }
        }

        /// <summary>
        /// A BogusTemperatureRampRateException is both an ApplicationException and an IModelError.
        /// It is thrown when a temperature ramp rate is specified that cannot be achieved in the
        /// model.
        /// </summary>
        public class BogusTemperatureRampRateException : TemperatureControllerException
        {

            /// <summary>
            /// Initializes a new instance of the <see cref="BogusTemperatureRampRateException"/> class.
            /// </summary>
            /// <param name="tc">The tc.</param>
            public BogusTemperatureRampRateException(TemperatureController tc)
                : base(tc)
            {

                name = "Temperature Ramp-Rate Specification Error";

                narrative = string.Format("Temperature ramp rate is specified as {0} ('X') degrees C per {1} ('Y') minute. Both X and Y must"
                    + " be between zero and infinity, exclusive of those limits.", tc.TCTemperatureRampRate.DegreesKelvin, tc.TCTemperatureRampRate.PerTimePeriod.TotalMinutes);

            }
        }

        #endregion

    }
}
