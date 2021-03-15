/* This source code licensed under the GNU Affero General Public License */

// ReSharper disable UnusedParameter.Global
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedMethodReturnValue.Global
namespace Highpoint.Sage.Utility
{
    /// <summary>
    /// Describes a participant's role in a handoff through a TupleSpace.
    /// </summary>
    public enum TsOpType
    {
        /// <summary>
        /// The participant will place the token into the common area.
        /// </summary>
        Post,
        /// <summary>
        /// The participant will read the token, and leave it in the common area.
        /// </summary>
        Read,
        /// <summary>
        /// The participant will take the token from the common area.
        /// </summary>
        Take
    }


}