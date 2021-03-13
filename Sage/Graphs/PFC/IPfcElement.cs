/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using System;

namespace Highpoint.Sage.Graphs.PFC
{
    /// <summary>
    /// Implemented by any entity (Links, Steps and Transitions) that participates
    /// in the structure of a ProcedureFunctionChart.
    /// </summary>
    public interface IPfcElement : IModelObject, IResettable
    {

        /// <summary>
        /// Sets (re-sets) the name of this element.
        /// </summary>
        /// <param name="newName">The new name.</param>
        void SetName(string newName);

        /// <summary>
        /// Gets the type of the element.
        /// </summary>
        /// <value>The type of the element.</value>
        PfcElementType ElementType
        {
            get;
        }

        /// <summary>
        /// The parent ProcedureFunctionChart of this node.
        /// </summary>
        IProcedureFunctionChart Parent
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets some piece of arbitrary user data. This data is (currently) not serialized.
        /// </summary>
        /// <value>The user data.</value>
        object UserData
        {
            get; set;
        }

        /// <summary>
        /// Updates the portion of the structure of the SFC that relates to this element.
        /// This is called after any structural changes in the Sfc, but before the resultant data
        /// are requested externally.
        /// </summary>
        void UpdateStructure();

        /// <summary>
        /// Determines whether this instance is connected to anything upstream or downstream.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this instance is connected; otherwise, <c>false</c>.
        /// </returns>
        bool IsConnected();

        /// <summary>
        /// Gets the SEID, or Source Element ID of this element. If the PFC of which 
        /// this element is a member is cloned, then this SEID will be the Guid of the element
        /// in the source PFC that is semantically/structurally equivalent to this one.
        /// </summary>
        /// <value>The SEID.</value>
        Guid SEID
        {
            get;
        }

    }
}
