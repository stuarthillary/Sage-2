/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.SimCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;


namespace SchedulerDemoMaterial
{

    [TestClass]
    public class TestActions
    {

        #region Predefined Timespans
        private static TimeSpan FIVE_MINS = TimeSpan.FromMinutes(05.0);
        private static TimeSpan TEN_MINS = TimeSpan.FromMinutes(10.0);
        private static TimeSpan FIFTEEN_MINS = TimeSpan.FromMinutes(15.0);
        private static TimeSpan TWENTY_MINS = TimeSpan.FromMinutes(20.0);
        #endregion

        [TestMethod]
        public void DoActionTest1()
        {

            #region Create Actions
            Action task1 = new Action("Task 1", FIVE_MINS, 0.0);
            Action task2 = new Action("Task 2", TEN_MINS, 0.0);
            Action task3 = new Action("Task 3", FIFTEEN_MINS, 0.0);
            Action task4 = new Action("Task 4", TEN_MINS, 0.0);
            Action task5 = new Action("Task 5", TWENTY_MINS, 0.0);
            Action task6 = new Action("Task 6", FIVE_MINS, 0.0);
            #endregion

            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            IAction scheme = new ActionList(task1, new ConcurrentActionSet(task2, task3), task4, new ParallelActionSet(task5, task6));
            exec.RequestEvent(new ExecEventReceiver(scheme.Run), DateTime.Now, 0.0, null, ExecEventType.Detachable);

            exec.Start();

        }

        public void DoActionTest2()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            IAction part1 = new ParallelActionSet(new Action("PreDelay", FIVE_MINS, 1.0), new Action("RscAcquire", TEN_MINS, 1.0));
            IAction part2 = new Action("PreDelay", TimeSpan.FromMinutes(5.0), 1.0);
            IAction part3 = new ConcurrentActionSet(new Action("XferIn", FIVE_MINS, 1.0), new Action("XferOut", FIFTEEN_MINS, 1.0));
            IAction part4 = new Action("PostDelay", TimeSpan.FromMinutes(5.0), 1.0);
            IAction part5 = new ParallelActionSet(new Action("RscRelease", FIVE_MINS, 1.0), new Action("PostDelay", TWENTY_MINS, 1.0));
            IAction scheme = new ActionList(part1, part2, part3, part4, part5);

            exec.RequestEvent(new ExecEventReceiver(scheme.Run), DateTime.Now, 0.0, null, ExecEventType.Detachable);
            exec.Start();
        }
    }


    public interface IAction
    {

        event ExecEventReceiver Starting;
        void Run(IExecutive exec, object userData);
        event ExecEventReceiver Finishing;
        string Name
        {
            get;
        }

    }

    public class Action : IAction
    {

        #region Private Fields
        private string _name;
        private TimeSpan _duration;
        private double _priority;
        #endregion

        public Action(string name, TimeSpan duration, double priority)
        {
            _name = name;
            _duration = duration;
            _priority = priority;
        }

        #region IAction Members
        public event Highpoint.Sage.SimCore.ExecEventReceiver Starting;
        public void Run(IExecutive exec, object userData)
        {
            Console.WriteLine(exec.Now + " : " + _name + " is starting.");
            if (Starting != null)
                Starting(exec, userData);

            Console.WriteLine(exec.Now + " : " + _name + " is pausing.");
            exec.CurrentEventController.SuspendUntil(exec.Now + _duration);

            if (Finishing != null)
                Finishing(exec, userData);
            Console.WriteLine(exec.Now + " : " + _name + " is completing.");
        }
        public event Highpoint.Sage.SimCore.ExecEventReceiver Finishing;
        public string Name
        {
            get
            {
                return _name;
            }
        }
        #endregion
    }

    public class ActionList : IAction
    {

        #region Private Fields
        private IAction[] _actions;
        private string _name;
        #endregion

        public ActionList(params IAction[] actions)
        {
            _actions = actions;
            _name = "[List] ";
            foreach (IAction action in _actions)
                _name += (" : " + action.Name);
        }

        #region IAction Members

        public event Highpoint.Sage.SimCore.ExecEventReceiver Starting;

        public void Run(IExecutive exec, object userData)
        {
            if (Starting != null)
                Starting(exec, userData);
            foreach (IAction action in _actions)
                action.Run(exec, userData);
            if (Finishing != null)
                Finishing(exec, userData);
        }

        public event Highpoint.Sage.SimCore.ExecEventReceiver Finishing;

        public string Name
        {
            get
            {
                return _name;
            }
        }

        #endregion

    }
    public class ParallelActionSet : IAction
    {

        #region Private Fields
        private IAction[] _actions;
        private IDetachableEventController _myIDEC;
        private object _lock;
        private int _remaining;
        private string _name;
        #endregion

        #region Constructors
        public ParallelActionSet(params IAction[] actions)
        {
            _actions = actions;
            _lock = new object();
            _remaining = 0;
            _name = "[Parallel]";
            foreach (IAction action in actions)
                _name += (" : " + action.Name);
        }
        #endregion

        #region IAction Members

        public event Highpoint.Sage.SimCore.ExecEventReceiver Starting;

        public void Run(IExecutive exec, object userData)
        {
            Console.WriteLine(exec.Now + " : " + _name + " is starting.");
            if (Starting != null)
                Starting(exec, userData);

            foreach (IAction action in _actions)
            {
                action.Finishing += new ExecEventReceiver(action_Finishing);
                exec.RequestEvent(new ExecEventReceiver(action.Run), exec.Now, 0.0, userData, ExecEventType.Detachable);
                _remaining++;
            }
            WaitForAllActionsToComplete(exec);

            if (Finishing != null)
                Finishing(exec, userData);
            Console.WriteLine(exec.Now + " : " + _name + " is completing.");
        }

        public event Highpoint.Sage.SimCore.ExecEventReceiver Finishing;

        public string Name
        {
            get
            {
                return _name;
            }
        }
        #endregion

        private void action_Finishing(IExecutive exec, object userData)
        {
            if (--_remaining == 0)
                _myIDEC.Resume();
        }

        private void WaitForAllActionsToComplete(IExecutive exec)
        {
            _myIDEC = exec.CurrentEventController;
            _myIDEC.Suspend();
        }
    }

    public class ConcurrentActionSet : IAction
    {

        #region Private Fields
        private IAction[] _actions;
        private ArrayList _suspendedIdecs;
        private object _lock;
        private int _remaining;
        private string _name;
        #endregion

        #region Constructors
        public ConcurrentActionSet(params IAction[] actions)
        {
            _actions = actions;
            _suspendedIdecs = new ArrayList();
            _lock = new object();
            _remaining = 0;
            _name = "[Parallel]";
            foreach (IAction action in actions)
                _name += (" : " + action.Name);
        }
        #endregion

        #region IAction Members

        public event Highpoint.Sage.SimCore.ExecEventReceiver Starting;

        public void Run(IExecutive exec, object userData)
        {
            Console.WriteLine(exec.Now + " : " + _name + " is starting.");
            if (Starting != null)
                Starting(exec, userData);

            foreach (IAction action in _actions)
            {
                action.Finishing += new ExecEventReceiver(action_Finishing);
                exec.RequestEvent(new ExecEventReceiver(action.Run), exec.Now, 0.0, userData, ExecEventType.Detachable);
                _remaining++;
            }
            _suspendedIdecs.Add(exec.CurrentEventController);
            exec.CurrentEventController.Suspend();

            if (Finishing != null)
                Finishing(exec, userData);
            Console.WriteLine(exec.Now + " : " + _name + " is completing.");
        }

        public event Highpoint.Sage.SimCore.ExecEventReceiver Finishing;

        public string Name
        {
            get
            {
                return _name;
            }
        }
        #endregion

        private void action_Finishing(IExecutive exec, object userData)
        {
            if (_suspendedIdecs.Count == _actions.Length)
            {
                foreach (IDetachableEventController idec in _suspendedIdecs)
                    idec.Resume();
            }
            else
            {
                _suspendedIdecs.Add(exec.CurrentEventController);
                exec.CurrentEventController.Suspend();
            }
        }
    }
}

