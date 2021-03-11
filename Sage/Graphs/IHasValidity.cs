/* This source code licensed under the GNU Affero General Public License */

using System.Collections;

namespace Highpoint.Sage.Graphs.Validity
{
    /// <summary>
    /// Implemented by any object that has a state of validity that is managed by a ValidationService.
    /// </summary>
    public interface IHasValidity : IPartOfGraphStructure
    {
        /// <summary>
        /// Gets or sets the validation service that oversees the implementer.
        /// </summary>
        /// <value>The validation service.</value>
		ValidationService ValidationService
        {
            get; set;
        }
        /// <summary>
        /// Gets the children (from a perspective of validity) of the implementer.
        /// </summary>
        /// <returns></returns>
		IList GetChildren();
        /// <summary>
        /// Gets the successors (from a perspective of validity) of the implementer.
        /// </summary>
        /// <returns></returns>
		IList GetSuccessors();
        /// <summary>
        /// Gets the parent (from a perspective of validity) of the implementer.
        /// </summary>
        /// <returns></returns>
		IHasValidity GetParent();
        /// <summary>
        /// Gets or sets the state (from a perspective of validity) of the implementer.
        /// </summary>
        /// <value>The state of the self.</value>
		Validity SelfState
        {
            get; set;
        }
        /// <summary>
        /// Fires when the implementer's validity state is changed.
        /// </summary>
		event ValidityChangeHandler ValidityChangeEvent;
        /// <summary>
        /// Called by the ValidationService upon an overall validity change.
        /// </summary>
        /// <param name="newValidity">The new validity.</param>
		void NotifyOverallValidityChange(Validity newValidity);
    }
}
