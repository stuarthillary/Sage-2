/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using System.Collections.Generic;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Graphs.PFC
{
    /// <summary>
    /// A List of IPfcNode objects that can be searched by name or by Guid.
    /// </summary>
    public class PfcNodeList : List<IPfcNode>
    {

        private static PfcNodeList _emptyList = new PfcNodeList();

        #region Constructors
        /// <summary>
        /// Creates a new instance of the <see cref="T:PFCNodeList"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        public PfcNodeList(int capacity) : base(capacity) { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:PFCNodeList"/> class.
        /// </summary>
        public PfcNodeList() : base() { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:PFCNodeList"/> class as a copy of an existing collection.
        /// </summary>
        /// <param name="srcCollection">The source collection.</param>
        public PfcNodeList(ICollection srcCollection)
            : base(srcCollection.Count)
        {
            foreach (object obj in srcCollection)
            {
                Add((IPfcNode)obj);
            }
        }

        #endregion Constructors

        #region PFCNodeList Members

        /// <summary>
        /// Returns all nodes in this collection that are of the specified type.
        /// </summary>
        /// <param name="elementType">Type of the element.</param>
        /// <returns>
        /// A collection of all nodes in this collection that are of the specified type.
        /// </returns>
        public PfcNodeList GetBy(PfcElementType elementType)
        {
            if (elementType.Equals(PfcElementType.Link))
            {
                return new PfcNodeList();
            }
            return (PfcNodeList)FindAll(delegate (IPfcNode node)
            {
                return node.ElementType.Equals(elementType);
            });

        }

        /// <summary>
        /// Gets the <see cref="T:IPfcNode"/> with the specified name.
        /// </summary>
        /// <value></value>
        public IPfcNode this[string name]
        {
            get
            {
                return Find(delegate (IPfcNode node)
                {
                    return node.Name.Equals(name);
                });
            }
        }

        /// <summary>
        /// Gets the <see cref="T:IPfcNode"/> with the specified GUID.
        /// </summary>
        /// <value></value>
        public IPfcNode this[Guid guid]
        {
            get
            {
                return Find(delegate (IPfcNode node)
                {
                    return node.Guid.Equals(guid);
                });
            }
        }

        #endregion

        /// <summary>
        /// Gets an empty list of PfcNodes.
        /// </summary>
        /// <value>The empty list.</value>
        public static PfcNodeList EmptyList
        {
            get
            {
                _Debug.Assert(_emptyList.Count == 0);
                return _emptyList;
            }
        }
    }
}
