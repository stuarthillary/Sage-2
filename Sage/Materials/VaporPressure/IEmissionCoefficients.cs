/* This source code licensed under the GNU Affero General Public License */

// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Materials.Chemistry.VaporPressure
{

    /// <summary>
    /// Determines if, in a certain situation, a set of coefficients, and therefore the
    /// calculation mechanism that uses those coefficients, can be used.
    /// </summary>
    public interface IEmissionCoefficients
    {
        /// <summary>
        /// Determines if, in a certain situation, a set of coefficients, and therefore the
        /// calculation mechanism that uses those coefficients, can be used.
        /// </summary>
        /// <param name="temperature">The temperature of the mixture being assessed, in degrees Kelvin.</param>
        /// <returns></returns>
        bool IsSufficientlySpecified(double temperature);
    }
}
