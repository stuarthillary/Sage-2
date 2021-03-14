/* This source code licensed under the GNU Affero General Public License */
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
    public interface ITagType
    {
        /// <summary>
        /// Gets the name of the tag type.
        /// </summary>
        /// <value>The name of the tag type.</value>
        string TypeName
        {
            get;
        }
        /// <summary>
        /// Gets the value candidates list for this tag type. If the tag type is unconstrained, it returns null.
        /// </summary>
        /// <value>The value candidates.</value>
        ReadOnlyCollection<string> ValueCandidates
        {
            get;
        }
        /// <summary>
        /// Gets a value indicating whether this instance is constrained to a specific set of candidate values.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is constrained; otherwise, <c>false</c>.
        /// </value>
        bool isConstrained
        {
            get;
        }
        /// <summary>
        /// Gets a value indicating whether this instance is extensible. An unconstrained tag type is by definition extensible.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is extensible; otherwise, <c>false</c>.
        /// </value>
        bool isExtensible
        {
            get;
        }
        /// <summary>
        /// Adds the value to the list of candidate values that tags of this type may take on. This will return false if the
        /// Tag Type is either not extensible, or not constrained.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <returns>True if successful, false if the Tag Type is either not extensible, or not constrained.</returns>
        bool AddValueCandidate(string value);
        /// <summary>
        /// Creates a new tag of this type, with the specified initial value.
        /// </summary>
        /// <param name="initialValue">The initial value.</param>
        /// <returns>A new Tag.</returns>
        ITag CreateTag(string initialValue);
    }
}
