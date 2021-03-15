/* This source code licensed under the GNU Affero General Public License */
using System;
// ReSharper disable UnusedMember.Local
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable VirtualMemberNeverOverriden.Global

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// A general exception that is fired by a SPB for reasons specific to the SPB.
    /// The message will provide an explanation of the error.
    /// </summary>
    public class SmartPropertyBagException : Exception
    {
        /// <summary>
        /// Creates a SmartPropertyBagException with a given message.
        /// </summary>
        /// <param name="msg">The message that will be associate with this SmartPropertyBagException</param>
		public SmartPropertyBagException(string msg) : base(msg) { }
    }
}