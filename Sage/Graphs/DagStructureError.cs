/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using System;
using System.Collections;

namespace Highpoint.Sage.Graphs
{

    /// <summary>
    /// This is a class that characterizes in an instance of IModelError, a structural error in a DAG.
    /// </summary>
    public class DagStructureError : IModelError
    {

        #region Private Fields
        private string _issue;
        private object _subject;
        private IList _target;
        private double _priority = 0.0;
        #endregion

        /// <summary>
        /// Creates a DAGStructureError.
        /// </summary>
        /// <param name="graphRoot">The root task of the DAG that the error pertains to.</param>
        /// <param name="target">An IList of the entities that comprise the loop.</param>
        /// <param name="narrative">A narrative that describes the error.</param>
        public DagStructureError(IEdge graphRoot, IList target, string narrative)
        {
            _issue = narrative;
            _subject = graphRoot;
            _target = target;
        }

        #region IModelError Members

        /// <summary>
        /// The name of the error.
        /// </summary>
        public string Name
        {
            get
            {
                return "Graph Structure Error";
            }
        }

        /// <summary>
        /// A narrative that describes the error.
        /// </summary>
        public string Narrative
        {
            get
            {
                return _issue;
            }
        }

        /// <summary>
        /// An IList that includes all of the participants in the loop.
        /// </summary>
        public object Target
        {
            get
            {
                return _target;
            }
        }

        /// <summary>
        /// The root task of the DAG that the error pertains to.
        /// </summary>
        public object Subject
        {
            get
            {
                return _subject;
            }
        }

        /// <summary>
        /// Gets or sets the priority of the notification.
        /// </summary>
        /// <value>The priority.</value>
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
        /// Represents an exception that may have been thrown in the creation of this error.
        /// </summary>
        public Exception InnerException
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this error should be automatically cleared at the start of a simulation.
        /// </summary>
        /// <value><c>true</c> if [auto clear]; otherwise, <c>false</c>.</value>
        public bool AutoClear
        {
            get
            {
                return false;
            }
        }

        #endregion

        /// <summary>
        /// A friendly representation of the error, including its narrative.
        /// </summary>
        /// <returns>A friendly representation of the error, including its narrative.</returns>
        public override string ToString()
        {
            return Name + " in graph \"" + Subject + "\" : " + Narrative + ".";
        }

    }
}
