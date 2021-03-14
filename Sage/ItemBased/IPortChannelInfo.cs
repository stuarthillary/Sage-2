/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.ItemBased.Ports
{
    /// <summary>
    /// Interface IPortChannelInfo specifies information about what travels on the port, and in
    /// what direction. Examples might be "Input" or "Output", or maybe "Control", "Kanban", etc.
    /// </summary>
    public interface IPortChannelInfo
    {
        /// <summary>
        /// Gets the direction of flow across the port.
        /// </summary>
        /// <value>The direction.</value>
        PortDirection Direction
        {
            get;
        }
        /// <summary>
        /// Gets the name of the type - usually "Input" or "Output", but could be "Control", "Kanban", etc..
        /// </summary>
        /// <value>The name of the type.</value>
        string TypeName
        {
            get;
        }
    }

}
