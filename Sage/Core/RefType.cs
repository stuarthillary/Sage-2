/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// An enum that is used to describe the nature of a dependency relationship. It always refers to one
    /// object in context of another, such as in an InitializerArgAttribute, where it describes the 
    /// relationship of the object being referred to in the argument to the object that owns the Initialize
    /// call. The documentation below will refer to the object that owns the initializer as the 'owner', and
    /// the object being referred to in the argument as the 'subject'.
    /// </summary>
    [System.Reflection.Obfuscation(Feature = "renaming", Exclude = true)]
    public enum RefType
    {
        /// <summary>
        /// The subject is fully owned by the owner - no other object may reference it during initialization.
        /// </summary>
        Private,
        /// <summary>
        /// The subject is owned by the owner, but may be referenced by others.
        /// </summary>
        Owned,
        /// <summary>
        /// This subject must appear as an argument that is denoted as a Master on some other owner.
        /// </summary>
        Watched,
        /// <summary>
        /// This subject may be declared as a shared subject by more than one object.
        /// </summary>
        Shared
    }

}
