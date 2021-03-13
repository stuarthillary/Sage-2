/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using System.Collections.Generic;

namespace Highpoint.Sage.Graphs.PFC
{
    /// <summary>
    /// A collection of IPfcNode objects that can be searched by name or by Guid.
    /// </summary>
    public class PfcStepNodeList : List<IPfcStepNode>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="T:StepCollection"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        public PfcStepNodeList(int capacity) : base(capacity) { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:StepCollection"/> class.
        /// </summary>
        public PfcStepNodeList() : base() { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:StepCollection"/> class.
        /// </summary>
        /// <param name="srcCollection">The SRC collection.</param>
        public PfcStepNodeList(ICollection srcCollection)
            : base(srcCollection.Count)
        {
            foreach (object obj in srcCollection)
            {
                Add((IPfcStepNode)obj);
            }
        }

        #region IPfcStepCollection Members

        /// <summary>
        /// Gets the <see cref="T:IPfcStepNode"/> with the specified name.
        /// </summary>
        /// <value></value>
        public IPfcStepNode this[string name]
        {
            get
            {
                return Find(delegate (IPfcStepNode node)
                {
                    return node.Name.Equals(name);
                });
            }
        }

        /// <summary>
        /// Gets the <see cref="T:IPfcStepNode"/> with the specified GUID.
        /// </summary>
        /// <value></value>
        public IPfcStepNode this[Guid guid]
        {
            get
            {
                return Find(delegate (IPfcStepNode node)
                {
                    return node.Guid.Equals(guid);
                });
            }
        }

        #endregion
    }
}
