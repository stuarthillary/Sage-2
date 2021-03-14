/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.SimCore
{

    /// <summary>
    /// This interface is implemented by an object that serves to indicate that an
    /// error has occurred in the model.
    /// </summary>
    public interface IModelError : INotification
    {
        /// <summary>
        /// An exception that may have been caught in the detection of this error.
        /// </summary>
        Exception InnerException
        {
            get;
        }

        /// <summary>
        /// Gets a value indicating whether this error should be automatically cleared at the start of a simulation.
        /// </summary>
        /// <value><c>true</c> if [auto clear]; otherwise, <c>false</c>.</value>
        bool AutoClear
        {
            get;
        }
    }


}

