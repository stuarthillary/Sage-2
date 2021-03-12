/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.SimCore;
using System;
using System.Collections;

namespace Highpoint.Sage.Graphs.Tasks
{
    public interface ITaskManagementService : IModelService
    {
        /// <summary>
        /// Fired when a TaskProcessor is added to this model.
        /// </summary>
        event TaskProcessorListener TaskProcessorAddedEvent;

        /// <summary>
        /// Fired when a TaskProcessor is removed from this model.
        /// </summary>
        event TaskProcessorListener TaskProcessorRemovedEvent;

        /// <summary>
        /// Adds a task processor to this model. A Task Processor is an entity that knows when to
        /// start executing a given task graph. This method must be called before the model starts.
        /// </summary>
        /// <param name="taskProcessor">The task processor being added to this model.</param>
        void AddTaskProcessor(TaskProcessor taskProcessor);

        /// <summary>
        /// Removes a task processor from this model. This method must be called before the model starts.
        /// </summary>
        /// <param name="taskProcessor">The task processor being removed from this model.</param>
        void RemoveTaskProcessor(TaskProcessor taskProcessor);

        /// <summary>
        /// The collection of task processors being managed by this model.
        /// </summary>
        ArrayList TaskProcessors
        {
            get;
        }

        /// <summary>
        /// Locates a task processor by its Guid.
        /// </summary>
        /// <param name="guid">The Guid of the task processor to be located.</param>
        /// <returns>The task processor, if found, otherwise null.</returns>
        TaskProcessor GetTaskProcessor(Guid guid);

        /// <summary>
        /// Returns the tasks known to this model.
        /// </summary>
        /// <param name="masterTasksOnly">If this is true, then only the root (master) tasks of all of the
        /// known task graphs are returned. Otherwise, all tasks under those root tasks are included as well.</param>
        /// <returns>A collection of the requested tasks.</returns>
        ICollection GetTasks(bool masterTasksOnly);
    }
}
