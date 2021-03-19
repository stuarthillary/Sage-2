/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.SimCore;
//using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

//using ProcessStep = Highpoint.Sage.Servers.SimpleServerWithPreQueue;

namespace Highpoint.Sage.Utility
{

    /// <summary>
    /// Summary description for zTestLocalEventQueue.
    /// </summary>
    [TestClass]
    public class LocalEventQueueTester : IHasName
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

        private int _numEvents;
        private LocalEventQueue _leq;

        [TestMethod]
        public void TestLocalEventQueue()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            _numEvents = 10;
            _leq = new LocalEventQueue(exec, 4, new ExecEventReceiver(DoSomething));

            DateTime when = DateTime.Now;
            _leq.Enqueue(_numEvents--, when);
            Console.WriteLine(_leq.EarliestCompletionTime.ToString());

            when += TimeSpan.FromMinutes(5);
            _leq.Enqueue(_numEvents--, when);

            when += TimeSpan.FromMinutes(5);
            _leq.Enqueue(_numEvents--, when);

            exec.Start();

        }

        [TestMethod]
        public void TestLocalEventQueue2()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            _numEvents = 10;
            _leq = new LocalEventQueue(exec, 2, new ExecEventReceiver(DoSomething));

            DateTime when = DateTime.Now;
            _leq.Enqueue(_numEvents--, when);
            Console.WriteLine(_leq.EarliestCompletionTime.ToString());

            when += TimeSpan.FromMinutes(5);
            _leq.Enqueue(_numEvents--, when);
            Console.WriteLine(_leq.EarliestCompletionTime.ToString());

            when += TimeSpan.FromMinutes(5);
            _leq.Enqueue(_numEvents--, when);
            Console.WriteLine(_leq.EarliestCompletionTime.ToString());

            exec.Start();

        }

        private void DoSomething(IExecutive exec, object userData)
        {
            string msg = "";
            if (!_leq.IsEmpty)
                msg = " - the new head of the event queue will happen at " + _leq.EarliestCompletionTime.ToString();
            Console.WriteLine(exec.Now.ToString() + " : Receiving event " + userData.ToString() + msg + ".");
            if (_numEvents > 0)
            {
                DateTime when = exec.Now + TimeSpan.FromMinutes(10);
                _leq.Enqueue(_numEvents--, when);
            }
        }

        public string Name
        {
            get
            {
                return "Local event queue tester";
            }
        }
    }
}
