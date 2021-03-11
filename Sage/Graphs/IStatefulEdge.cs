/* This source code licensed under the GNU Affero General Public License */

using System.Collections;
using Highpoint.Sage.Utility.Mementos;

namespace Highpoint.Sage.Graphs
{
    /// <summary>
    /// Implemented by any edge that modifies the state of its graphContext.
    /// </summary>
    public interface IStatefulEdge {
        /// <summary>
        /// Gets the state of the implementing object immediately prior to execution within the provided context.
        /// </summary>
        /// <param name="graphContext">The graph context.</param>
        /// <returns>The state of the implementing object immediately prior to execution.</returns>
		IMemento GetPreState(IDictionary graphContext);
        /// <summary>
        /// Gets the state of the implementing object immediately following execution within the provided context.
        /// </summary>
        /// <param name="graphContext">The graph context.</param>
        /// <returns>The state of the implementing object immediately following execution.</returns>
		IMemento GetPostState(IDictionary graphContext);
	}
}
