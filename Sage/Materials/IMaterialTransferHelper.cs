/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Materials.Chemistry;
using System;

namespace Highpoint.Sage.Materials
{
    /// <summary>
    /// This interface is implemented by any object that intends to help perform a material transfer
    /// by extracting the material of interest, and declaring how long it will take to transfer.
    /// </summary>
    public interface IMaterialTransferHelper : IMaterialExtractor, SimCore.ICloneable
    {
        /// <summary>
        /// Indicates the duration of the transfer.
        /// </summary>
		TimeSpan Duration
        {
            get;
        }
        /// <summary>
        /// Indicates the mass of material that will be involved in the transfer.
        /// </summary>
        double Mass
        {
            get;
        }
        /// <summary>
        /// Indicates the MaterialType of the material that will be involved in the transfer. If it is
        /// null, then the specified mass will be transferred, but it will be of the mixture specified
        /// in the target mixture.
        /// </summary>
        MaterialType MaterialType
        {
            get;
        }
    }
}