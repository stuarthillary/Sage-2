/* This source code licensed under the GNU Affero General Public License */

// ReSharper disable UnusedParameter.Global
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedMethodReturnValue.Global
namespace Highpoint.Sage.Utility
{

    // TODO: Obsolete the Tuple implementation and get it to use the new .NET Tuple class if possible.
    /// <summary>
    /// Interface ITuple is a key/data pair. It is used only in the TupleSpace implementation
    /// </summary>
    public interface ITuple
    {
        /// <summary>
        /// TODO: All objects in a tuple must be able to be a key. This is a simplified use case where the zeroth element is the only key.
        /// </summary>
        object Key
        {
            get;
        }
        /// <summary>
        /// TODO: All objects in a tuple must be able to be a key. This is a simplified use case where the 1st element is the only data (though it may be a list.)
        /// </summary>
        object Data
        {
            get;
        }
        /// <summary>
        /// Called when a Tuple is posted to a TupleSpace.
        /// </summary>
        /// <param name="ts">The ts.</param>
        void OnPosted(ITupleSpace ts);
        /// <summary>
        /// Called when a Tuple is read from a TupleSpace.
        /// </summary>
        /// <param name="ts">The ts.</param>
        void OnRead(ITupleSpace ts);
        /// <summary>
        /// Called when a Tuple is taken from a TupleSpace.
        /// </summary>
        /// <param name="ts">The ts.</param>
        void OnTaken(ITupleSpace ts);
    }
}