/* This source code licensed under the GNU Affero General Public License */
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased
{
    /// <summary>
    /// This is a holder class for access to IComparers that can be used to sort tags and TagHolders in their lists.
    /// </summary>
    internal static class TagComparers
    {
        /// <summary>
        /// Returns an IComparer that compares objects that implement IHasTags, where the comparison is done against
        /// a specifically-named tag.
        /// </summary>
        /// <param name="tagName">Name of the tag.</param>
        /// <returns></returns>
        public static IComparer<ITagHolder> TagsByValue(string tagName)
        {
            return new _TagsByValue(tagName);
        }

        class _TagsByValue : IComparer<ITagHolder>
        {
            private string _tagName = null;
            public _TagsByValue(string tagName)
            {
                _tagName = tagName;
            }
            #region IComparer<IHasTags> Members

            public int Compare(ITagHolder x, ITagHolder y)
            {
                string s1 = x.Tags[_tagName].Value;
                string s2 = y.Tags[_tagName].Value;
                if (s1 == null && s2 == null)
                    return 0;
                if (s1 == null)
                    return 1;
                if (s2 == null)
                {
                    return -1;
                }
                return s1.CompareTo(s2);
            }

            #endregion
        }
    }
}
