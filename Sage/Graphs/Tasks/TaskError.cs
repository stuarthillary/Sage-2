/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using System;

namespace Highpoint.Sage.Graphs.Tasks
{
    public class TaskError : IModelError
    {
        private Task _task;
        private string _name;
        private string _narrative;
        private object _subject = null;
        private double _priority = 0.0;
        private bool _autoClear = false;

        public Task Task
        {
            get
            {
                return _task;
            }
            protected set
            {
                _task = value;
            }
        }
        protected TaskError()
        {
        }
        public TaskError(Task theTask)
        {
            _task = theTask;
            _name = "Task Error";
            _narrative = "Task error in task " + _task.Name;
            _subject = null;
        }

        #region Implementation of IModelError
        public string Name
        {
            get
            {
                return _name ?? "";
            }
            protected set
            {
                _name = value;
            }
        }
        public string Narrative
        {
            get
            {
                return _narrative ?? "";
            }
            protected set
            {
                _narrative = value;
            }
        }
        public object Target
        {
            get
            {
                return _task;
            }
        }
        public object Subject
        {
            get
            {
                return _subject;
            }
            protected set
            {
                _subject = value;
            }
        }
        public double Priority
        {
            get
            {
                return _priority;
            }
            set
            {
                _priority = value;
            }
        }
        /// <summary>
        /// An exception that may have been caught in the detection of this error.
        /// </summary>
        public Exception InnerException
        {
            get
            {
                return null;
            }
        }

        public bool AutoClear
        {
            get
            {
                return _autoClear;
            }
        }
        #endregion

        public override string ToString()
        {
            return Name + " occurred at " + _task.Name + " due to " + Subject + " : " + Narrative;
        }
    }
}
