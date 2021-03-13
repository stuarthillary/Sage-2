/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// A marker class that indicates that a given exception was thrown by the executive, rather than
    /// the application code.
    /// </summary>
    public class ExecutiveException : Exception
    {
        /// <summary>
        /// Creates an ExecutiveException.
        /// </summary>
        /// <param name="message">The message to be delivered by the exception.</param>
        public ExecutiveException(string message) : base(message) { }
    }
}
