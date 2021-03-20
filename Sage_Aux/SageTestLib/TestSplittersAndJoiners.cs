/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.ItemBased.SplittersAndJoiners;
using Highpoint.Sage.SimCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

namespace Highpoint.Sage.ItemBased.Blocks
{

    /// <summary>
    /// Summary description for zTestBranchBlocks.
    /// </summary>
    [TestClass]
    public class SplitterTester
    {

        #region MSTest Goo
        [TestInitialize]
        public void Init()
        {
        }
        [TestCleanup]
        public void destroy()
        {
            Debug.WriteLine("Done.");
        }
        #endregion

        private int _lastResult = 0;
        readonly int _nPorts = 6;
        public SplitterTester()
        {
        }

        [TestMethod]
        public void TestSplitters()
        {

            Model model = new Model();
            model.RandomServer = new Randoms.RandomServer(12345, 100);

            SimultaneousPushSplitter sps = new SimultaneousPushSplitter(model, "SimultaneousPushSplitter", Guid.NewGuid(), 6);
            foreach (IPort port in sps.Ports)
                port.PortDataPresented += new PortDataEvent(OnPortDataPresented);

            sps.Input.Put(new object());
            Assert.IsTrue(_lastResult == _nPorts + 1, "Not all output ports reported arrival of the pushed object.");
        }

        private void OnPortDataPresented(object data, IPort where)
        {
            Debug.WriteLine(data + " arriving on port with key " + where.Key.ToString());
            _lastResult++;
        }
    }

    /// <summary>
    /// Summary description for zTestBranchBlocks.
    /// </summary>
    [TestClass]
    public class JoinerTester
    {

        #region MSTest Goo
        [TestInitialize]
        public void Init()
        {
        }
        [TestCleanup]
        public void destroy()
        {
            Debug.WriteLine("Done.");
        }
        #endregion

        private int _lastResult = 0;
        private readonly int _nPorts = 6;
        public JoinerTester()
        {
        }

        [TestMethod]
        public void TestJoiner()
        {

            Model model = new Model();
            model.RandomServer = new Randoms.RandomServer(12345, 100);

            PushJoiner pj = new PushJoiner(model, "PushJoiner", Guid.NewGuid(), 6);
            foreach (IPort port in pj.Ports)
                port.PortDataPresented += new PortDataEvent(OnPortDataPresented);

            foreach (IPort port in pj.Ports)
            {
                if (port is IInputPort)
                    ((IInputPort)port).Put(new object());
            }
            Assert.IsTrue(_lastResult == _nPorts * 2, "Not all output ports reported arrival of the pushed object.");
        }

        private void OnPortDataPresented(object data, IPort where)
        {
            Debug.WriteLine(data + " arriving on port with key " + where.Key.ToString());
            _lastResult++;
        }
    }
}
