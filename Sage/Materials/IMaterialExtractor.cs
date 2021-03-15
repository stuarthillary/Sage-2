/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Materials.Chemistry;

namespace Highpoint.Sage.Materials
{
    /// <summary>
    /// This interface is implemented by an object that will be used to extract material 
    /// from a mixture. Note that the implementer will actually change the source material in
    /// doing so.
    /// </summary>
    public interface IMaterialExtractor
    {
        /// <summary>
        /// Extracts a substance or mixture from another substance or mixture.
        /// </summary>
        IMaterial GetExtract(IMaterial source);
    }
}