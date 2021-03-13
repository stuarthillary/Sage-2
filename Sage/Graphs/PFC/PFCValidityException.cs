/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.Graphs.PFC
{
    /// <summary>
    /// An exception that is thrown if there is a cycle in a dependency graph that has been analyzed.
    /// </summary>
    [Serializable]
    public class PFCValidityException : Exception
    {
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp

        #region protected ctors

        /// <summary>
        /// Initializes a new instance of this class with serialized data. 
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown. </param>
        /// <param name="context">The <see cref="System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected PFCValidityException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }

        #endregion

        private IProcedureFunctionChart _pfc = null;

        /// <summary>
        /// Gets the members of the cycle.
        /// </summary>
        /// <value>The members of the cycle.</value>
        public IProcedureFunctionChart Pfc
        {
            get
            {
                return _pfc;
            }
        }

        #region public ctors

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public PFCValidityException(IProcedureFunctionChart pfc)
        {
            _pfc = pfc;
        }

        /// <summary>
        /// Creates a new instance of this class with a specific message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="pfc">The members of the cycle.</param>
        public PFCValidityException(IProcedureFunctionChart pfc, string message) : base(message)
        {
            _pfc = pfc;
        }

        /// <summary>
        /// Creates a new instance of this class with a specific message and an inner exception.
        /// </summary>
        /// <param name="pfc">The members of the cycle.</param>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public PFCValidityException(IProcedureFunctionChart pfc, string message, Exception innerException)
            : base(message, innerException)
        {
            _pfc = pfc;
        }

        #endregion

    }

}