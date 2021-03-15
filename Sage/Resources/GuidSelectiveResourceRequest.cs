/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.Resources
{
    /// <summary>
    /// A GuidSelectiveResourceRequest requests a specified quantity of a guid-specified
    /// resource from its manager. It assumes the resources to be unique to the given Guid.
    /// </summary>
    /// <seealso cref="Highpoint.Sage.Resources.ResourceRequest" />
    public class GuidSelectiveResourceRequest : ResourceRequest
    {

        private Guid _requiredRscGuid;

        /// <summary>
        /// Initializes a new instance of the <see cref="GuidSelectiveResourceRequest"/> class.
        /// </summary>
        /// <param name="whichResource">Guid which indicates which resource is desired.</param>
        /// <param name="howMuch">How much of the resource is desired.</param>
        /// <param name="key">The key that will be used to see if the resource manager is allowed to
        /// grant a given resource to the requester.</param>
        public GuidSelectiveResourceRequest(Guid whichResource, double howMuch, object key) : base(howMuch)
        {
            Key = key;
            _requiredRscGuid = whichResource;
        }

        /// <summary>
        /// Gets the score that describes the suitability of the resource to fulfill this resource request.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <returns>The score</returns>
        public override double GetScore(IResource resource)
        {
            if (_requiredRscGuid.Equals(resource.Guid))
                return Double.MaxValue;
            return Double.MinValue;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return "GuidSelectiveResourceRequest targeted at Guid = " + _requiredRscGuid;
        }

        /// <summary>
        /// Gets a Guid which indicates which resource is desired.
        /// </summary>
        /// <value>The which resource.</value>
        public Guid WhichResource => _requiredRscGuid;

        /// <summary>
        /// Gets or sets the required resource unique identifier.
        /// </summary>
        /// <value>The required resource unique identifier.</value>
        public Guid RequiredRscGuid
        {
            get
            {
                return _requiredRscGuid;
            }

            set
            {
                _requiredRscGuid = value;
            }
        }

        /// <summary>
        /// Gets the default replicator.
        /// </summary>
        /// <returns>ResourceRequestSource.</returns>
        protected override ResourceRequestSource GetDefaultReplicator()
        {
            return DefaultReplicator;
        }

        private IResourceRequest DefaultReplicator()
        {
            GuidSelectiveResourceRequest irr = new GuidSelectiveResourceRequest(_requiredRscGuid, QuantityDesired, Key)
            {
                DefaultResourceManager = DefaultResourceManager
            };
            return irr;
        }
    }
}
