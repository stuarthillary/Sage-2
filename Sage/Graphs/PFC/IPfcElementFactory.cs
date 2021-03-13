/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Utility;
using System;

namespace Highpoint.Sage.Graphs.PFC
{
    /// <summary>
    /// An implementer of IPfcElementFactory is the factory from which the PfcElements are drawn when an
    /// IProcedureFunctionChart is creating an SfcElement such as a node, link or step.
    /// </summary>
    public interface IPfcElementFactory
    {
        /// <summary>
        /// Creates a step node with the provided characteristics.
        /// </summary>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        /// <param name="description">The description.</param>
        /// <returns>The new IPfcStepNode.</returns>
        IPfcStepNode CreateStepNode(string name, Guid guid, string description);

        /// <summary>
        /// Performs raw instantiation of a new step node.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        /// <param name="description">The description.</param>
        /// <returns></returns>
        IPfcStepNode NewStepNode(IProcedureFunctionChart parent, string name, Guid guid, string description);

        /// <summary>
        /// Creates a transition node with the provided characteristics.
        /// </summary>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        /// <param name="description">The description.</param>
        /// <returns>The new IPfcTransitionNode.</returns>
        IPfcTransitionNode CreateTransitionNode(string name, Guid guid, string description);
        /// <summary>
        /// Performs raw instantiation of a new transition node.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        /// <param name="description">The description.</param>
        /// <returns></returns>
        IPfcTransitionNode NewTransitionNode(IProcedureFunctionChart parent, string name, Guid guid, string description);

        /// <summary>
        /// Creates a link element with the provided characteristics.
        /// </summary>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        /// <param name="description">The description.</param>
        /// <returns>The new IPfcLinkElement.</returns>
        IPfcLinkElement CreateLinkElement(string name, Guid guid, string description);
        /// <summary>
        /// Performs raw instantiation of a new link element.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        /// <param name="description">The description.</param>
        /// <returns></returns>
        IPfcLinkElement NewLinkElement(IProcedureFunctionChart parent, string name, Guid guid, string description);

        /// <summary>
        /// Initializes the specified step node after it has been created.
        /// </summary>
        /// <param name="stepNode">The step node.</param>
        void Initialize(IPfcStepNode stepNode);
        /// <summary>
        /// Initializes the specified transition node after it has been created.
        /// </summary>
        /// <param name="transitionNode">The transition node.</param>
        void Initialize(IPfcTransitionNode transitionNode);
        /// <summary>
        /// Initializes the specified link element after it has been created.
        /// </summary>
        /// <param name="linkElement">The link element.</param>
        void Initialize(IPfcLinkElement linkElement);

        /// <summary>
        /// Called when the loading of a new PFC has been completed.
        /// </summary>
        /// <param name="newPfc">The new PFC.</param>
        void OnPfcLoadCompleted(IProcedureFunctionChart newPfc);

        /// <summary>
        /// Gets the Procedure Function Chart for which this factory is creating elements.
        /// </summary>
        /// <value>The host PFC.</value>
        IProcedureFunctionChart HostPfc
        {
            get; set;
        }

        /// <summary>
        /// Gets the GUID generator in use by this element factory.
        /// </summary>
        /// <value>The GUID generator.</value>
        GuidGenerator GuidGenerator
        {
            get;
        }

        /// <summary>
        /// Returns true if the name of this element conforms to the naming rules that this factory imposes.
        /// </summary>
        /// <param name="element">The element whose name is to be assessed.</param>
        /// <returns><c>true</c> if the name of this element conforms to the naming rules that this factory imposes; otherwise, <c>false</c>.</returns>
        bool IsCanonicallyNamed(IPfcElement element);

        /// <summary>
        /// Causes Step, Transition and Link naming cursors to retract to the sequentially-earliest
        /// name that is not currently assigned in the PFC. That is, if the next transition name to
        /// be assigned was T_044, and the otherwise-highest assigned name was T_025, the transition
        /// naming cursor would retract to T_026. The Step and Link cursors would likewise retract
        /// as a result of this call.
        /// </summary>
        void Retract();
    }

}
