/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using System.Collections.Generic;

namespace Highpoint.Sage.Graphs.PFC
{
    /// <summary>
    /// A collection of IPfcTransitionNode objects that can be searched by name or by Guid.
    /// </summary>
    public class PfcTransitionNodeList : List<IPfcTransitionNode>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="T:TransitionCollection"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        public PfcTransitionNodeList(int capacity) : base(capacity) { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:TransitionCollection"/> class.
        /// </summary>
        public PfcTransitionNodeList() : base() { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:TransitionCollection"/> class.
        /// </summary>
        /// <param name="srcCollection">The SRC collection.</param>
        public PfcTransitionNodeList(ICollection srcCollection) : base()
        {
            foreach (object obj in srcCollection)
            {
                Add((IPfcTransitionNode)obj);
            }
        }

        #region IPfcTransitionCollection Members

        /// <summary>
        /// Gets the <see cref="T:IPfcTransitionNode"/> with the specified name.
        /// </summary>
        /// <value></value>
        public IPfcTransitionNode this[string name]
        {
            get
            {
                return Find(delegate (IPfcTransitionNode node)
                {
                    return node.Name.Equals(name);
                });
            }
        }

        /// <summary>
        /// Gets the <see cref="T:IPfcTransitionNode"/> with the specified GUID.
        /// </summary>
        /// <value></value>
        public IPfcTransitionNode this[Guid guid]
        {
            get
            {
                return Find(delegate (IPfcTransitionNode node)
                {
                    return node.Guid.Equals(guid);
                });
            }
        }

        #endregion

    }
}
