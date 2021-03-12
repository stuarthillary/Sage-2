/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Graphs.Tasks
{
    public class TaskHasInvalidSelfStateError : TaskError
    {
        public TaskHasInvalidSelfStateError(Task task, object subject)
        {
            Task = task;
            Name = "InvalidSelfStateError";
            Narrative = "The task " + task.Name + " is reported to be invalid\r\n\t";
            //m_narrative += Diagnostics.DiagnosticAids.ReportOnTaskValidity(theTask);


            Subject = subject;
        }
    }
}
