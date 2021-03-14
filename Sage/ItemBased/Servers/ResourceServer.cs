/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Resources;
using Highpoint.Sage.SimCore;
using System;
using System.Collections;

namespace Highpoint.Sage.ItemBased.Servers
{
    /// <summary>
    /// A resource server is a server that acquires a resource on behalf of an object presented
    /// at its input port, waits a particular duration, releases that resource, and passes the
    /// object to its output port.<p></p>
    /// The ResourceServer is aware of a ResourcePool, and when the ResourceServer is placed in
    /// service, or the ResourcePool fires a Release event, the resource server attampts to pull
    /// and service a new object from its input port.
    /// </summary>
    public class ResourceServer : ServerPlus
    {

        private IResourceRequest[] _requestTemplates;
        private readonly Hashtable _resourcesInUse;
        private readonly bool _useBlockingCalls = false;

        public ResourceServer(IModel model, string name, Guid guid, IPeriodicity periodicity, IResourceRequest[] requestTemplates)
            : base(model, name, guid, periodicity)
        {

            _requestTemplates = requestTemplates;
            if (_requestTemplates == null)
                _requestTemplates = new IResourceRequest[] { };

            _resourcesInUse = new Hashtable();

            foreach (IResourceRequest irr in _requestTemplates)
                if (irr.DefaultResourceManager == null)
                    _useBlockingCalls = true;
            if (!_useBlockingCalls)
            {
                foreach (IResourceRequest irr in _requestTemplates)
                {
                    irr.DefaultResourceManager.ResourceReleased += new ResourceStatusEvent(DefaultResourceManager_ResourceReleased);
                }
            }
            else
            {
                throw new NotSupportedException("Resource Server Templates must specify (and use) default resource manager.");
            }
        }

        protected override bool RequiresAsyncEvents
        {
            get
            {
                return _useBlockingCalls;
            }
        }

        protected override bool CanWeProcessServiceObjectHandler(IServer server, object obj)
        {
            IResourceRequest[] replicates = MultiRequestProcessor.Replicate(ref _requestTemplates);
            bool success = MultiRequestProcessor.ReserveAll(ref replicates, _useBlockingCalls);
            if (success)
                _resourcesInUse.Add(obj, replicates);
            return success;
        }

        protected override void PreCommencementSetupHandler(IServer server, object obj)
        {
            IResourceRequest[] replicates = (IResourceRequest[])_resourcesInUse[obj];
            MultiRequestProcessor.AcquireAll(ref replicates, _useBlockingCalls);
        }

        protected override void PreCompletionTeardownHandler(IServer server, object obj)
        {
            IResourceRequest[] replicates = (IResourceRequest[])_resourcesInUse[obj];
            MultiRequestProcessor.ReleaseAll(ref replicates);
            _resourcesInUse.Remove(obj);
        }

        private void DefaultResourceManager_ResourceReleased(IResourceRequest irr, IResource resource)
        {
            TryToPullServiceObject();
        }
    }
}