/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.ItemBased.Ports
{
    /// <summary>
    /// Class PortManager is an abstract class that sets up some of the basic functionality of
    /// both input port managers and output port managers.
    /// </summary>
    public abstract class PortManager
    {

        /// <summary>
        /// The diagnostics switch. Set in the Sage Config file.
        /// </summary>
        protected static bool Diagnostics = Sage.Diagnostics.DiagnosticAids.Diagnostics("PortManager");

        /// <summary>
        /// Types of buffer persistence. How long the data is buffered for the port.
        /// </summary>
        public enum BufferPersistence
        {
            /// <summary>
            /// The data is not buffered. If it is not read in the call that sets it, it is lost.
            /// </summary>
            None,
            /// <summary>
            /// The data is buffered until it is read or overwritten.
            /// </summary>
            UntilRead,
            /// <summary>
            /// The data is buffered until it is overwritten.
            /// </summary>
            UntilWrite
        }

        /// <summary>
        /// The buffer persistence
        /// </summary>
        protected BufferPersistence bufferPersistence;

        /// <summary>
        /// Gets or sets the data buffer persistence.
        /// </summary>
        /// <value>The data buffer persistence.</value>
        public BufferPersistence DataBufferPersistence
        {
            get
            {
                return bufferPersistence;
            }
            set
            {
                bufferPersistence = value;
            }
        }

        /// <summary>
        /// Clears the buffer.
        /// </summary>
        public abstract void ClearBuffer();

        /// <summary>
        /// Gets a value indicating whether this port is connected.
        /// </summary>
        /// <value><c>true</c> if this port is connected; otherwise, <c>false</c>.</value>
        public abstract bool IsPortConnected
        {
            get;
        }

    }
}
