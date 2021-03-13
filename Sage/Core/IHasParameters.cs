/* This source code licensed under the GNU Affero General Public License */

using System.Collections;

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// Implemented by any object that has a dictionary of parameters.
    /// </summary>
    public interface IHasParameters
    {
        /// <summary>
        /// Gets the parameters dictionary.
        /// </summary>
        /// <value>The parameters.</value>
		IDictionary Parameters
        {
            get;
        }
    }
}
