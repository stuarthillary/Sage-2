/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections.Generic;
// ReSharper disable RedundantDefaultMemberInitializer

namespace Highpoint.Sage.SimCore
{
    internal class ExecEvent : IExecEvent
    {
        private static readonly bool _usePool = false;
        private static readonly Queue<ExecEvent> _pool = new Queue<ExecEvent>();

        public static ExecEvent Get(ExecEventReceiver eer, DateTime when, double priority, object userData, ExecEventType eet, long key, bool isDaemon)
        {
            ExecEvent retval;
            if (_pool.Count == 0)
            {
                retval = new ExecEvent(eer, when, priority, userData, eet, key, isDaemon);
            }
            else
            {
                retval = _pool.Dequeue();
                retval.Initialize(eer, when, priority, userData, eet, key, isDaemon);
            }
            return retval;
        }

        private ExecEvent(ExecEventReceiver eer, DateTime when, double priority, object userData, ExecEventType eet, long key, bool isDaemon)
        {
            Initialize(eer, when, priority, userData, eet, key, isDaemon);
        }

        private void Initialize(ExecEventReceiver eer, DateTime when, double priority, object userData, ExecEventType eet, long key, bool isDaemon)
        {
            ExecEventReceiver = eer;
            When = when;
            Priority = priority;
            UserData = userData;
            EventType = eet;
            Key = key;
            IsDaemon = isDaemon;
            Ticks = when.Ticks;
            ServiceCompleted = null;
        }

        public bool IsDaemon
        {
            get;
            private set;
        }

        public long Ticks
        {
            get; 
            private set;
        }

        public override string ToString()
        {
            return "Event: Time= " + When + ", pri= " + Priority + ", type= " + EventType + ", userData= " + UserData;
        }

        #region IExecEvent Members

        public ExecEventReceiver ExecEventReceiver
        {
            get;
            private set;
        }

        public DateTime When
        {
            get;
            private set;
        }

        public double Priority
        {
            get;
            private set;
        }

        public object UserData
        {
            get;
            private set;
        }

        public ExecEventType EventType
        {
            get;
            private set;
        }

        public long Key
        {
            get;
            private set;
        }
        #endregion

        public void OnServiceCompleted()
        {
            if (ServiceCompleted != null)
            {
                ServiceCompleted(Key, ExecEventReceiver, Priority, When, UserData, EventType);
            }
            if (_usePool)
            {
                _pool.Enqueue(this);
            }
        }

        public event EventMonitor ServiceCompleted;

    }
}
