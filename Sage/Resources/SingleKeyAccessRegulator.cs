/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Resources
{
    /// <summary>
    /// Grants access to the requestor if the subject is null or matches
    /// the requested subject, and the stored key matches the provided key
    /// via the .Equals(...) operator.
    /// </summary>
    public class SingleKeyAccessRegulator : IAccessRegulator
    {

        #region Private Fields

        private readonly object _key;
        private readonly object _subject;

        #endregion

        /// <summary>
        /// Creates a new instance of the <see cref="T:SingleKeyAccessRegulator"/> class.
        /// </summary>
        /// <param name="subject">The subject.</param>
        /// <param name="key">The key.</param>
        public SingleKeyAccessRegulator(object subject, object key)
        {
            _subject = subject;
            _key = key;
        }

        /// <summary>
        /// Returns true if the given subject can be acquired using the presented key.
        /// </summary>
        /// <param name="subject">The resource whose acquisition is being queried.</param>
        /// <param name="usingKey">The key that is to be presented by the prospective acquirer.</param>
        /// <returns>
        /// True if the acquire will be allowed, false if not.
        /// </returns>
		public bool CanAcquire(object subject, object usingKey)
        {
            return ((_subject == null || _subject.Equals(subject) || subject.Equals(_subject)) && _key.Equals(usingKey));
        }
    }
}
