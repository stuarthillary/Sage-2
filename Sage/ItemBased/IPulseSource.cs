/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.ItemBased
{
    /// <summary>
    /// Implemented by an object that generates pulses, either periodic or random.
    /// </summary>
    public interface IPulseSource
    {
        /// <summary>
        /// Fired when a PulseSource delivers its 'Do It!' command.
        /// </summary>
        event PulseEvent PulseEvent;
    }
}