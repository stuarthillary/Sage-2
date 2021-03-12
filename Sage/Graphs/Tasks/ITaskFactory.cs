/* This source code licensed under the GNU Affero General Public License */

using System.Collections;
using Highpoint.Sage.SimCore;

namespace Highpoint.Sage.Graphs.Tasks
{
    /// <summary>
    /// Implemented by an object that can create Task objects.
    /// </summary>
    public interface ITaskFactory {
		/// <summary>
		/// Creates a task object that is a part of the provided model.
		/// </summary>
		/// <param name="model">The model in which this task will run.</param>
		/// <param name="creationContext">The creation context (collection of serialization-related data) that this serialization stream uses.</param>
		/// <returns>The task.</returns>
		Task CreateTask(IModel model, Hashtable creationContext);
	}
}
