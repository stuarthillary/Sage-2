/* This source code licensed under the GNU Affero General Public License */

using System.Diagnostics;

namespace Highpoint.Sage.Utility
{
    using Highpoint.Sage.SimCore;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections;

    /// <summary>
    /// Summary description for TupleTester.
	/// </summary>
	[TestClass]
    public class TupleTester
    {
        private ITupleSpace _tsut; // ExchangeUnderTest.
        private ArrayList _results;
        private readonly IExecutive _exec;
        private readonly DateTime _t;
        private readonly ExecEventReceiver _postIt;
        private readonly ExecEventReceiver _readIt;
        private readonly ExecEventReceiver _takeIt;
        private readonly ExecEventReceiver _blockingPostIt;
        private readonly ExecEventReceiver _blockingReadIt;
        private readonly ExecEventReceiver _blockingTakeIt;
        private readonly ExecEventReceiver _blockTilGone;
        private readonly string _k1 = "Object 1";

        public TupleTester()
        {
            _exec = ExecFactory.Instance.CreateExecutive();
            _t = new DateTime(2004, 07, 15, 05, 23, 14);
            _postIt = new ExecEventReceiver(PostTuple);
            _readIt = new ExecEventReceiver(ReadTuple);
            _takeIt = new ExecEventReceiver(TakeTuple);
            _blockingPostIt = new ExecEventReceiver(BlockingPostTuple);
            _blockingReadIt = new ExecEventReceiver(BlockingReadTuple);
            _blockingTakeIt = new ExecEventReceiver(BlockingTakeTuple);
            _blockTilGone = new ExecEventReceiver(BlockTilGoneTuple);
        }

        [TestInitialize]
        public void Init()
        {
        }
        [TestCleanup]
        public void destroy()
        {
            Debug.WriteLine("Done.");
        }

        [TestMethod]
        public void TestTupleBasics()
        {
            string[] expected = new string[] { "632254657940000000:RT1:" + HashCode(_k1), "632254657940000000:RT2b:" + HashCode(_k1), "632254657940000000:PT1:" + HashCode(_k1), "632254657940000000:PT2:" + HashCode(_k1), "632254657940000000:TT1:" + HashCode(_k1), "632254657940000000:TT2a:" + HashCode(_k1) };
            InitTest();
            RequestEvent(XActType.Read, _k1, 0);
            RequestEvent(XActType.Post, _k1, 0);
            RequestEvent(XActType.Take, _k1, 0);
            _exec.Start();
            EvaluateTest(expected, false);
        }

        private string HashCode(string k1)
        {
            return k1.GetHashCode(StringComparison.Ordinal).ToString();
        }

        [TestMethod]
        public void TestRead()
        {
            string[] expected = new string[] { "632254657940000000:PT1:" + HashCode(_k1), "632254657940000000:PT2:" + HashCode(_k1), "632254659740000000:RT1:" + HashCode(_k1), "632254659740000000:RT2a:" + HashCode(_k1) };
            InitTest();

            RequestEvent(XActType.Post, _k1, 0);
            RequestEvent(XActType.Read, _k1, 3);
            _exec.Start();
            EvaluateTest(expected, false);
        }
        [TestMethod]
        public void TestTake()
        {
            string[] expected = new string[] { "632254657940000000:PT1:" + HashCode(_k1), "632254657940000000:PT2:" + HashCode(_k1), "632254659740000000:TT1:" + HashCode(_k1), "632254659740000000:TT2a:" + HashCode(_k1) };
            InitTest();
            RequestEvent(XActType.Post, _k1, 0);
            RequestEvent(XActType.Take, _k1, 3);
            _exec.Start();
            EvaluateTest(expected, false);
        }
        [TestMethod]
        public void TestBlockingPost()
        {
            string[] expected = new string[] { "632254657940000000:BPT1:" + HashCode(_k1), "632254657940000000:RT1:" + HashCode(_k1), "632254657940000000:RT2a:" + HashCode(_k1), "632254657940000000:RT1:" + HashCode(_k1), "632254657940000000:RT2a:" + HashCode(_k1), "632254657940000000:TT1:" + HashCode(_k1), "632254657940000000:TT2a:" + HashCode(_k1), "632254657940000000:BPT2:" + HashCode(_k1) };
            InitTest();

            RequestEvent(XActType.BlockingPost, _k1, 0);
            RequestEvent(XActType.Read, _k1, 0);
            RequestEvent(XActType.Read, _k1, 0);
            RequestEvent(XActType.Take, _k1, 0);
            _exec.Start();
            EvaluateTest(expected, false);
        }

        [TestMethod]
        public void TestBlockingRead()
        {
            string[] expected = new string[] { "632254657940000000:RT1:" + HashCode(_k1), "632254657940000000:RT2b:" + HashCode(_k1), "632254657940000000:BRT1:" + HashCode(_k1), "632254657940000000:PT1:" + HashCode(_k1), "632254657940000000:PT2:" + HashCode(_k1), "632254657940000000:BRT2:" + HashCode(_k1), "632254657940000000:TT1:" + HashCode(_k1), "632254657940000000:TT2a:" + HashCode(_k1) };
            InitTest();
            RequestEvent(XActType.Read, _k1, 0);
            RequestEvent(XActType.BlockingRead, _k1, 0);
            RequestEvent(XActType.Post, _k1, 0);
            RequestEvent(XActType.Take, _k1, 0);
            _exec.Start();
            EvaluateTest(expected, false);
        }

        [TestMethod]
        public void TestBlockingTake()
        {
            string[] expected = new string[] { "632254657940000000:RT1:" + HashCode(_k1), "632254657940000000:RT2b:" + HashCode(_k1), "632254657940000000:BTT1:" + HashCode(_k1), "632254657940000000:PT1:" + HashCode(_k1), "632254657940000000:PT2:" + HashCode(_k1), "632254657940000000:BTT2:" + HashCode(_k1), "632254657940000000:RT1:" + HashCode(_k1), "632254657940000000:RT2b:" + HashCode(_k1) };
            InitTest();
            RequestEvent(XActType.Read, _k1, 0);
            RequestEvent(XActType.BlockingTake, _k1, 0);
            RequestEvent(XActType.Post, _k1, 0);
            RequestEvent(XActType.Read, _k1, 0);
            _exec.Start();
            EvaluateTest(expected, false);
        }

        [TestMethod]
        public void TestBlock()
        {
            string[] expected = new string[] { "632254657940000000:PT1:" + HashCode(_k1), "632254657940000000:PT2:" + HashCode(_k1), "632254658540000000:WTG1:" + HashCode(_k1), "632254659140000000:RT1:" + HashCode(_k1), "632254659140000000:RT2a:" + HashCode(_k1), "632254659740000000:TT1:" + HashCode(_k1), "632254659740000000:TT2a:" + HashCode(_k1), "632254659740000000:WTG2:" + HashCode(_k1) };
            InitTest();

            RequestEvent(XActType.Post, _k1, 0);
            RequestEvent(XActType.BlockTilGone, _k1, 1);
            RequestEvent(XActType.Read, _k1, 2);
            RequestEvent(XActType.Take, _k1, 3);
            _exec.Start();
            EvaluateTest(expected, false);
        }


        private enum XActType { Post, Read, Take, BlockingPost, BlockingRead, BlockingTake, BlockTilGone };

        private void RequestEvent(XActType xactType, string key, int when)
        {
            ExecEventReceiver eer;
            switch (xactType)
            {
                case XActType.Post:
                    {
                        eer = _postIt;
                        break;
                    }
                case XActType.Read:
                    {
                        eer = _readIt;
                        break;
                    }
                case XActType.Take:
                    {
                        eer = _takeIt;
                        break;
                    }
                case XActType.BlockingPost:
                    {
                        eer = _blockingPostIt;
                        break;
                    }
                case XActType.BlockingRead:
                    {
                        eer = _blockingReadIt;
                        break;
                    }
                case XActType.BlockingTake:
                    {
                        eer = _blockingTakeIt;
                        break;
                    }
                case XActType.BlockTilGone:
                    {
                        eer = _blockTilGone;
                        break;
                    }
                default:
                    {
                        eer = null;
                        break; /*throw new ApplicationException("Unknown XActType.");*/
                    }
            }
            _exec.RequestEvent(eer, _t + TimeSpan.FromMinutes(when), 0, key, ExecEventType.Detachable);
        }

        private void InitTest()
        {
            _tsut = new Exchange(_exec);
            _results = new ArrayList();
            _exec.Reset();
        }

        private void EvaluateTest(string[] expected, bool benchmark)
        {
            if (benchmark)
            {
                Console.WriteLine("Expected result representation of the latest test run is:");
                string resultString = "";
                resultString += "new string[]{\"";
                for (int i = 0; i < _results.Count; i++)
                {
                    resultString += _results[i];
                    if (i < (_results.Count - 1))
                    {
                        resultString += "\",\"";
                    }
                    else
                    {
                        resultString += "\"";
                    }
                }
                resultString += "};";
                Debug.WriteLine(resultString);
                //System.Windows.Forms.Clipboard.SetDataObject(resultString);
            }
            else
            {
                string msg = "Incorrect number of elements in \"Expected\" results.";
                if (expected.Length != _results.Count)
                    Assert.IsTrue(false, msg);
                for (int i = 0; i < _results.Count; i++)
                {
                    if (!expected[i].Equals(_results[i]))
                    {
                        msg = "Argument mismatch in element " + i + " of the expected test results.";
                        Assert.IsTrue(false, msg);
                    }
                }
            }
        }

        private void PostTuple(IExecutive exec, object key)
        {
            _results.Add("" + exec.Now.Ticks + ":PT1:" + key.GetHashCode());
            Console.WriteLine(exec.Now + " : Posting Tuple w/ prikey of " + key + ".");
            _tsut.Post(key, DataFromKey(key), false);
            _results.Add("" + exec.Now.Ticks + ":PT2:" + key.GetHashCode());
            Console.WriteLine(exec.Now + " : Done posting Tuple w/ prikey of " + key + ".");
        }

        private void ReadTuple(IExecutive exec, object key)
        {
            _results.Add("" + exec.Now.Ticks + ":RT1:" + key.GetHashCode());
            Console.Write(exec.Now + " : Reading Tuple w/ prikey of " + key + ".");
            ITuple tuple = _tsut.Read(key, false);
            object data = (tuple == null ? null : tuple.Data);
            if (data != null)
            {
                Console.WriteLine(" Tuple data = " + data + ".");
                _results.Add("" + exec.Now.Ticks + ":RT2a:" + key.GetHashCode());
            }
            else
            {
                Console.WriteLine(" " + key + " is an unknown priKey.");
                _results.Add("" + exec.Now.Ticks + ":RT2b:" + key.GetHashCode());
            }
        }

        private void TakeTuple(IExecutive exec, object key)
        {
            Console.Write(exec.Now + " : Taking Tuple w/ prikey of " + key + ".");
            _results.Add("" + exec.Now.Ticks + ":TT1:" + key.GetHashCode());
            ITuple tuple = _tsut.Take(key, false);
            object data = (tuple == null ? null : tuple.Data);
            if (data != null)
            {
                Console.WriteLine(" Tuple data = " + data + ".");
                _results.Add("" + exec.Now.Ticks + ":TT2a:" + key.GetHashCode());
            }
            else
            {
                Console.WriteLine(" " + key + " is an unknown priKey.");
                _results.Add("" + exec.Now.Ticks + ":TT2b:" + key.GetHashCode());
            }
        }

        private void BlockingPostTuple(IExecutive exec, object key)
        {
            Console.WriteLine(exec.Now + " : Starting blocking post of Tuple w/ prikey of " + key + ".");
            _results.Add("" + exec.Now.Ticks + ":BPT1:" + key.GetHashCode());
            _tsut.Post(key, DataFromKey(key), true);
            Console.WriteLine(exec.Now + " : Done with blocking post of tuple data with key = " + key + ".");
            _results.Add("" + exec.Now.Ticks + ":BPT2:" + key.GetHashCode());
        }

        private void BlockingReadTuple(IExecutive exec, object key)
        {
            Console.WriteLine(exec.Now + " : Starting blocking read of Tuple w/ prikey of " + key + ".");
            _results.Add("" + exec.Now.Ticks + ":BRT1:" + key.GetHashCode());
            object data = _tsut.Read(key, true).Data;
            Console.WriteLine(exec.Now + " : Done with blocking post of tuple data = " + data + ".");
            _results.Add("" + exec.Now.Ticks + ":BRT2:" + key.GetHashCode());
        }

        private void BlockingTakeTuple(IExecutive exec, object key)
        {
            Console.WriteLine(exec.Now + " : Starting blocking take of Tuple w/ prikey of " + key + ".");
            _results.Add("" + exec.Now.Ticks + ":BTT1:" + key.GetHashCode());
            object data = _tsut.Take(key, true).Data;
            Console.WriteLine(exec.Now + " : Done with blocking take of tuple data = " + data + ".");
            _results.Add("" + exec.Now.Ticks + ":BTT2:" + key.GetHashCode());
        }

        private void BlockTilGoneTuple(IExecutive exec, object key)
        {
            Console.WriteLine(exec.Now + " : Starting wait for departure of Tuple w/ prikey of " + key + ".");
            _results.Add("" + exec.Now.Ticks + ":WTG1:" + key.GetHashCode());
            _tsut.BlockWhilePresent(key);
            Console.WriteLine(exec.Now + " : Done with wait for departure of Tuple w/ prikey of " + key + ".");
            _results.Add("" + exec.Now.Ticks + ":WTG2:" + key.GetHashCode());
        }

        private string DataFromKey(object key)
        {
            return "Data:" + key.ToString();
        }
    }
}
