/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using System;

namespace Highpoint.Sage.Materials.Thermodynamics
{
    /// <summary>
    /// This interface permits the user to set, read and control an object's temperature control capabilities.
    /// </summary>
    public interface ITemperatureController
    {
        /// <summary>
        /// This sets a value for thermal conductance between the compartment containing the
        /// heating/cooling medium and the compartment containing the mixture in a vessel at a
        /// certain level. For example, SetThermalConductance(0.30,150) sets the thermal conductance
        /// to 150 Watts per degree kelvin difference between the heating/cooling medium and the
        /// mixture, when the mixture fills the vessel to it's '30% FULL' line.
        /// </summary>
        /// <param name="level">Describes at what level (0.30 equates to 30% FULL) the datum is correct.</param>
        /// <param name="val">Thermal conductance, in Watts per degree kelvin.</param>
        void SetThermalConductance(double level, double val);
        /// <summary>
        /// Gets the value of thermal conductance between the compartment containing the
        /// heating/cooling medium and the compartment containing the mixture, when a
        /// mixture fills a vessel to the level specified. This value is a linear interpolation
        /// based on the discrete data points provided.
        /// </summary>
        /// <param name="level">The percentage full (0.30 is 30%) that the vessel is, at the data point of interest.</param>
        /// <returns>The value of the specified thermal conductance.</returns>
        double GetThermalConductance(double level);
        /// <summary>
        /// This sets a value for thermal conductance between the outside environment (ambient)
        /// and the compartment containing the mixture in a vessl at a certain level. For example,
        /// SetAmbientThermalConductance(0.50,20) sets the thermal conductance to 20 Watts per degree
        /// kelvin difference between the outside air and the mixture, when the mixture fills the
        /// vessel to it's '30% FULL' line.
        /// </summary>
        /// <param name="level">Describes at what level (0.30 equates to 30% FULL) the datum is correct.</param>
        /// <param name="val">Thermal conductance, in Watts per degree kelvin.</param>
        void SetAmbientThermalConductance(double level, double val);
        /// <summary>
        /// Gets the value of thermal conductance between the outside environment (ambient)
        /// and the compartment containing the mixture, when a mixture fills a vessel to the
        /// level specified. This value is a linear interpolation based on the discrete data
        /// points provided.
        /// </summary>
        /// <param name="level">The percentage full (0.30 is 30%) that the vessel is, at the
        /// data point of interest.</param>
        /// <returns>The value of the specified thermal conductance.</returns>
        double GetAmbientThermalConductance(double level);
        /// <summary>
        /// Sets and gets a boolean that represents whether the Temperature Control System is
        /// enabled (true - the TCSrcTemperature is relevant, but ambient temperature is ignored, 
        /// or false - TCSrcTemperature is ignored, and temperature is allowed to drif toward ambient.)
        /// </summary>
        bool TCEnabled
        {
            set; get;
        }
        /// <summary>
        /// The target temperature for the temperature control system. This is the temperature that
        /// the system will seek and maintain (+/- the specified error band), if it is enabled.
        /// </summary>
        double TCSetpoint
        {
            set; get;
        }
        /// <summary>
        /// This is the temperature of the heating/cooling medium for the system, if it is in constant_T mode.
        /// </summary>
        double TCSrcTemperature
        {
            set; get;
        }
        /// <summary>
        /// This is the maximum temperature of the heating/cooling medium for the system, important in constant
        /// delta T and constant ramp rate modes.
        /// </summary>
        double TCMaxSrcTemperature
        {
            set; get;
        }
        /// <summary>
        /// This is the minimum temperature of the heating/cooling medium for the system, important in constant
        /// delta T and constant ramp rate modes.
        /// </summary>
        double TCMinSrcTemperature
        {
            set; get;
        }
        /// <summary>
        /// This is the difference in temperature between the heating/cooling medium and the mixture, if the 
        /// system is in constant_DeltaT mode.
        /// </summary>
        double TCSrcDelta
        {
            set; get;
        }

        /// <summary>
        /// This is the ramp rate that the temperature control system will maintain if it is set to
        /// Constant_RampRate mode. Note that it does not have meaning if the temperature control system 
        /// is not set to Constant_RampRate mode. It defaults to 0 degrees per minute.
        /// </summary>
        TemperatureRampRate TCTemperatureRampRate
        {
            set; get;
        }
        /// <summary>
        /// This is the "outside temperature", for example, the atmospheric temperature in the plant.
        /// </summary>
        double AmbientTemperature
        {
            set; get;
        }
        /// <summary>
        /// The mode of the system (Constant delta-T, constant TSrc or Constant_RampRate).
        /// </summary>
        TemperatureControllerMode TCMode
        {
            get; set;
        }
        ///// <summary>
        ///// When the temperature control system has reached its setpoint, the mixture temperature
        ///// will be at some actual temperature within this number of degrees of the setpoint, and
        ///// will vary unpredictably in this band, over time.
        ///// </summary>
        //double ErrorBand { get; set; }
        /// <summary>
        /// The temperature controller's precision is a measure of how close to the setpoint the mixture
        /// needs to be in actuality before the controller will consider the mixture to have reached the
        /// setpoint. This is necessary in order to represent the physical case where the temperature
        /// controller mode is Constant_TSrc, the src temp is at 79 degrees, and the setpoint is also at 79 degrees -
        /// theoretically, the mixture, unless already at precisely 79 degrees, will not reach the desired
        /// setpoint temperature. However, if precision is set to 0.0001, then the mixture will be considered
        /// to have reached its setpoint when at 78.9999, or at 79.0001, if being driven up from below, or 
        /// down from above, respectively.
        /// </summary>
        double Precision
        {
            get; set;
        }
        /// <summary>
        /// The temperature control system will predict the amount of time required for it to reach
        /// the setpoint temperature (plus or minus the temperature controller's precision) in the
        /// mixture. <para><B>This will throw an exception if the system cannot ever drive the mixture
        /// to the target temperature.</B></para>
        /// </summary>
        TimeSpan TimeNeededToReachTargetTemp();
        /// <summary>
        /// The temperature control system will modify the mixture (and perhaps its TCSrcTemperature,
        /// if it is in constant deltaT mode) to represent the state in effect after passage of the
        /// proscribed timespan.
        /// </summary>
        /// <param name="elapsedSinceLastUpdate"></param>
        void ImposeEffectsOfDuration(TimeSpan elapsedSinceLastUpdate);

        /// <summary>
        /// Returns the current power available from the temperature controller, given existing settings
        /// of TCMode, Tsrc, Tambient, thermal conductivity and mixture levels.
        /// </summary>
        /// <returns>Thermal power currently available, in watts.</returns>
        double GetCurrentThermalPower();

        /// <summary>
        /// Validates the current settings of this Temperature controller. If warnings and errors are encountered,
        /// then the method adds those errors and warnings to the model. If the model reference is null, then this
        /// method ignores warnings and throws an exception with the first error encountered.
        /// </summary>
        /// <param name="model">The model in whose context this temperature controller is running.</param>
        void Validate(IModel model);

    }
}
