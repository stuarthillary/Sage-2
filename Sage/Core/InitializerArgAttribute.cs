/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.SimCore
{

    /// <summary>
    /// An InitializerArgAttribute is an attribute that is attached to an argument to an IModelObject Initialize(...)
    /// method. The method argument list must always begin with the Model, Name, Guid and Description arguments, and
    /// then the specific arguments necessary to initialize the object. These specific arguments are the ones that are
    /// decorated with this attribute type.
    /// </summary>
    public class InitializerArgAttribute : Attribute
    {

        #region Private Members

        private readonly string _argName;
        private readonly string _argDescription;
        private readonly RefType _argRefType;
        private readonly Type _argType;
        private readonly int _upperBound;
        private readonly bool _optional;
        private readonly int _paramIndex;

        #endregion Private Members

        /// <summary>
        /// Decorates an argument in an Initialize method. The argument is not optional, and if the argument is an array, it may have no more than one element. 
        /// </summary>
        /// <param name="paramIndex">0 for the first Initializer argument (not counting model, name, decription, guid), 1 for the second, etc.</param>
        /// <param name="argName">The display name for the argument.</param>
        /// <param name="argDescription">A description of the purpose of the argument.</param>
        /// <param name="argRefType">The RefType (Master, Slave, Shared, Private) of the argument or object represented by the argument.</param>
        /// <param name="argType">The Runtime Type of the argument (particularly relevant when the argument is an IModelObject guid.)</param>
        public InitializerArgAttribute(int paramIndex, string argName, RefType argRefType, Type argType, string argDescription)
        {
            _paramIndex = paramIndex;
            _argName = argName;
            _argDescription = argDescription;
            _argRefType = argRefType;
            _argType = argType;
            _optional = false;
            _upperBound = 1;
        }

        /// <summary>
        /// Decorates an argument in an Initialize method. If the argument is an array, it may have no more than one element. 
        /// </summary>
        /// <param name="paramIndex">0 for the first Initializer argument (not counting model, name, decription, guid), 1 for the second, etc.</param>
        /// <param name="argName">The display name for the argument.</param>
        /// <param name="argDescription">A description of the purpose of the argument.</param>
        /// <param name="argRefType">The RefType (Master, Slave, Shared, Private) of the argument or object represented by the argument.</param>
        /// <param name="argType">The Runtime Type of the argument (particularly relevant when the argument is an IModelObject guid.</param>
        /// <param name="optional">True if this argument is a guid that can be Guid.Empty (which maps to 'null' in the ModelObjectDictionary.)</param>
        public InitializerArgAttribute(int paramIndex, string argName, RefType argRefType, Type argType, bool optional, string argDescription)
        {
            _paramIndex = paramIndex;
            _argName = argName;
            _argDescription = argDescription;
            _argRefType = argRefType;
            _argType = argType;
            _optional = optional;
            _upperBound = 1;
        }

        /// <summary>
        /// Decorates an argument in an Initialize method. 
        /// </summary>
        /// <param name="paramIndex">0 for the first Initializer argument (not counting model, name, decription, guid), 1 for the second, etc.</param>
        /// <param name="argName">The display name for the argument.</param>
        /// <param name="argDescription">A description of the purpose of the argument.</param>
        /// <param name="argRefType">The RefType (Master, Slave, Shared, Private) of the argument or object represented by the argument.</param>
        /// <param name="argType">The Runtime Type of the argument (particularly relevant when the argument is an IModelObject guid.</param>
        /// <param name="optional">True if this argument is a guid that can be Guid.Empty (which maps to 'null' in the ModelObjectDictionary.)</param>
        /// <param name="upperBound">If the argment type is an array, this integer depicts the largest number of elements it can have.</param>
        public InitializerArgAttribute(int paramIndex, string argName, RefType argRefType, Type argType, bool optional, int upperBound, string argDescription)
        {
            _paramIndex = paramIndex;
            _argName = argName;
            _argDescription = argDescription;
            _argRefType = argRefType;
            _argType = argType;
            _optional = optional;
            _upperBound = upperBound;
        }

        /// <summary>
        /// Gets the name of this InitializerArgAttribute.
        /// </summary>
        /// <value>The name.</value>
		public string Name
        {
            get
            {
                return _argName;
            }
        }
        /// <summary>
        /// Gets the description of this InitializerArgAttribute.
        /// </summary>
        /// <value>The description.</value>
		public string Description
        {
            get
            {
                return _argDescription;
            }
        }
        /// <summary>
        /// Gets the <see cref="RefType"/> of this InitializerArgAttribute.
        /// </summary>
        /// <value>The type of the ref.</value>
		public RefType RefType
        {
            get
            {
                return _argRefType;
            }
        }
        /// <summary>
        /// Gets the type of this InitializerArgAttribute.
        /// </summary>
        /// <value>The type.</value>
		public Type Type
        {
            get
            {
                return _argType;
            }
        }
        /// <summary>
        /// Gets a value indicating whether this <see cref="T:InitializerArgAttribute"/> is optional.
        /// </summary>
        /// <value><c>true</c> if optional; otherwise, <c>false</c>.</value>
		public bool Optional
        {
            get
            {
                return _optional;
            }
        }
        /// <summary>
        /// Gets the upper bound of this InitializerArgAttribute.
        /// </summary>
        /// <value>The upper bound.</value>
		public int UpperBound
        {
            get
            {
                return _upperBound;
            }
        }
        /// <summary>
        /// Gets the index of this InitializerArgAttribute.
        /// </summary>
        /// <value>The index of the param.</value>
		public int ParamIndex
        {
            get
            {
                return _paramIndex;
            }
        }
        /// <summary>
        /// Gets the default value of this InitializerArgAttribute.
        /// </summary>
        /// <value>The default value.</value>
		public object DefaultValue
        {
            get
            {
                return null;
            }
        } // TODO: populate this.
    }

    //public class InitializerArgsAttribute:System.Attribute {
    //    private InitializerArgAttribute[] m_args;
    //    public InitializerArgsAttribute(InitializerArgAttribute[] args){
    //        m_args = args;
    //    }
    //}
}
