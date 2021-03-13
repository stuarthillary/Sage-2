/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// Implemented by any object that is likely to be tracked by the core, or
    /// perhaps a user, framework.
    /// </summary>
    public interface IHasIdentity : IHasName
    {

        /// <summary>
        /// A description of this object.
        /// </summary>
        string Description
        {
            get;
        }

        /// <summary>
        /// The Guid for this object. Typically required to be unique.
        /// </summary>
        Guid Guid
        {
            get;
        }
    }
}
