/* This source code licensed under the GNU Affero General Public License */

using System;
// ReSharper disable RedundantDefaultMemberInitializer

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// MissingParameterException is thrown when a required parameter is missing. Typically used in a late bound, read-from-name/value pair collection scenario.
    /// </summary>
    [Serializable]
    public class RuntimeException : Exception
    {

        #region protected ctors
        /// <summary>
        /// Initializes a new instance of this class with serialized data. 
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown. </param>
        /// <param name="context">The <see cref="System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected RuntimeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        #endregion

        #region public ctors
        /// <summary>
        /// Creates a new instance of the <see cref="T:AnalysisFailedException"/> class.
        /// </summary>
        public RuntimeException()
        {
        }
        /// <summary>
        /// Creates a new instance of the <see cref="T:AnalysisFailedException"/> class with a specific message and an inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The exception inner exception.</param>
        public RuntimeException(string message, Exception innerException) : base(message, innerException) { }
        #endregion
    }
}
