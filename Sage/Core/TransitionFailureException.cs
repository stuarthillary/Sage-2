/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// An exception that is fired if and when a transition fails for a reason
    /// internal to the state machine - currently, this is only in the case of
    /// a request to perform an illegal state transition.
    /// </summary>
    public class TransitionFailureException : Exception
    {

        private readonly IList _reasons;
        private readonly string _message;

        private static string MessageFromReasons(IList reasons)
        {
            string message = "";
            foreach (ITransitionFailureReason itfr in reasons)
            {
                message += itfr.Reason + Environment.NewLine;
            }
            return message;
        }
        private static string MessageFromReason(ITransitionFailureReason reason)
        {
            ArrayList reasons = new ArrayList { reason };
            return MessageFromReasons(reasons);
        }

        /// <summary>
        /// Creates a TransitionFailureException around a list of failure reasons.
        /// </summary>
        /// <param name="reasons">A list of failure reasons.</param>
        public TransitionFailureException(IList reasons) : base(MessageFromReasons(reasons))
        {
            _message = MessageFromReasons(reasons);
        }



        /// <summary>
        /// Creates a TransitionFailureException around a single reason.
        /// </summary>
        /// <param name="reason">The TransitionFailureReason.</param>
        public TransitionFailureException(ITransitionFailureReason reason) : base(MessageFromReason(reason))
        {
            _reasons = new ArrayList();
            _reasons.Add(reason);
            _message = MessageFromReason(reason);
        }

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        /// <value></value>
        /// <returns>The error message that explains the reason for the exception, or an empty string("").</returns>
        public override string Message
        {
            get
            {
                return _message;
            }
        }

        /// <summary>
        /// Gives the caller access to the list (collection) of failure reasons.
        /// </summary>
        public ICollection Reasons
        {
            get
            {
                return _reasons;
            }
        }

        /// <summary>
        /// Provides a human-readable representation of the failure exception,
        /// in the form of a narrative describing the failure reasons.
        /// </summary>
        /// <returns>A narrative describing the failure reasons.</returns>
        public override string ToString()
        {
            int nr = _reasons.Count;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("Failure making model transition request. ");
            if (nr == 1)
            {
                sb.Append("There is 1 reason why:");
            }
            else
            {
                sb.Append("There are " + nr + " reasons why:");
            }

            foreach (ITransitionFailureReason itfr in _reasons)
            {
                sb.Append("\r\n\t");
                sb.Append(itfr.Reason);
            }

            return sb.ToString();
        }
    }

}
