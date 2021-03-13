/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// This class can be used as a key for an object into a Task Graph's graphContext, where the
    /// contents of the key are intended to be cleared out of the GC after each run of the model.
    /// </summary>
    [TaskGraphVolatile]
    public class VolatileKey
    {
        private readonly string _name = "VolatileKey";
        /// <summary>
        /// Creates a VolatileKey for use as a key for an object into a Task Graph's graphContext.
        /// </summary>
        public VolatileKey()
        {
        }
        /// <summary>
        /// Creates a VolatileKey for use as a key for an object into a Task Graph's graphContext.
        /// </summary>
        public VolatileKey(string name)
        {
            _name = name;
        }
        /// <summary>
        /// Returns the name of this key.
        /// </summary>
        /// <returns>The name of this key.</returns>
        public override string ToString()
        {
            return _name;
        }

    }
}
