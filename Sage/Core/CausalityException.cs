/* This source code licensed under the GNU Affero General Public License */

using System;
// ReSharper disable RedundantDefaultMemberInitializer

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// A CausalityException is raised if the executive encounters a request to fire an event at a time earlier than the
    /// current time whose events are being served.
    /// </summary>
    /// <seealso cref="System.ApplicationException" />
    public class CausalityException : Exception
    {
        public CausalityException(string message) : base(message) { }
    }
}
