/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// A simple class that implements ITransitionFailureReason
    /// </summary>
    public class SimpleTransitionFailureReason : ITransitionFailureReason
    {
        readonly string _reason;
        readonly object _object;
        /// <summary>
        /// Creates a SimpleTransitionFailureReason around a reason string and an object that
        /// indicates where the problem arose.
        /// </summary>
        /// <param name="reason">What went wrong.</param>
        /// <param name="Object">Where the problem arose.</param>
        public SimpleTransitionFailureReason(string reason, object Object)
        {
            _reason = reason;
            _object = Object;
        }

        /// <summary>
        /// What went wrong.
        /// </summary>
        public string Reason
        {
            get
            {
                return _reason;
            }
        }
        /// <summary>
        /// Where the problem arose.
        /// </summary>
        public object Object
        {
            get
            {
                return _object;
            }
        }
    }

}
