/* This source code licensed under the GNU Affero General Public License */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Highpoint.Sage.SimCore
{

    [TestClass]
    public class ExecTester
    {

        #region Private Fields
        private int NUM_EVENTS = 12;
        private Random _random = new Random(1000);
        private int _validateCount;
        private int _validatePriority;
        private DateTime _validateWhen;
        private bool _error;
        private ArrayList _validateUnRequest;

        private ExecEventType _execEventType = ExecEventType.Synchronous;
        #endregion Private Fields

        public ExecTester()
        {
            Init();
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

        /// <summary>
        /// Checks to see that an executive can store & service all submitted events.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks to see that an executive can store & service all submitted events.")]
        public void TestExecutiveCount()
        {

            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            DateTime now = DateTime.Now;
            DateTime when;
            double priority;

            // initialize validate variable
            _validateCount = 0;
            Debug.WriteLine("");
            Debug.WriteLine("Start test TestExecutiveCount");
            Debug.WriteLine("");

            for (int i = 0; i < NUM_EVENTS; i++)
            {
                when = new DateTime(now.Ticks + _random.Next());
                priority = _random.NextDouble();
                ++_validateCount;
                Debug.WriteLine("Primary requesting event number " + _validateCount);
                exec.RequestEvent(new ExecEventReceiver(ExecEventRecieverCount), when, priority, null, _execEventType);
            }

            if (_validateCount != NUM_EVENTS)
            {
                Debug.WriteLine("Number of submitted event requests don't equal supposed number of : " + NUM_EVENTS);
            }

            Debug.WriteLine("");

            exec.Start();

            // test validate variable
            Assert.IsTrue(0 == _validateCount, "Executive did not submit all events");

            Debug.WriteLine("");
        }

        private void ExecEventRecieverCount(IExecutive exec, object userData)
        {
            --_validateCount;
        }

        /// <summary>
        /// Checks to see that an executive can store & service all submitted events, 
        /// using RequestEvent method without the event type parameter.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks to see that an executive can store & service all submitted events, using RequestEvent method without the event type parameter.")]
        public void TestExecutiveCountDefaultParameter()
        {

            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            DateTime now = DateTime.Now;
            DateTime when;
            double priority;

            // initialize validate variable
            _validateCount = 0;
            Debug.WriteLine("");
            Debug.WriteLine("Start test ExecEventRecieverCountLessParameter");
            Debug.WriteLine("");

            for (int i = 0; i < NUM_EVENTS; i++)
            {
                when = new DateTime(now.Ticks + _random.Next());
                priority = _random.NextDouble();
                ++_validateCount;
                Debug.WriteLine("Primary requesting event number " + _validateCount);
                exec.RequestEvent(new ExecEventReceiver(ExecEventRecieverCountLessParameter), when, priority, null);
            }

            if (_validateCount != NUM_EVENTS)
            {
                Debug.WriteLine("Number of submitted event requests don't equal supposed number of : " + NUM_EVENTS);
            }

            Debug.WriteLine("");

            exec.Start();

            // test validate variable
            Assert.IsTrue(0 == _validateCount, "Executive did not submit all events");

            Debug.WriteLine("");
        }

        private void ExecEventRecieverCountLessParameter(IExecutive exec, object userData)
        {
            --_validateCount;
        }

        /// <summary>
        /// Checks to see that an executive can store & service all submitted events, 
        /// ordered by the requested callback priority at the same callback time.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks to see that an executive can store & service all submitted events, ordered by the requested callback priority at the same callback time.")]
        public void TestExecutivePriority()
        {

            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            DateTime now = DateTime.Now;
            int priority;

            // initialize validation variables
            _error = false;
            _validatePriority = 0;
            Debug.WriteLine("");
            Debug.WriteLine("Start test TestExecutivePriority");
            Debug.WriteLine("");

            for (int i = 0; i < NUM_EVENTS; i++)
            {
                priority = (int)(_random.NextDouble() * 100);
                if (_validatePriority < priority)
                {
                    _validatePriority = priority;
                }
                Debug.WriteLine("Primary requesting event service for " + now + ", at priority " + priority);
                exec.RequestEvent(new ExecEventReceiver(ExecEventRecieverPriority), now, priority, priority, _execEventType);
            }

            Debug.WriteLine("");

            exec.Start();

            Debug.WriteLine("");

            // test validate variable
            Assert.IsTrue(!_error, "Executive did not submit events in the order of the correct priority");

            Debug.WriteLine("");
        }

        public void ExecEventRecieverPriority(IExecutive exec, object userData)
        {
            if (_validatePriority < (int)userData)
            {
                _error = true;
            }
            _validatePriority = (int)userData;
            Debug.WriteLine("Primary fireing event with priority " + (int)userData);
        }

        /// <summary>
        /// Checks to see that an executive can store & service all submitted events, 
        /// ordered by the requested callback time.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks to see that an executive can store & service all submitted events, ordered by the requested callback time.")]
        public void TestExecutiveWhen()
        {

            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            DateTime now = DateTime.Now;
            DateTime when;

            // initialize validation variables
            _error = false;
            _validateWhen = new DateTime(now.Ticks);
            Debug.WriteLine("");
            Debug.WriteLine("Start test TestExecutiveWhen");
            Debug.WriteLine("");

            for (int i = 0; i < NUM_EVENTS; i++)
            {
                when = new DateTime(now.Ticks + _random.Next());
                Debug.WriteLine("Primary requesting event service for " + when);
                //if (m_validateWhen.Ticks < when.Ticks) {m_validateWhen = when;}
                exec.RequestEvent(new ExecEventReceiver(ExecEventRecieverWhen), when, 0, when, _execEventType);
            }

            Debug.WriteLine("");

            exec.Start();

            Debug.WriteLine("");

            // test validation variable
            Assert.IsTrue(!_error, "Executive did not submit events in correct date/time order");

            Debug.WriteLine("");
        }

        public void ExecEventRecieverWhen(IExecutive exec, object userData)
        {
            if (_validateWhen.Ticks > ((DateTime)userData).Ticks)
            {
                _error = true;
            }
            _validateWhen = (DateTime)userData;
            Debug.WriteLine("Primary fireing event at date/time " + (DateTime)userData);
        }

        /// <summary>
        /// Checks to see that an executive can unrequest submitted events, 
        /// identifying the events by a hash code
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks to see that an executive can unrequest submitted events, identifying the events by a hash code.")]
        public void TestExecutiveUnRequestHash()
        {

            string[] eventUserData = new string[] { "Cat", "Dog", "Bat", "Frog", "Mink", "Bee", "Bird", "Worm", "Horse", "Moose", "Bear", "Platypus" };
            ArrayList eventsToRemove = new ArrayList(new string[] { "Cat", "Bat", "Bee", "Bird", "Bear", "Platypus" });
            //ArrayList eventsThatShouldRemain = new ArrayList(new string[]{"Dog","Frog","Mink","Worm","Horse","Moose"});

            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            DateTime now = DateTime.Now;
            DateTime when;
            double priority;

            // initialize validation variables
            _error = false;
            _validateUnRequest = new ArrayList();
            Debug.WriteLine("");
            Debug.WriteLine("Start test TestExecutiveUnRequestHash");
            Debug.WriteLine("");

            ArrayList eventIDsForRemoval = new ArrayList();
            foreach (string eud in eventUserData)
            {
                when = new DateTime(now.Ticks + _random.Next());
                priority = _random.NextDouble();
                Trace.Write("Primary requesting event service with user data \"" + eud + "\", and eventID ");
                long eventID = exec.RequestEvent(new ExecEventReceiver(ExecEventRecieverUnRequestHash), when, priority, eud, _execEventType);
                Debug.WriteLine(eventID + ".");
                if (eventsToRemove.Contains(eud))
                {
                    eventIDsForRemoval.Add(eventID);
                    Debug.WriteLine("\tWe will be requesting the removal of this event.");
                }
            }

            foreach (long eventID in eventIDsForRemoval)
            {
                Debug.WriteLine("Unrequesting event # " + eventID);
                _validateUnRequest.Add(eventID);
                exec.UnRequestEvent(eventID);
            }

            Debug.WriteLine("");

            exec.Start();

            Debug.WriteLine("");

            // test validation variable
            Assert.IsTrue(!_error, "Executive did fire a unrequested event");

            Debug.WriteLine("");
        }

        public void ExecEventRecieverUnRequestHash(IExecutive exec, object userData)
        {
            if (_validateUnRequest.Contains(userData))
            {
                _error = true;
            }
            Debug.WriteLine("Primary firing event with user data = \"" + userData + "\"");
        }

        /// <summary>
        /// Checks to see that an executive can unrequest submitted events, 
        /// identifying the events by a target object.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks to see that an executive can unrequest submitted events, identifying the events by a target object.")]
        public void TestExecutiveUnRequestTarget()
        {

            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            DateTime now = DateTime.Now;
            DateTime when;
            double priority;
            OtherTarget ot = null;

            // initialize validation variables
            _error = false;
            _validateUnRequest = new ArrayList();
            Debug.WriteLine("");
            Debug.WriteLine("Start test TestExecutiveUnRequestTarget");
            Debug.WriteLine("");

            for (int i = 0; i < NUM_EVENTS; i++)
            {
                when = new DateTime(now.Ticks + _random.Next());
                priority = _random.NextDouble();
                Debug.WriteLine("Primary requesting event service " + i);
                switch (i)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 5:
                    case 7:
                    case 11:
                        _validateUnRequest.Add(i);
                        ot = new OtherTarget(_validateUnRequest, _error);
                        ExecEventReceiver eer = new ExecEventReceiver(ot.ExecEventRecieverUnRequestEventReceiver);
                        exec.RequestEvent(eer, when, priority, i, _execEventType);
                        exec.UnRequestEvents(ot);
                        break;
                    default:
                        exec.RequestEvent(new ExecEventReceiver(this.ExecEventRecieverUnRequestEventReceiver), when, priority, i, _execEventType);
                        break;
                };
            }


            Debug.WriteLine("");

            // AEL			exec.UnRequestEvents(new OtherTarget(m_validateUnRequest, m_error));

            exec.Start();

            Debug.WriteLine("");

            // test validation variable
            Assert.IsTrue(!_error, "Executive did fire a unrequested event");

            Debug.WriteLine("");
        }

        /// <summary>
        /// Checks to see that an executive can unrequest submitted events, identifying the events by a delegate method.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks to see that an executive can unrequest submitted events, identifying the events by a delegate method.")]
        public void TestExecutiveUnRequestDelegate()
        {

            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            DateTime now = DateTime.Now;
            DateTime when;
            double priority;

            // initialize validation variables
            _error = false;
            _validateUnRequest = new ArrayList();
            Debug.WriteLine("");
            Debug.WriteLine("Start test TestExecutiveUnRequestDelegate");
            Debug.WriteLine("");

            for (int i = 0; i < NUM_EVENTS; i++)
            {
                when = new DateTime(now.Ticks + _random.Next());
                priority = _random.NextDouble();
                Debug.WriteLine("Primary requesting event service " + i);
                switch (i)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 5:
                    case 7:
                    case 11:
                        _validateUnRequest.Add(i);
                        ExecEventReceiver eer = new ExecEventReceiver(this.ExecEventRecieverUnRequestDelegate);
                        exec.RequestEvent(eer, when, priority, i, _execEventType);
                        exec.UnRequestEvents((Delegate)eer);
                        break;
                    default:
                        exec.RequestEvent(new ExecEventReceiver(this.ExecEventRecieverUnRequestEventReceiver), when, priority, i, _execEventType);
                        break;
                };
            }


            Debug.WriteLine("");

            // AEL		exec.UnRequestEvents((Delegate)(new ExecEventReceiver(this.ExecEventRecieverUnRequestDelegate)));

            exec.Start();

            Debug.WriteLine("");

            // test validation variable
            Assert.IsTrue(!_error, "Executive did fire a unrequested event");

            Debug.WriteLine("");
        }

        /// <summary>
        /// Checks to see that an executive can start, then stop and restart.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks to see that an executive can start, then stop and restart.")]
        public void TestExecutiveStopStart()
        {

            // use System.Threading;

            DateTime startTime;
            System.Threading.Thread starter, interrupter;
            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            // First get the duration w/o pause.
            startTime = DateTime.Now;
            starter = new System.Threading.Thread(new ParameterizedThreadStart(StartExec));
            starter.Start(exec);
            starter.Join();
            TimeSpan shortDuration = DateTime.Now - startTime;
            Debug.WriteLine("Duration w/o pause is " + shortDuration.TotalSeconds + " seconds.");
            exec.Reset();

            // Now get the duration w/ pause.
            startTime = DateTime.Now;
            starter = new System.Threading.Thread(new ParameterizedThreadStart(StartExec));
            interrupter = new System.Threading.Thread(new ParameterizedThreadStart(StopAndRestartExec));
            starter.Start(exec);
            interrupter.Start(exec);
            interrupter.Join();
            TimeSpan pauseDuration = DateTime.Now - startTime;

            // Finally, assess the comparative durations to ensure the test passed.
            Debug.WriteLine("Total test duration was " + pauseDuration.TotalSeconds + " seconds.");
            TimeSpan minAcceptableDuration = shortDuration + TimeSpan.FromMilliseconds(1500);
            Assert.IsTrue(pauseDuration > minAcceptableDuration,
                "Test duration of less than " + minAcceptableDuration.TotalSeconds
                + " seconds indicates a failure to properly stop and restart.");
        }


        /// <summary>
        /// Checks to see that an executive can start, then stop and restart.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks to see that an executive can start, then stop and restart.")]
        public void TestExecutivePauseResume()
        {
            DateTime startTime;
            System.Threading.Thread starter, pauser;
            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            InstrumentExecutiveStates(exec);

            // First get the duration w/o pause.
            startTime = DateTime.Now;
            starter = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(StartExec));
            starter.Start(exec);
            starter.Join();
            TimeSpan shortDuration = DateTime.Now - startTime;
            Debug.WriteLine("Duration w/o pause is " + shortDuration.TotalSeconds + " seconds.");
            exec.Reset();

            // Now get the duration w/ pause.
            startTime = DateTime.Now;
            starter = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(StartExec));
            pauser = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(PauseAndResumeExec));
            starter.Start(exec);
            pauser.Start(exec);
            starter.Join();
            TimeSpan pauseDuration = DateTime.Now - startTime;

            // Finally, assess the comparative durations to ensure the test passed.
            Debug.WriteLine("Total test duration was " + pauseDuration.TotalSeconds + " seconds.");
            TimeSpan minAcceptableDuration = shortDuration + TimeSpan.FromMilliseconds(1500);
            Assert.IsTrue(pauseDuration > minAcceptableDuration,
                "Test duration of less than " + minAcceptableDuration.TotalSeconds + " seconds indicates a failure to properly stop and restart.");
        }

        private void InstrumentExecutiveStates(IExecutive exec)
        {
            exec.ExecutiveStarted_SingleShot += new ExecutiveEvent(delegate (IExecutive e)
            {
                Debug.WriteLine("Executive Started (single shot).");
            });
            exec.ExecutiveStarted += new ExecutiveEvent(delegate (IExecutive e)
            {
                Debug.WriteLine("Executive Started.");
            });
            exec.ExecutivePaused += new ExecutiveEvent(delegate (IExecutive e)
            {
                Debug.WriteLine("Executive Paused.");
            });
            exec.ExecutiveResumed += new ExecutiveEvent(delegate (IExecutive e)
            {
                Debug.WriteLine("Executive Resumed.");
            });
            exec.ExecutiveStopped += new ExecutiveEvent(delegate (IExecutive e)
            {
                Debug.WriteLine("Executive Stopped.");
            });
            exec.ExecutiveFinished += new ExecutiveEvent(delegate (IExecutive e)
            {
                Debug.WriteLine("Executive Finished.");
            });
            exec.ExecutiveAborted += new ExecutiveEvent(delegate (IExecutive e)
            {
                Debug.WriteLine("Executive Aborted.");
            });
        }

        #region TestExecutiveStopStart() and TestExecutivePauseResume() support methods.

        private void StartExec(object obj)
        {
            IExecutive exec = (IExecutive)obj;
            Debug.WriteLine("\r\n" + "Starting exec..." + "\r\n");
            DateTime startTime = new DateTime(2006, 5, 16);
            exec.RequestEvent(new ExecEventReceiver(SteadyStateEventStream), startTime, 1, 400);
            exec.Start();
        }

        private void StopAndRestartExec(object obj)
        {
            System.Threading.Thread.Sleep(1000);
            IExecutive exec = (IExecutive)obj;
            Debug.WriteLine("\r\n" + "Pausing for two seconds..." + "\r\n");
            Debug.WriteLine("Before pause, Exec state is " + exec.State);
            exec.Stop();
            System.Threading.Thread.Sleep(2000);
            Debug.WriteLine("After pause, Exec state is " + exec.State);
            Debug.WriteLine("\r\n" + "Resuming..." + "\r\n");
            exec.Start();
            Debug.WriteLine("Exec state is now " + exec.State);
        }

        private void PauseAndResumeExec(object obj)
        {
            System.Threading.Thread.Sleep(1000);
            IExecutive exec = (IExecutive)obj;
            Debug.WriteLine("\r\n" + "Pausing for two seconds..." + "\r\n");
            Debug.WriteLine("Before pause, Exec state is " + exec.State);
            exec.Pause();
            System.Threading.Thread.Sleep(2000);
            Debug.WriteLine("After pause, Exec state is " + exec.State);
            Debug.WriteLine("\r\n" + "Resuming..." + "\r\n");
            exec.Resume();
            Debug.WriteLine("Exec state is now " + exec.State);
        }

        private void SteadyStateEventStream(IExecutive exec, object userData)
        {
            int evtNum = (int)userData;
            if (evtNum % 20 == 0)
            {
                Debug.WriteLine(evtNum);
            }
            if (evtNum > 0)
            {
                evtNum--;
                DateTime nextEventTime = exec.Now + TimeSpan.FromMinutes(5);
                exec.RequestEvent(new ExecEventReceiver(SteadyStateEventStream),
                    nextEventTime, 1, evtNum);
            }
            System.Threading.Thread.Sleep(10); // Just to make the test discernible to the eye.
        }

        #endregion

        /// <summary>
        /// Checks to see that an executive can unrequest submitted events, 
        /// identifying the events by a selector.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks to see that an executive can unrequest submitted events, identifying the events by a selector.")]
        public void TestExecutiveUnRequestSelector()
        {

            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            DateTime now = DateTime.Now;
            DateTime when;
            double priority;
            //			IExecEventSelector ees = null;

            // initialize validation variables
            _error = false;
            _validateUnRequest = new ArrayList();
            Debug.WriteLine("");
            Debug.WriteLine("Start test TestExecutiveUnRequestSelector");
            Debug.WriteLine("");

            for (int i = 0; i < NUM_EVENTS; i++)
            {
                when = new DateTime(now.Ticks + _random.Next());
                priority = _random.NextDouble();
                Debug.WriteLine("Primary requesting event service " + i);
                switch (i)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 5:
                    case 7:
                    case 11:
                        //						m_validateUnRequest.Add(i);
                        //						ees = new ExecEventSelectorByTargetType(this.GetType());
                        //						ExecEventReceiver eer = new ExecEventReceiver(ees.SelectThisEvent(eer,when,priority,i,m_execEventType));
                        //						exec.RequestEvent(eer,when,priority,i,m_execEventType);
                        //						exec.UnRequestEvents(ees);
                        break;
                    default:
                        exec.RequestEvent(new ExecEventReceiver(this.ExecEventRecieverUnRequestEventReceiver), when, priority, i, _execEventType);
                        break;
                };
            }


            Debug.WriteLine("");

            // AEL			exec.UnRequestEvents(new ExecEventSelectorByTargetType(this));

            exec.Start();

            Debug.WriteLine("");

            // test validation variable
            Assert.IsTrue(!_error, "Executive fired a unrequested event");

            Debug.WriteLine("");
        }

        #region TestExecutiveUnRequestSelector() support methods.

        public void ExecEventRecieverUnRequestEventReceiver(IExecutive exec, object userData)
        {
            if (_validateUnRequest.Contains(userData))
            {
                _error = true;
            }
            Debug.WriteLine("Primary firing event number" + (int)userData);
        }

        public void ExecEventRecieverUnRequestDelegate(IExecutive exec, object userData)
        {
            if (_validateUnRequest.Contains(userData))
            {
                _error = true;
            }
            Debug.WriteLine("ERROR: Primary firing unrequested event number" + (int)userData);
        }

        #endregion

        /// <summary>
        /// Checks to see that an executive can handle seperate threads.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks to see that an executive can handle seperate threads.")]
        public void TestThreadSepFunctionality()
        {

            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            DateTime now = DateTime.Now;
            DateTime when;
            double priority;

            for (int i = 0; i < NUM_EVENTS; i++)
            {
                when = new DateTime(now.Ticks + _random.Next());
                priority = _random.NextDouble();
                Debug.WriteLine("Primary requesting detachable event service for " + when + ", at priority " + priority);
                exec.RequestEvent(new ExecEventReceiver(TimeSeparatedTask), when, priority, "Task " + i, ExecEventType.Detachable);
            }

            exec.Start();

            Debug.WriteLine("\r\n\r\n\r\nNow going to do it again after a 1.5 second pause.\r\n\r\n\r\n");
            System.Threading.Thread.Sleep(1500);

            exec = ExecFactory.Instance.CreateExecutive();
            now = DateTime.Now;

            for (int i = 0; i < NUM_EVENTS; i++)
            {
                when = new DateTime(now.Ticks + _random.Next());
                priority = _random.NextDouble();
                Debug.WriteLine("Primary requesting detachable event service for " + when + ", at priority " + priority);
                exec.RequestEvent(new ExecEventReceiver(TimeSeparatedTask), when, priority, "Task " + i, ExecEventType.Detachable);
            }

            exec.Start();

        }

        #region Test Times
        readonly string[] _testTimes = new string[]{"9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:21:18 AM",
                                             "9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:21:18 AM",
                                             "9/1/1998 7:00:00 AM",
                                             "9/1/1998 12:21:18 AM",
                                             "9/1/1998 8:00:00 AM",
                                             "9/1/1998 12:21:18 AM",
                                             "9/1/1998 8:00:00 AM",
                                             "9/1/1998 12:21:18 AM",
                                             "9/1/1998 8:00:00 AM",
                                             "9/1/1998 8:00:00 AM",
                                             "9/1/1998 8:00:00 AM",
                                             "9/1/1998 8:00:00 AM",
                                             "9/1/1998 8:00:00 AM",
                                             "9/1/1998 12:00:00 PM",
                                             "9/1/1998 12:00:00 PM",
                                             "9/1/1998 12:00:00 PM",
                                             "9/1/1998 12:27:31 AM",
                                             "9/1/1998 12:42:36 AM",
                                             "9/1/1998 12:27:29 AM",
                                             "9/1/1998 12:42:36 AM",
                                             "9/1/1998 12:27:26 AM",
                                             "9/1/1998 12:42:36 AM",
                                             "9/1/1998 12:27:24 AM",
                                             "9/1/1998 12:42:36 AM",
                                             "9/1/1998 12:27:23 AM",
                                             "9/1/1998 12:42:36 AM",
                                             "9/1/1998 3:48:41 AM",
                                             "9/1/1998 3:48:42 AM",
                                             "9/1/1998 3:48:44 AM",
                                             "9/1/1998 3:48:47 AM",
                                             "9/1/1998 3:48:49 AM",
                                             "9/1/1998 12:48:58 AM",
                                             "9/1/1998 1:03:54 AM",
                                             "9/1/1998 12:48:56 AM",
                                             "9/1/1998 1:03:54 AM",
                                             "9/1/1998 12:45:20 AM",
                                             "9/1/1998 1:06:52 AM",
                                             "9/1/1998 1:06:39 AM"};
        #endregion Test Times

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the Heap collection.")]
        public void RecreateFailure()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive("Highpoint.Sage.SimCore.ExecutiveFastLight, Sage", Guid.NewGuid());
            foreach (string s in _testTimes)
            {
                DateTime dt = DateTime.Parse(s);
                exec.RequestEvent(new ExecEventReceiver(DoIt), dt, 0.0, dt.ToString());
            }


            _lastNow = exec.Now;
            exec.Start();
        }

        DateTime _lastNow;
        private void DoIt(IExecutive exec, object userData)
        {
            string errMsg = "";
            if (exec.Now < _lastNow)
                errMsg = "<-- CAUSALITY VIOLATION!";
            _lastNow = exec.Now;
            Console.WriteLine("At " + exec.Now + ", servicing event that was requested for " + userData.ToString() + errMsg);
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test different mechanisms for acquiring an instance of IExecutive.")]
        public void TestExecAcquisition()
        {

            // using Highpoint.Sage.SimCore;

            // Obtain an executive of the default type and unspecified Guid from the factory.
            IExecutive exec1 = ExecFactory.Instance.CreateExecutive();
            // ... or ...
            // Obtain an executive of the default type and specified Guid from the factory.
            IExecutive exec2 = ExecFactory.Instance.CreateExecutive(Guid.NewGuid());
            // ... or ...
            // Obtain an executive of the specified type and Guid from the factory.
            IExecutive exec3 = ExecFactory.Instance.CreateExecutive("Highpoint.Sage.SimCore.Executive",
                                                                    Guid.NewGuid());
            // ... or ...
            // Obtain an executive of the specified type and Guid from the factory.
            IExecutive exec4 = ExecFactory.Instance.CreateExecutive("Highpoint.Sage.SimCore.ExecutiveFastLight",
                                                                    Guid.NewGuid());

            Console.WriteLine(exec1.ToString());
            Console.WriteLine(exec2.ToString());
            Console.WriteLine(exec3.ToString());
            Console.WriteLine(exec4.ToString());
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the event that is supposed to fire each time the clock is supposed to change.")]
        public void TestClockAboutToChangeEvent()
        {

            IExecutive exec1 = ExecFactory.Instance.CreateExecutive();
            exec1.ClockAboutToChange += new ExecutiveEvent(exec1_ClockAboutToChange);
            _result = "";
            DateTime t0 = new DateTime(2007, 5, 16, 12, 34, 56);

            // Daemon event. Should never fire.
            exec1.RequestDaemonEvent(new ExecEventReceiver(MyExecEventReceiver2), new DateTime(2025, 12, 25), 0.0, null);

            int[] increments = new int[] { 1, 2, 3, 0, 0, 2, 0, 2, 3, 0, 0, 0, 4, 0 };
            foreach (int increment in increments)
            {
                exec1.RequestEvent(new ExecEventReceiver(MyExecEventReceiver2), t0, 0.0, null);
                t0 += TimeSpan.FromMinutes(increment);
            }

            exec1.Start();

            Console.WriteLine(_result);

            Assert.IsTrue(_result.Equals("16/05/2007 12:34:56 PM : Event is firing.\r\n\tClock, currently at 16/05/2007 12:34:56 PM, is about to change.\r\n16/05/2007 12:35:56 PM : Event is firing.\r\n\tClock, currently at 16/05/2007 12:35:56 PM, is about to change.\r\n16/05/2007 12:37:56 PM : Event is firing.\r\n\tClock, currently at 16/05/2007 12:37:56 PM, is about to change.\r\n16/05/2007 12:40:56 PM : Event is firing.\r\n16/05/2007 12:40:56 PM : Event is firing.\r\n16/05/2007 12:40:56 PM : Event is firing.\r\n\tClock, currently at 16/05/2007 12:40:56 PM, is about to change.\r\n16/05/2007 12:42:56 PM : Event is firing.\r\n16/05/2007 12:42:56 PM : Event is firing.\r\n\tClock, currently at 16/05/2007 12:42:56 PM, is about to change.\r\n16/05/2007 12:44:56 PM : Event is firing.\r\n\tClock, currently at 16/05/2007 12:44:56 PM, is about to change.\r\n16/05/2007 12:47:56 PM : Event is firing.\r\n16/05/2007 12:47:56 PM : Event is firing.\r\n16/05/2007 12:47:56 PM : Event is firing.\r\n16/05/2007 12:47:56 PM : Event is firing.\r\n\tClock, currently at 16/05/2007 12:47:56 PM, is about to change.\r\n16/05/2007 12:51:56 PM : Event is firing.\r\n", StringComparison.Ordinal));
        }

        private string _result = null;
        void exec1_ClockAboutToChange(IExecutive exec)
        {
            _result += ("\tClock, currently at " + getExecNow(exec) + ", is about to change.\r\n");
        }

        void MyExecEventReceiver2(IExecutive exec, object userData)
        {
            _result += (getExecNow(exec) + " : Event is firing.\r\n");
        }

        private string getExecNow(IExecutive exec)
        {
            return exec.Now.ToString("dd/MM/yyyy hh:mm:ss tt");
        }

        private int _exec1_ExecutiveStarted_SingleShot_Count = 0;
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the single shot event (fires once when the first event is about to fire, then unregisters.")]
        public void TestSingleShotEvent()
        {
            IExecutive exec1 = ExecFactory.Instance.CreateExecutive();
            _exec1_ExecutiveStarted_SingleShot_Count = 0;
            exec1.ExecutiveStarted_SingleShot += new ExecutiveEvent(exec1_ExecutiveStarted_SingleShot);
            exec1.Start();
            exec1.Start();
            Debug.Assert(_exec1_ExecutiveStarted_SingleShot_Count == 1);
        }

        void exec1_ExecutiveStarted_SingleShot(IExecutive exec)
        {
            _exec1_ExecutiveStarted_SingleShot_Count++;
        }

        private int _eventCountCeil = 100000;
        private double _pctSynchronous = 0.50;
        private double[] _arrPctSynch = new double[] { 1.0, 0.5, 0.2, 0.1, 0.0 };
        private int[] _arrEcc = new int[] { 10000, 100000, 1000000 };

        public void TestPerformanceMultiple()
        {
            foreach (double pctSynch in _arrPctSynch)
            {
                _pctSynchronous = pctSynch;
                foreach (int ecc in _arrEcc)
                {
                    _eventCountCeil = ecc;
                    TestPerformance();
                }
            }
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the Executive's capability to run fast with a range of event type mixes.")]
        public void TestPerformance()
        {
            IExecutive exec1 = ExecFactory.Instance.CreateExecutive("Highpoint.Sage.SimCore.Executive", Guid.NewGuid());
            Randoms.RandomServer rsvr = new Highpoint.Sage.Randoms.RandomServer(012345, 1000);
            Randoms.IRandomChannel rch = rsvr.GetRandomChannel(987654321, 1000);
            DateTime timeCursor = new DateTime(2009, 1, 1, 0, 0, 0);

            _eventCountCeil = 180000;
            _pctSynchronous = 0.0;

            for (int i = 0; i < 100; i++)
            {
                timeCursor += TimeSpan.FromMinutes(rch.NextDouble() * 100);
                exec1.RequestEvent(new ExecEventReceiver(PerfTestExecute), timeCursor, 0.0, rch);
            }
            DateTime then = DateTime.Now;
            exec1.Start();
            TimeSpan howLong = DateTime.Now - then;
            Console.WriteLine("Serviced " + exec1.EventCount + " events in " + howLong.TotalMilliseconds + " msec.");
        }

        private void PerfTestExecute(IExecutive exec, object userData)
        {
            Randoms.IRandomChannel rch = (Randoms.IRandomChannel)userData;
            if (rch.NextDouble() < 0.405 && exec.EventCount < _eventCountCeil)
            {
                int nNewEvents = rch.Next(1, 5);
                for (int i = 0; i < nNewEvents; i++)
                {
                    DateTime when = exec.Now + TimeSpan.FromMinutes(rch.NextDouble() * 100);
                    if (rch.NextDouble() < _pctSynchronous)
                    {
                        exec.RequestEvent(new ExecEventReceiver(PerfTestExecute), when, 0.0, rch);
                    }
                    else
                    {
                        exec.RequestEvent(new ExecEventReceiver(PerfTestExecute), when, 0.0, rch, ExecEventType.Detachable);
                    }
                }
            }
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the Executive's capability to execute a simple event join.")]
        public void TestEventJoinDetachable()
        {
            IExecutive exec1 = ExecFactory.Instance.CreateExecutive("Highpoint.Sage.SimCore.Executive", Guid.NewGuid());
            DateTime setupTime = new DateTime(2008, 11, 24, 12, 15, 44);
            ExecEventType eet = ExecEventType.Detachable; // Don't change this one. Join must be done on a detachable event.
            exec1.RequestEvent(new ExecEventReceiver(JoinDetachableSetup), setupTime, 0.0, null, eet);
            exec1.Start();

        }

        private void JoinDetachableSetup(IExecutive exec, object userData)
        {
            DateTime[] whens = new DateTime[3];
            whens[0] = new DateTime(2008, 11, 25, 12, 15, 44);
            whens[1] = new DateTime(2008, 11, 26, 12, 15, 44);
            whens[2] = new DateTime(2008, 11, 27, 12, 15, 44);
            List<long> eventKeys = new List<long>();
            ExecEventType eet = ExecEventType.Synchronous;
            for (int i = 0; i < 3; i++)
            {
                if (eet == ExecEventType.Synchronous)
                {
                    eventKeys.Add(exec.RequestEvent(new ExecEventReceiver(DoItWithoutSuspension), whens[i], 0.0, null, eet));
                }
                else if (eet == ExecEventType.Detachable)
                {
                    eventKeys.Add(exec.RequestEvent(new ExecEventReceiver(DoItWithSuspension), whens[i], 0.0, null, eet));
                }
            }
            Console.WriteLine(exec.Now + " : Waiting to join.");
            exec.Join(eventKeys.ToArray());
            Console.WriteLine(exec.Now + " : Done waiting to join.");
        }

        private void DoItWithSuspension(IExecutive exec, object userData)
        {
            Console.WriteLine(exec.Now + " : Starting \"DoItWithSuspension\"");
            exec.CurrentEventController.SuspendUntil(exec.Now + TimeSpan.FromMinutes(5.0));
            Console.WriteLine(exec.Now + " : Finished \"DoItWithSuspension\"");
        }

        private void DoItWithoutSuspension(IExecutive exec, object userData)
        {
            Console.WriteLine(exec.Now + " : Doing \"DoItWithoutSuspension\"");
        }

        #region Internal Methods

        private void TimeSeparatedTask(IExecutive exec, object userData)
        {
            IDetachableEventController dec = exec.CurrentEventController;

            Debug.WriteLine(exec.Now + " : " + userData.ToString() + " performing initialization of detachable task on thread " + System.Threading.Thread.CurrentThread.GetHashCode());

            while (_random.Next(3) < 2)
            {

                DateTime when = exec.Now + TimeSpan.FromDays(1.5);

                Debug.WriteLine("Suspending task until " + when);

                dec.SuspendUntil(when);

                Debug.WriteLine(exec.Now + " : " + userData.ToString() + " performing continuation of detachable task on thread " + System.Threading.Thread.CurrentThread.GetHashCode());
            }
        }

        private void MyExecEventReceiver(IExecutive exec, object userData)
        {
            if (_random.NextDouble() > .15)
            {
                DateTime when = new DateTime(exec.Now.Ticks + _random.Next());
                Debug.WriteLine("Secondary requesting event service for " + when + ".");
                exec.RequestEvent(new ExecEventReceiver(MyExecEventReceiver), when, _random.NextDouble(), null, _execEventType);
            }

            Debug.WriteLine("Running event at time " + exec.Now + ", and priority level " + exec.CurrentPriorityLevel + " on thread " + System.Threading.Thread.CurrentThread.GetHashCode());

            //if ( m_random.NextDouble() < .05 ) {
            //    Debug.WriteLine("Putting task to sleep at time " + exec.Now + ".");
            //Thread.CurrentThread.Suspend();
            //    Thread.Sleep(1000);
            //}
        }

        #endregion
    }

    public class OtherTarget
    {

        ArrayList _validateUnRequest;
        bool _error;

        public OtherTarget(ArrayList pvalidateUnRequest, bool perror)
        {
            _validateUnRequest = pvalidateUnRequest;
            _error = perror;
        }

        public void ExecEventRecieverUnRequestEventReceiver(IExecutive exec, object userData)
        {
            if (_validateUnRequest.Contains(userData))
            {
                _error = true;
            }
            Debug.WriteLine("ERROR: Primary firing unrequested event number" + (int)userData);
        }

    }

    public class ExecEventSelectorByTargetType : IExecEventSelector
    {

        private Type _type;

        public ExecEventSelectorByTargetType(System.Type targetType)
        {

            _type = targetType;

        }

        #region IExecEventSelector Members

        public bool SelectThisEvent(Highpoint.Sage.SimCore.ExecEventReceiver eer, DateTime when, double priority, object userData, Highpoint.Sage.SimCore.ExecEventType eet)
        {

            return (_type.Equals(eer.Target.GetType()));

        }

        #endregion

    }

}
