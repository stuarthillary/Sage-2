/* This source code licensed under the GNU Affero General Public License */

using System.Collections;

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// Implemented by an object that is able to handle (and perhaps resolve) an error.
    /// </summary>
    public interface IErrorHandler
    {
        /// <summary>
        /// Called when an individual error occurs, and gives the error handler an opportunity to
        /// resolve the error.
        /// </summary>
        /// <param name="modelError">The error that just occurred.</param>
        /// <returns>true if the error was handled.</returns>
        bool HandleError(IModelError modelError);
        /// <summary>
        /// Called to give the error handler an opportunity to handle all currently-existent errors
        /// in one fell swoop. This is typically called immediately prior to attempting a requested
        /// state transition, and if, after attempting resolution, any errors remain, the requested
        /// transition is made to fail.
        /// </summary>
        /// <param name="modelErrors">An IEnumerable that contains the errors to be handled.</param>
        void HandleErrors(IEnumerable modelErrors);
    }


}

