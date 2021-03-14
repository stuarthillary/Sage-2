/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Resources;
using System.Collections;

namespace Highpoint.Sage.Materials.Chemistry
{

    /// <summary>
    /// A MaterialConduitManager handles replenishment and drawdown from a primary 
    /// resource manager (i.e. pool) to one or more secondary managers in the case 
    /// that the primary resource manager either cannot satisfy a request from, or 
    /// cannot accept a release to, the pool it manages. One might think of it as an 
    /// intermediary between a ready tank and an inventory tank, or a waste tank and
    /// a reclamation plant.
    /// </summary>
    public class MaterialConduitManager
    {

        #region Private Fields
        private readonly IResourceManager _myResourceManager;
        private readonly Hashtable _conduits;
        private Hashtable _resources;
        #endregion


        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialConduitManager"/> class.
        /// </summary>
        /// <param name="mustHaveMaterialsInIt">The must have materials in it.</param>
        public MaterialConduitManager(IResourceManager mustHaveMaterialsInIt)
        {
            _conduits = new Hashtable();
            _myResourceManager = mustHaveMaterialsInIt;
            _myResourceManager.ResourceRequested += myResourceManager_ResourceRequested;
            _myResourceManager.ResourceAdded += IndexResources;
            _myResourceManager.ResourceRemoved += IndexResources;
            IndexResources(_myResourceManager, null);
        }

        /// <summary>
        /// Adds a conduit that allows this resource manager to draw from, and push to, another resource manager
        /// that handles a specified material. The other resource manager is seen as providing a source for the
        /// material if this resource manager cannot fulfill a request, and provides a destination to absorb any
        /// more material than can be absorbed by a material resource item in this resource manager.
        /// </summary>
        /// <param name="secondary">The resource manager that provides over/underflow support.</param>
        /// <param name="mt">The material type for which this conduit applies.</param>
        public void AddConduit(IResourceManager secondary, MaterialType mt)
        {
            _conduits.Add(mt, secondary);
        }

        private void myResourceManager_ResourceRequested(IResourceRequest irr, IResource resource)
        {
            MaterialResourceRequest resourceRequest = irr as MaterialResourceRequest;
            if (resourceRequest != null)
            {
                MaterialResourceRequest mrr = resourceRequest;
                //_Debug.WriteLine("I am taking care of a request for " + mrr.QuantityDesired + " kg of " + mrr.MaterialType.Name);
                IResourceManager rm = (IResourceManager)_conduits[mrr.MaterialType];
                if (rm != null)
                {
                    //_Debug.WriteLine("There is a conduit specified for " + mrr.MaterialType.Name + ", and it is " + rm);
                    HandleRequest(mrr, rm);
                }
                else
                {
                    //_Debug.WriteLine("There is no conduit specified for " + mrr.MaterialType.Name);
                }
            }
            else
            {
                //_Debug.WriteLine("I am skipping handling of a request for " + irr.QuantityDesired + " units of something.");
            }
        }

        private void HandleRequest(MaterialResourceRequest mrr, IResourceManager secondary)
        {
            MaterialResourceItem mri = (MaterialResourceItem)_resources[mrr.MaterialType];
            if (mri == null)
                return;

            if (mrr.QuantityDesired > 0)
            { // Depletion - do we have enough supply?
                if (mrr.QuantityDesired > (mri.Available + mri.PermissibleOverbook))
                    TransferIn(mrr, secondary, (mrr.QuantityDesired - mri.Available));
            }
            else
            { // Augmentation - do we have room?
                double howMuchRoom = (mri.Capacity + mri.PermissibleOverbook) - mri.Available;
                if (howMuchRoom + mrr.QuantityDesired < 0)
                    TransferOut(mrr, secondary, (-mrr.QuantityDesired - howMuchRoom));
            }
        }

        private void TransferIn(MaterialResourceRequest mrr, IResourceManager secondary, double quantity)
        {
            //_Debug.WriteLine("Gotta transfer " + quantity + " kg in.");
            IResourceRequest newMrr = new MaterialResourceRequest(mrr.MaterialType, quantity, MaterialResourceRequest.Direction.Deplete);
            if (secondary.Acquire(newMrr, false))
            {
                //_Debug.WriteLine("Successfully transferred.");
            }
        }

        private void TransferOut(MaterialResourceRequest mrr, IResourceManager secondary, double quantity)
        {
            //_Debug.WriteLine("Gotta transfer " + quantity + " kg out.");
            IResourceRequest newMrr = new MaterialResourceRequest(mrr.MaterialType, quantity, MaterialResourceRequest.Direction.Deplete);
            if (secondary.Acquire(newMrr, false))
            {
                //_Debug.WriteLine("Successfully transferred.");
            }
        }

        private void IndexResources(IResourceManager irm, IResource resource)
        {
            _resources = new Hashtable();
            foreach (IResource rsc in _myResourceManager.Resources)
            {
                if (!(rsc is MaterialResourceItem))
                    continue;
                _resources.Add(((MaterialResourceItem)rsc).MaterialType, rsc);
            }
        }
    }
}