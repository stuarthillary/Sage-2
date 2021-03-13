/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using System.Collections.Generic;

namespace Highpoint.Sage.Graphs.PFC
{

    ///<summary>
    /// A collection of IPfcLinkElement objects that can be searched by name or by Guid.
    ///</summary>
    public class PfcLinkElementList : List<IPfcLinkElement>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="T:LinkCollection"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        public PfcLinkElementList(int capacity) : base(capacity) { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:LinkCollection"/> class.
        /// </summary>
        public PfcLinkElementList() : base() { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:LinkCollection"/> class.
        /// </summary>
        /// <param name="srcCollection">The SRC collection.</param>
        public PfcLinkElementList(ICollection srcCollection)
            : base(srcCollection.Count)
        {
            foreach (object obj in srcCollection)
            {
                Add((IPfcLinkElement)obj);
            }
        }

        #region IPfcLinkCollection Members

        /// <summary>
        /// Gets the <see cref="T:IPfcLinkElement"/> with the specified name.
        /// </summary>
        /// <value></value>
        public IPfcLinkElement this[string name]
        {
            get
            {
                return Find(delegate (IPfcLinkElement node)
                {
                    return node.Name.Equals(name);
                });
            }
        }

        /// <summary>
        /// Gets the <see cref="T:IPfcLinkElement"/> with the specified GUID.
        /// </summary>
        /// <value></value>
        public IPfcLinkElement this[Guid guid]
        {
            get
            {
                return Find(delegate (IPfcLinkElement node)
                {
                    return node.Guid.Equals(guid);
                });
            }
        }

        #endregion

        /// <summary>
        /// Gets the priority comparer, used to sort this list by increasing link priority.
        /// </summary>
        /// <value>The priority comparer.</value>
        public static IComparer<IPfcLinkElement> PriorityComparer
        {
            get
            {
                return new _PriorityComparer();
            }
        }

        private class _PriorityComparer : IComparer<IPfcLinkElement>
        {
            #region IComparer<IPfcLinkElement> Members

            public int Compare(IPfcLinkElement x, IPfcLinkElement y)
            {
                return (x.Priority > y.Priority ? 1 : x.Priority < y.Priority ? -1 : 0);
            }

            #endregion
        }

        internal bool NeedsSorting(IComparer<IPfcLinkElement> iComparer)
        {
            // TODO: Performance improvement to be made here, some day.
            for (int i = 0; i < Count - 1; i++)
            {
                if (iComparer.Compare(this[i], this[i + 1]) != 1)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
