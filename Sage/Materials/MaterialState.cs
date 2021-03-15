/* This source code licensed under the GNU Affero General Public License */


// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable RedundantDefaultMemberInitializer

namespace Highpoint.Sage.Materials.Chemistry
{
    /// <summary>
    /// Enum MaterialState represents the current (or initial) state of a material. It is used if the modeler wishes to
    /// represent the thermodynamic effects of state change, and if they do, then Latent Heat of Vaporization and SpecificHeat
    /// must be provided. Note - the mechanisms for modeling thermal transition to the solid state are not present, as this is
    /// a rare transition to occur in manufacturing processes - at least no need has yet been encountered.
    /// </summary>
    public enum MaterialState
    {
        /// <summary>
        /// The unknown MaterialState.
        /// </summary>
        Unknown,
        /// <summary>
        /// The solid MaterialState.
        /// </summary>
        Solid,
        /// <summary>
        /// The liquid MaterialState.
        /// </summary>
        Liquid,
        /// <summary>
        /// The gas MaterialState.
        /// </summary>
        Gas
    }
}
