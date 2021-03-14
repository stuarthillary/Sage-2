/* This source code licensed under the GNU Affero General Public License */

using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased.Ports
{
    public class GeneralPortChannelInfo : IPortChannelInfo
    {
        private readonly string _name;
        private readonly PortDirection _direction;
        public GeneralPortChannelInfo(string name, PortDirection direction)
        {
            _name = name;
            _direction = direction;
        }


        #region IPortChannelInfo Members

        public PortDirection Direction
        {
            get
            {
                return _direction;
            }
        }

        public string TypeName
        {
            get
            {
                return _name;
            }
        }

        #endregion

        public static GeneralPortChannelInfo StandardInput
        {
            get
            {
                return stdinput;
            }
        }
        public static GeneralPortChannelInfo StandardOutput
        {
            get
            {
                return stdoutput;
            }
        }
        public static List<IPortChannelInfo> StdInputOnly
        {
            get
            {
                return stdinputonlylist;
            }
        }
        public static List<IPortChannelInfo> StdOutputOnly
        {
            get
            {
                return stdoutputonlylist;
            }
        }
        public static List<IPortChannelInfo> StdInputAndOutput
        {
            get
            {
                return stdinandoutlist;
            }
        }

        private static GeneralPortChannelInfo stdinput = new GeneralPortChannelInfo("Input", PortDirection.Input);
        private static GeneralPortChannelInfo stdoutput = new GeneralPortChannelInfo("Output", PortDirection.Output);
        private static List<IPortChannelInfo> stdinputonlylist = new List<IPortChannelInfo>(new IPortChannelInfo[] { stdinput });
        private static List<IPortChannelInfo> stdoutputonlylist = new List<IPortChannelInfo>(new IPortChannelInfo[] { stdoutput });
        private static List<IPortChannelInfo> stdinandoutlist = new List<IPortChannelInfo>(new IPortChannelInfo[] { stdinput, stdoutput });
    }
}
