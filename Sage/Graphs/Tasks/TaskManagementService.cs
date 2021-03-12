/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.SimCore;
using System;
using System.Collections;
using System.Diagnostics;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Graphs.Tasks
{
    public class TaskManagementService : ITaskManagementService
    {
        private static readonly bool _diagnostics = Diagnostics.DiagnosticAids.Diagnostics("TaskManagementService");
        private static readonly bool _managePostMortemData = Diagnostics.DiagnosticAids.Diagnostics("Graph.KeepPostMortems");

        private readonly Hashtable _taskProcessors;
        private IModel _model;
        public TaskManagementService()
        {
            _taskProcessors = new Hashtable();
        }


        #region >>> TaskProcessor Management <<<

        public void OnModelStarting(IModel model)
        {
            // A part of the protocol for this generic model is that 
            // all TaskProcessors run when the model runs.
            foreach (TaskProcessor tp in TaskProcessors)
                tp.Activate();
        }

        /// <summary>
        /// Fired when a TaskProcessor is added to this model.
        /// </summary>
        public event TaskProcessorListener TaskProcessorAddedEvent;
        /// <summary>
        /// Fired when a TaskProcessor is removed from this model.
        /// </summary>
        public event TaskProcessorListener TaskProcessorRemovedEvent;

        /// <summary>
        /// Adds a task processor to this model. A Task Processor is an entity that knows when to
        /// start executing a given task graph. This method must be called before the model starts.
        /// </summary>
        /// <param name="taskProcessor">The task processor being added to this model.</param>
        public void AddTaskProcessor(TaskProcessor taskProcessor)
        {
            // TODO: Add this to an Errors & Warnings collection instead of dumping it to Trace.
            if (_taskProcessors.Contains(taskProcessor.Guid))
            {
                _Debug.WriteLine("Model already contains task processor being added at:");
                _Debug.WriteLine((new StackTrace()).ToString());
                _Debug.WriteLine("...the request to add it will be ignored.");
                return;
            }
            _taskProcessors.Add(taskProcessor.Guid, taskProcessor);
            TaskProcessorAddedEvent?.Invoke(this, taskProcessor);
        }

        /// <summary>
        /// Removes a task processor from this model. This method must be called before the model starts.
        /// </summary>
        /// <param name="taskProcessor">The task processor being removed from this model.</param>
        public void RemoveTaskProcessor(TaskProcessor taskProcessor)
        {
            _taskProcessors.Remove(taskProcessor.Guid);
            TaskProcessorRemovedEvent?.Invoke(this, taskProcessor);
        }

        /// <summary>
        /// The collection of task processors being managed by this model.
        /// </summary>
        public ArrayList TaskProcessors
        {
            get
            {
                ArrayList tps = new ArrayList(_taskProcessors.Values);
                return ArrayList.ReadOnly(tps);
            }
        }

        /// <summary>
        /// Locates a task processor by its Guid.
        /// </summary>
        /// <param name="guid">The Guid of the task processor to be located.</param>
        /// <returns>The task processor, if found, otherwise null.</returns>
        public TaskProcessor GetTaskProcessor(Guid guid)
        {
            return (TaskProcessor)_taskProcessors[guid];
        }

        /// <summary>
        /// Returns the tasks known to this model.
        /// </summary>
        /// <param name="masterTasksOnly">If this is true, then only the root (master) tasks of all of the
        /// known task graphs are returned. Otherwise, all tasks under those root tasks are included as well.</param>
        /// <returns>A collection of the requested tasks.</returns>
        public virtual ICollection GetTasks(bool masterTasksOnly)
        {
            ArrayList kids = new ArrayList();
            foreach (TaskProcessor tp in TaskProcessors)
            {
                kids.Add(tp.MasterTask);
                if (!masterTasksOnly)
                    kids.AddRange(tp.MasterTask.GetChildTasks(false));
            }
            return kids;
        }

        /// <summary>
        /// Clears any errors whose target (the place where the error occurred) is a task that has been 
        /// removed from the model.
        /// </summary>
        public void ClearOrphanedErrors()
        {

            ArrayList allTasks = new ArrayList();

            foreach (TaskProcessor tp in _taskProcessors.Values)
            {
                if (allTasks.Contains(tp.MasterTask))
                    continue;
                allTasks.AddRange(tp.MasterTask.GetChildTasks(false));
            }

            if (_diagnostics)
            {
                _Debug.WriteLine("Clearing orphaned errors:\r\nKnown Tasks:");
                foreach (Task task in allTasks)
                {
                    _Debug.WriteLine("\t" + task.Name);
                }
            }

            ArrayList keysToClear = new ArrayList();
            foreach (IModelError err in _model.Errors)
            {
                if (err.Target is Task)
                {
                    if (_diagnostics)
                        _Debug.WriteLine("Checking error " + err.Narrative + ", targeted to " + ((Task)err.Target).Name);
                    if (!allTasks.Contains(err.Target))
                    {
                        if (_diagnostics)
                            _Debug.WriteLine("Clearing error " + err.Name);
                        keysToClear.Add(err.Target);
                    }
                }
            }

            foreach (object key in keysToClear)
                _model.ClearAllErrorsFor(key);
        }

        /// <summary>
        /// Returns the post mortem data on all known task graph executions. This data indicates the vertices
        /// and edges that fired and those that did not fire. It is typically fed into the Diagnostics class'
        /// DumpPostMortemData(...) API.
        /// </summary>
        /// <returns>A Hashtable of postmortem data.</returns>
        public Hashtable GetPostMortems()
        {
            Hashtable postmortems = new Hashtable();
#if DEBUG
            if (_managePostMortemData)
            {
                foreach (TaskProcessor tp in TaskProcessors)
                {
                    foreach (IDictionary graphContext in tp.GraphContexts)
                    {
                        PmData pmData = (PmData)graphContext["PostMortemData"];
                        postmortems.Add(tp.Name, pmData);
                    }
                }
            }
#endif //DEBUG
            return postmortems;
        }

        #endregion


        public void InitializeService(IModel model)
        {
            _model = model;
            _model.Starting += OnModelStarting;
        }

        public bool IsInitialized
        {
            get
            {
                return _model != null;
            }
            set
            {
            }
        }
        public bool InlineInitialization => true;
    }
}
