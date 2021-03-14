/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// An error that is registered as a result of an exception having been thrown, unhandled, by the model.
    /// </summary>
    public class ModelExceptionError : GenericModelError
    {
        private Exception _exception;
        /// <summary>
        /// Creates a ModelExceptionError around a thrown exception.
        /// </summary>
        /// <param name="ex">The exception that caused this error.</param>
        public ModelExceptionError(Exception ex) : base("Model Exception Error", ex.Message, null, null)
        {
            _exception = ex;
        }

        /// <summary>
        /// The exception that caused this error.
        /// </summary>
        public Exception BaseException
        {
            get
            {
                return _exception;
            }
        }

    }


}

