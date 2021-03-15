/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using System.Globalization;
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Highpoint.Sage.Resources
{
    /// <summary>
    /// A class that implements IModelWarning, and is intended to contain data on a resource request that
    /// was aborted due to deadlock or starvation, at the end of a model run.
    /// The creator of this class must add the instance into the Model's Warnings collection.
    /// </summary>
    public class TerminalResourceRequestAbortedWarning : IModelWarning
    {
        #region Private fields
        private readonly IDetachableEventController _idec;
        private readonly IExecutive _exec;
        #endregion

        /// <summary>
        /// Creates a new instance of the <see cref="T:TerminalResourceRequestAbortedWarning"/> class.
        /// </summary>
        /// <param name="exec">The executive under whose control this warning occurred.</param>
        /// <param name="mgr">The Resource Manager from which the resource was obtained.</param>
        /// <param name="req">The request through which the resource was obtained.</param>
        /// <param name="idec">The <see cref="Highpoint.Sage.SimCore.IDetachableEventController"/> that controls the thread in which the resource was last manipulated.</param>
        public TerminalResourceRequestAbortedWarning(IExecutive exec, IResourceManager mgr, IResourceRequest req, IDetachableEventController idec)
        {
            _idec = idec;
            ResourceRequest = req;
            ResourceManager = mgr;
            _exec = exec;
            Narrative = GetNarrative();
        }

        #region IModelWarning Members

        public string Name => "Resource Request Aborted";

        public string Narrative
        {
            get;
        }

        /// <summary>
        /// Returns the IResourceManager that was unable to satisfy the request.
        /// </summary>
        public object Target => ResourceManager;

        /// <summary>
        /// Returns the IResourceRequest that was unsatisfied.
        /// </summary>
        public object Subject => ResourceRequest;

        /// <summary>
        /// Gets or sets the priority of the notification.
        /// </summary>
        /// <value>The priority.</value>
        public double Priority
        {
            get; set;
        }

        #endregion

        /// <summary>
        /// Returns the IResourceManager that was unable to satisfy the request.
        /// </summary>
        public IResourceManager ResourceManager
        {
            get;
        }

        /// <summary>
        /// Returns the IResourceRequest that was unsatisfied.
        /// </summary>
        public IResourceRequest ResourceRequest
        {
            get;
        }

        /// <summary>
        /// Gets the narrative of this warning.
        /// </summary>
        /// <returns></returns>
        private string GetNarrative()
        {
            string byWhom = ResourceRequest.Requester == null ? "<unknown requester>" : string.Format("{0} [{1}]", ResourceRequest.Requester.Name, ResourceRequest.Requester.Guid);
            string whenRequested = "<unknown>";
            if (_idec != null)
            {
                whenRequested = _idec.RootEvent.When.ToString(CultureInfo.CurrentCulture);
            }
            else
            {
                //System.Diagnostics.Debugger.Break();
            }

            string whenAborted = "<unknown abort time>";
            if (_exec != null)
                whenAborted = _exec.Now.ToString(CultureInfo.CurrentCulture);

            if (_idec?.SuspendedStackTrace != null)
            {
                return string.Format("The simulation ended at time {0}, with no more events. There was a request made at time {1} "
                    + "by {2} of the resource manager {3}, but it was never able to service the request. The call was made from:\r\n",
                    whenAborted, whenRequested, byWhom, ResourceManager.Name);
            }
            else
            {
                return string.Format("The simulation ended at time {0}, with no more events. There was a request made at time {1} "
                    + "by {2} of the resource manager {3}, but it was never able to service the request.",
                    whenAborted, whenRequested, byWhom, ResourceManager.Name);
            }
        }
    }
}
