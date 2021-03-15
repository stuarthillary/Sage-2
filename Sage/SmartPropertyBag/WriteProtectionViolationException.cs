/* This source code licensed under the GNU Affero General Public License */

using System;
// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// Thrown when someone tries to change a value that is write-locked.
    /// </summary>
    public class WriteProtectionViolationException : Exception
    {

        /// <summary>
        /// Creates a new instance of the <see cref="T:WriteProtectionViolationException"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="msg">The message.</param>
        public WriteProtectionViolationException(object target, string msg) : base(msg)
        {
            Target = target;
        }
        /// <summary>
        /// Creates a new instance of the <see cref="T:WriteProtectionViolationException"/> class.
        /// </summary>
        /// <param name="target">The target - the object that received the attempt to change its value.</param>
        /// <param name="writeLock">The write lock that is watching that target.</param>
        public WriteProtectionViolationException(object target, WriteLock writeLock) :
            base("Attempted write protection violation in " + target + (writeLock.WhereApplied != null ? ". WriteLock applied at : \r\n" + writeLock.WhereApplied : "."))
        {
            Target = target;
        }
        /// <summary>
        /// Creates a new instance of the <see cref="T:WriteProtectionViolationException"/> class.
        /// </summary>
        /// <param name="target">The target - the object that received the attempt to change its value.</param>
        public WriteProtectionViolationException(object target) : base("Attempted write protection violation in " + target)
        {
            Target = target;
        }
        /// <summary>
        /// Gets the target - the object that received the attempt to change its value.
        /// </summary>
        /// <value>The target.</value>
        public object Target
        {
            get;
        }
    }
}