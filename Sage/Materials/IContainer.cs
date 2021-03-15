/* This source code licensed under the GNU Affero General Public License */


// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable RedundantAssignment

namespace Highpoint.Sage.Materials.Chemistry
{
    /// <summary>
    /// Implemented by anything that can hold a mixture, and has a capacity.
    /// </summary>
    public interface IContainer {

        /// <summary>
        /// Gets the mixture.
        /// </summary>
        /// <value>The mixture.</value>
        Mixture Mixture { get; }

        /// <summary>
        /// Gets the capacity in cubic meters.
        /// </summary>
        /// <value>The capacity.</value>
        double Capacity { get; }

        /// <summary>
        /// Gets the pressure.
        /// </summary>
        /// <value>The pressure.</value>
        double Pressure { get; }

    }
}
