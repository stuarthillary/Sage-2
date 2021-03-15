/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.Resources
{
    /// <summary>
    /// A SimpleResourceRequest requests a specified quantity of whatever is in a
    /// resource manager. It assumes the resources to be homogenenous (i.e. any
    /// offered resource is immediately accepted.)
    /// </summary>
    public class SimpleResourceRequest : ResourceRequest
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleResourceRequest"/> class.
        /// </summary>
        /// <param name="howMuch">The how much.</param>
        public SimpleResourceRequest(double howMuch) : base(howMuch) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleResourceRequest"/> class.
        /// </summary>
        /// <param name="howMuch">The how much.</param>
        /// <param name="fromWhere">From where.</param>
        public SimpleResourceRequest(double howMuch, IResourceManager fromWhere) : this(howMuch)
        {
            DefaultResourceManager = fromWhere;
        }

        /// <summary>
        /// Gets the score that describes the suitability of the resource to fulfill this resource request.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <returns>The score</returns>
        public override double GetScore(IResource resource)
        {
            return double.MaxValue;
        }

        protected override ResourceRequestSource GetDefaultReplicator()
        {
            return DefaultReplicator;
        }

        private IResourceRequest DefaultReplicator()
        {
            IResourceRequest irr = new SimpleResourceRequest(QuantityDesired);
            irr.DefaultResourceManager = DefaultResourceManager;
            return irr;
        }

    }
}
