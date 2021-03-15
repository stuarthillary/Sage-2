/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.ItemBased.SplittersAndJoiners;
using Highpoint.Sage.SimCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

namespace Highpoint.Sage.ItemBased
{
    using DelegTwoChoice = SimpleDelegatedTwoChoiceBranchBlock;
    using StochTwoChoice = SimpleStochasticTwoChoiceBranchBlock;

    /// <summary>
    /// Summary description for zTestBranchBlocks.
    /// </summary>
    [TestClass]
    public class BranchBlockTester
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

        int _lastResult = -1;
        public BranchBlockTester()
        {
        }

        private readonly int[] _expected = new int[]{1,1,0,1,0,1,0,0,1,1,1,1,1,0,1,1,0,0,0,1,0,1,1,0,1,1,1,0,1,1,1,1,0,
                                              1,1,1,1,1,1,1,0,0,1,1,1,1,1,1,0,1,1,0,0,1,1,1,1,1,1,0,1,1,1,1,0,1,
                                              1,0,0,1,1,1,0,1,1,0,1,1,1,1,1,1,1,1,1,1,0,0,1,1,1,1,1,1,1,1,1,1,1,1,};
        private int _itemNumber;

        [TestMethod]
        public void TestStochasticBranchBlock()
        {
            Model model = new Model();
            model.RandomServer = new Randoms.RandomServer(12345, 100);

            StochTwoChoice ss2cbb = new StochTwoChoice(model, "s2c", Guid.NewGuid(), .2);
            ss2cbb.Outputs[0].PortDataPresented += new PortDataEvent(Out0_PortDataPresented);
            ss2cbb.Outputs[1].PortDataPresented += new PortDataEvent(Out1_PortDataPresented);

            for (_itemNumber = 0; _itemNumber < _expected.Length; _itemNumber++)
            {
                ss2cbb.Input.Put(new object());
                Debug.Write(_lastResult + ",");
                Assert.IsTrue(_lastResult == _expected[_itemNumber], "Unexpected choice.");
            }
        }

        [TestMethod]
        public void TestDelegatedBranchBlock()
        {
            Model model = new Model();
            DelegTwoChoice d2cbb = new DelegTwoChoice(model, "s2c", Guid.NewGuid());
            d2cbb.BooleanDeciderDelegate = new BooleanDecider(ChooseYesOrNo);
            d2cbb.Outputs[0].PortDataPresented += new PortDataEvent(Out0_PortDataPresented);
            d2cbb.Outputs[1].PortDataPresented += new PortDataEvent(Out1_PortDataPresented);
            Randoms.RandomServer rs = new Randoms.RandomServer(12345, 100);
            model.RandomServer = rs;
            for (_itemNumber = 0; _itemNumber < _expected.Length; _itemNumber++)
            {
                d2cbb.Input.Put(new object());
                Debug.Write(_lastResult + ",");
                Assert.IsTrue(_lastResult == _expected[_itemNumber], "Unexpected choice.");
            }
        }

        private bool ChooseYesOrNo(object serverObject)
        {
            return _expected[_itemNumber] == 0;
        }

        private void Out0_PortDataPresented(object data, IPort where)
        {
            _lastResult = 0;
        }

        private void Out1_PortDataPresented(object data, IPort where)
        {
            _lastResult = 1;
        }
    }
}
