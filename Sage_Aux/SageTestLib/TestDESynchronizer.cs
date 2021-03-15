/* This source code licensed under the GNU Affero General Public License */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

namespace Highpoint.Sage.SimCore
{

    [TestClass]
    public class DESynchTester
    {

        private int NUM_EVENTS = 12;
        private Random _random = new Random();
        private DetachableEventSynchronizer _des = null;
        public DESynchTester()
        {
            Init();
        }

        private int _submitted = 0;
        private int _synchronized = 0;
        private int _secondary = 0;
        private DateTime _synchtime = new DateTime();

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
        [Highpoint.Sage.Utility.FieldDescription("This test checks the submission of detached events and synchronization of those events with an ISynchChannel")]
        public void TestBaseFunctionality()
        {

            Model model = new Model();

            IExecutive exec = model.Executive;
            DateTime now = DateTime.Now;
            DateTime when;
            double priority;

            _des = new DetachableEventSynchronizer(model);

            for (int i = 0; i < NUM_EVENTS; i++)
            {
                when = new DateTime(now.Ticks + _random.Next());
                priority = _random.NextDouble();
                Debug.WriteLine("Primary requesting event service for " + when + ", at priority " + priority);
                object userData = null;
                if (_random.Next(5) < 2)
                {
                    ISynchChannel isc = _des.GetSynchChannel(priority);
                    userData = isc;
                    Debug.WriteLine("Creating synchronized event for time " + when);
                    _synchronized++;
                }
                exec.RequestEvent(new ExecEventReceiver(MyExecEventReceiver), when, priority, userData, ExecEventType.Detachable);
                _submitted++;
            }

            Assert.IsTrue(_submitted > 0, "There are no events submitted");
            Assert.IsTrue(_synchronized > 0, "There are no events synchronized");
            Assert.IsTrue(_secondary == 0, "There cannot be secondary events submitted yet");

            exec.Start();

            Assert.IsTrue(_submitted == 0, "Not all submitted events had been fired");
            Assert.IsTrue(_synchronized == 0, "Not all synchronized events had been fired");
            Assert.IsTrue(_secondary > 0, "There has not been a secondary events submitted");
        }

        private void MyExecEventReceiver(IExecutive exec, object userData)
        {
            if (userData == null)
            {
                DoUnsynchronized(exec, userData);
            }
            else
            {
                DoSynchronized(exec, (ISynchChannel)userData);
            }
        }
        private void DoUnsynchronized(IExecutive exec, object userData)
        {
            if (_random.NextDouble() > .15)
            {
                DateTime when = new DateTime(exec.Now.Ticks + _random.Next());
                Debug.WriteLine("Secondary requesting event service for " + when + ".");
                exec.RequestEvent(new ExecEventReceiver(MyExecEventReceiver), when, _random.NextDouble(), null, ExecEventType.Detachable);
                _submitted++;
                _secondary++;
            }
            Debug.WriteLine("Running event at time " + exec.Now + ", and priority level " + exec.CurrentPriorityLevel + " on thread " + System.Threading.Thread.CurrentThread.GetHashCode());
            _submitted--;
        }
        private void DoSynchronized(IExecutive exec, ISynchChannel isc)
        {
            Debug.WriteLine("Pausing synchronized event at time " + exec.Now + ", and priority level " + exec.CurrentPriorityLevel + " on thread " + System.Threading.Thread.CurrentThread.GetHashCode());
            isc.Synchronize();
            if (_synchtime == new DateTime())
            {
                _synchtime = exec.Now;
            }
            else
            {
                Assert.IsTrue(_synchtime.Equals(exec.Now), "Synchronized event did not fire at the synchronization time");
            }
            _synchronized--;
            _submitted--;
            Debug.WriteLine("Running synchronized event at time " + exec.Now + ", sequence number " + isc.Sequencer + " and priority level " + exec.CurrentPriorityLevel + " on thread " + System.Threading.Thread.CurrentThread.GetHashCode());
        }
    }
}

