/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.ItemBased.Ports
{
    /// <summary>
    /// This interface is implemented by any object that can choose ports. It is useful in
    /// constructing an autonomous route navigator, route strategy object, or transportation
    /// manager.
    /// </summary>
    public interface IPortSelector
    {
        /// <summary>
        /// Selects a port from among a presented set of ports.
        /// </summary>
        /// <param name="portSet">The Set of ports.</param>
        /// <returns>The selected port.</returns>
        IPort SelectPort(IPortSet portSet);
    }

}
