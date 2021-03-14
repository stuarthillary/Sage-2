/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Materials.Chemistry
{
    /// <summary>
    /// An enumeration that describes the kind of change that has taken place in a material.
    /// </summary>
    public enum MaterialChangeType
    {
        /// <summary>
        /// The contents of the mixture changed.
        /// </summary>
        Contents,
        /// <summary>
        /// The temperature of the mixture changed.
        /// </summary>
        Temperature
    }
}