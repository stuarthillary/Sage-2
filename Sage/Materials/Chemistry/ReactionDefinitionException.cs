/* This source code licensed under the GNU Affero General Public License */

using System;
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Highpoint.Sage.Materials.Chemistry
{
    /// <summary>
    /// An exception that is thrown if there is a cycle in a dependency graph that has been analyzed.
    /// </summary>
    [Serializable]
    public class ReactionDefinitionException : Exception
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
        protected ReactionDefinitionException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        #endregion

        private readonly Reaction _reaction = null;
        /// <summary>
        /// Gets the members of the cycle.
        /// </summary>
        /// <value>The members of the cycle.</value>
        public Reaction Reaction
        {
            get
            {
                return _reaction;
            }
        }
        #region public ctors
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public ReactionDefinitionException(Reaction reaction) : base(string.Format("{0} is not valid.", reaction.ToString()))
        {
            _reaction = reaction;
        }

        /// <summary>
        /// Creates a new instance of this class with a specific message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="reaction">The reaction.</param>
        public ReactionDefinitionException(string message, Reaction reaction) : base(message)
        {
            _reaction = reaction;
        }
        #endregion
    }

}
