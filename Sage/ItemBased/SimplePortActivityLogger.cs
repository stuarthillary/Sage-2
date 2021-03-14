/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.ItemBased.Ports;
using System.Collections;
using System.Text;

namespace Highpoint.Sage.ItemBased
{
    internal class SimplePortActivityLogger
    {
        private readonly StringBuilder _contents = new StringBuilder();
        private readonly ArrayList _arrayList = new ArrayList();
        private readonly IPort _port;
        public SimplePortActivityLogger(IPort port)
        {
            _port = port;
            port.PortDataPresented += new PortDataEvent(PortDataPresented);
            port.PortDataAccepted += new PortDataEvent(PortDataAccepted);
            port.PortDataRejected += new PortDataEvent(PortDataRejected);
        }

        public SimplePortActivityLogger(IPortSet portSet)
        {
            foreach (IPort port in portSet)
            {
                port.PortDataPresented += new PortDataEvent(PortDataPresented);
                port.PortDataAccepted += new PortDataEvent(PortDataAccepted);
                port.PortDataRejected += new PortDataEvent(PortDataRejected);
            }
        }

        void PortDataPresented(object data, IPort port)
        {
            _contents.Append("A port on " + port.Owner + " was presented with " + data + "\r\n");
        }

        void PortDataAccepted(object data, IPort port)
        {
            _contents.Append("A port on " + port.Owner + " accepted " + data + "\r\n");
        }

        void PortDataRejected(object data, IPort port)
        {
            _contents.Append("A port on " + port.Owner + " rejected " + data + "\r\n");
        }

        public string Contents
        {
            get
            {
                return _contents.ToString();
            }
        }
    }
}