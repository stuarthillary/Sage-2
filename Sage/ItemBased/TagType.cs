/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Highpoint.Sage.ItemBased
{
    /// <summary>
    /// A TagType contains metadata governing the use of Tags for a particular purpose. Tags can be
    /// constrained or not, extensible or not, and have a list of candidate values or not. 
    /// <b></b>Example 1: A tag named "LotID" would be unconstrained, and therefore, extensible. That
    /// is to say that the tag may hold any (string) value and therefore, any new value is acceptable.
    /// <b></b>Example 2: A tag might be of type "Rework", and be constrained to values "Yes" or "No",
    /// and not be extensible, i.e. with no provision for being able to add any other options.
    /// <b></b>Example 3: A tag might be of type "Flavor", and be constrained to "Chocolate", "Vanilla"
    /// and "Strawberry", but be extensible so that during execution, some dispatcher (or whatever) can
    /// add "Tutti-Frutti" to the list of acceptable values.
    /// <b></b>A TagType is used to create tags or its type.
    /// </summary>
    public class TagType : ITagType
    {

        #region Private Fields
        private readonly string _typeName;
        private readonly List<string> _values;
        private readonly bool _extensible;
        private readonly bool _constrained;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="TagType"/> class.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        public TagType(string typeName)
        {
            _typeName = typeName;
            _values = new List<string>();
            _extensible = true;
            _constrained = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TagType"/> class.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="extensible">if set to <c>true</c> [extensible].</param>
        /// <param name="values">The values.</param>
        public TagType(string typeName, bool extensible, params string[] values)
        {
            _values = new List<string>(values);
            _constrained = true;
            _extensible = extensible;
        }

        #region ITagType Members

        /// <summary>
        /// Gets the name of the tag type.
        /// </summary>
        /// <value>The name of the tag type.</value>
        public string TypeName
        {
            get
            {
                return _typeName;
            }
        }

        /// <summary>
        /// Gets the value candidates list for this tag type. If the tag type is unconstrained, it returns null.
        /// </summary>
        /// <value>The value candidates.</value>
        public ReadOnlyCollection<string> ValueCandidates
        {
            get
            {
                if (_constrained)
                {
                    return _values.AsReadOnly();
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is constrained to a specific set of candidate values.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is constrained; otherwise, <c>false</c>.
        /// </value>
        public bool isConstrained
        {
            get
            {
                return _constrained;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is extensible. An unconstrained tag type is by definition extensible.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is extensible; otherwise, <c>false</c>.
        /// </value>
        public bool isExtensible
        {
            get
            {
                return (!_constrained) || _extensible;
            }
        }

        /// <summary>
        /// Adds the value to the list of candidate values that tags of this type may take on. This will return false if the
        /// Tag Type is either not extensible, or not constrained.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <returns></returns>
        public bool AddValueCandidate(string value)
        {
            if (!_constrained)
                return false;

            if (!_extensible)
                return false;

            _values.Add(value);
            return true;
        }

        /// <summary>
        /// Creates a new tag of this type, with the specified initial value.
        /// </summary>
        /// <param name="initialValue">The initial value.</param>
        /// <returns>A new Tag.</returns>
        public ITag CreateTag(string initialValue)
        {
            if (isConstrained)
            {
                if (!ValueCandidates.Contains(initialValue))
                {
                    if (isExtensible)
                    {
                        AddValueCandidate(initialValue);
                    }
                    else
                    {
                        string errMsg = string.Format("Attempting to create an instance of Tag Type {0} with initial value {1}, but it can only contain values of {2}, and is not extensible.",
                            TypeName, initialValue, Utility.StringOperations.ToCommasAndAndedList(_values));
                        throw new ApplicationException(errMsg);
                    }
                }
            }

            Tag retval = new Tag(this);
            retval.SetValue(initialValue);
            return retval;
        }

        #endregion
    }
}
