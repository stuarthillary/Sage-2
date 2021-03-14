/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Materials.Thermodynamics
{
    /// <summary>
    /// This is the operating mode for the Temperature Controller. The temperature
    /// controller will either maintain a constant driving temperature in the cooling
    /// or heating medium (constant_T mode), it will maintain a constant delta T
    /// across the boundary between the cooling or heating medium and the mixture (as
    /// in the constant_DeltaT mode), or it will manipulate the deltaT to ensure a
    /// constant temperature ramp rate in the mixture.
    /// </summary>
    public enum TemperatureControllerMode
    {
        /// <summary>
        /// Maintains a constant driving temperature in the cooling or heating medium (constant_T mode)
        /// </summary>
        ConstantT,
        /// <summary>
        /// Maintains a constant delta T across the boundary between the cooling or heating medium and the mixture.
        /// </summary>
        ConstantDeltaT,

        /// <summary>
        /// Maintains a constant temperature ramp-rate in the mixture.
        /// </summary>
        Constant_RampRate
    }
}
