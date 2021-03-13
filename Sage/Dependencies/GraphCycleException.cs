/* This source code licensed under the GNU Affero General Public License */
/*###############################################################################
#  Material previously published at http://builder.com/5100-6387_14-5025380.html
#  Highpoint Software Systems is a Wisconsin Limited Liability Corporation.
###############################################################################*/

using System;
using System.Collections;

namespace Highpoint.Sage.Dependencies
{
    /// <summary>
    /// An exception that is thrown if there is a cycle in a dependency graph that has been analyzed.
    /// </summary>
    [Serializable]
    public class GraphCycleException : Exception
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
        protected GraphCycleException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        #endregion

        private IList _members = null;
        /// <summary>
        /// Gets the members of the cycle.
        /// </summary>
        /// <value>The members of the cycle.</value>
        public IList Members
        {
            get
            {
                return _members;
            }
        }
        #region public ctors
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public GraphCycleException(IList members)
        {
            _members = members;
        }

        /// <summary>
        /// Creates a new instance of this class with a specific message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="members">The members of the cycle.</param>
        public GraphCycleException(string message, IList members) : base(message) { _members = members; }

        /// <summary>
        /// Creates a new instance of this class with a specific message and an inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The exception inner exception.</param>
        /// <param name="members">The members of the cycle.</param>
        public GraphCycleException(string message, Exception innerException, IList members) : base(message, innerException) { _members = members; }
        #endregion
    }

}