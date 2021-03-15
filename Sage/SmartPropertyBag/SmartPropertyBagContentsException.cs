/* This source code licensed under the GNU Affero General Public License */
// ReSharper disable UnusedMember.Local
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable VirtualMemberNeverOverriden.Global

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// An exception that is fired when the SPB is asked to add/remove/retrieve a
    /// key that is inappropriate for some reason. 	The message will provide an
    /// explanation of the error.
    /// </summary>
    public class SmartPropertyBagContentsException : SmartPropertyBagException
    {
        /// <summary>
        /// Creates a SmartPropertyBagContentsException with a given message.
        /// </summary>
        /// <param name="msg">The message that will be associate with this SmartPropertyBagContentsException</param>
        public SmartPropertyBagContentsException(string msg) : base(msg) { }
    }
}