/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Graphs.PFC
{
    /// <summary>
    /// Holds key data on the unit with which a specific step is associated.
    /// </summary>
    public interface IPfcUnitInfo
    {
        /// <summary>
        /// The name of the unit with which a step is associated.
        /// </summary>
        string Name
        {
            get; set;
        }
        /// <summary>
        /// The sequence number of the unit with which a step is associated.
        /// </summary>
        int SequenceNumber
        {
            get; set;
        }
    }
}
