/* This source code licensed under the GNU Affero General Public License */

using System.Collections;

namespace Highpoint.Sage.Resources
{

    /// <summary>
    /// An access regulator that maintains a list of keys, the presentation
    /// of an object with a .Equals(...) match to any one of which will result
    /// in an allowed acquisition.
    /// </summary>
    public class MultiKeyAccessRegulator : IAccessRegulator
    {

        #region Private Fields

        private readonly ArrayList _keys;
        private readonly object _subject;

        #endregion

        /// <summary>
        /// Creates a new instance of the <see cref="T:MultiKeyAccessRegulator"/> class.
        /// </summary>
        /// <param name="subject">The subject that the caller wiches to acquire.</param>
        /// <param name="keys">The keys that the caller is presenting, in hopes of an acquisition.</param>
        public MultiKeyAccessRegulator(object subject, ArrayList keys)
        {
            _subject = subject;
            _keys = keys;
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
            return ((_subject.Equals(subject) || subject.Equals(_subject)) && _keys.Contains(usingKey));
        }
    }
}
