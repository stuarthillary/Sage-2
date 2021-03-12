/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.Graphs.Tasks
{
    public class TaskException : Exception
    {
        private Task _task = null;
        public TaskException(Task task, string message) : base(message)
        {
            _task = task;
        }
        public Task Task
        {
            get
            {
                return _task;
            }
        }
    }
}
