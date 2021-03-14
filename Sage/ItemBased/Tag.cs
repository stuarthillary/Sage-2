/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.ItemBased
{


    /// <summary>
    /// Class Tag is the base class for implementations of Tags.
    /// </summary>
    /// <seealso cref="Highpoint.Sage.ItemBased.ITag" />
    public class Tag : ITag
    {
        private readonly ITagType _tagType;
        private readonly string _value = "";

        /// <summary>
        /// Initializes a new instance of the <see cref="Tag"/> class.
        /// </summary>
        /// <param name="tagType">Type of the tag.</param>
        public Tag(TagType tagType)
        {
            _tagType = tagType;
            if (_tagType.isConstrained)
            {
                _value = _tagType.ValueCandidates[0];
            }
        }
        #region ITag Members

        /// <summary>
        /// Sets the value of this tag. Must follow any constraints specified by the tag type.
        /// </summary>
        /// <param name="newValue">The new value.</param>
        /// <returns><c>true</c> if setting the new value was successful, <c>false</c> otherwise.</returns>
        /// <exception cref="System.Exception">The method or operation is not implemented.</exception>
        public bool SetValue(string newValue)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region IReadOnlyTag Members

        /// <summary>
        /// Gets the type of the tag.
        /// </summary>
        /// <value>The type of the tag.</value>
        public ITagType TagType
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        /// <summary>
        /// Gets the name of the tag.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        /// <summary>
        /// Gets the value of the tag.
        /// </summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">The method or operation is not implemented.</exception>
        public string Value
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        #endregion
    }
}
