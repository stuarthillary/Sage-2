/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Utility
{
    /// <summary>
    /// Implemented by an object that is capable of managing context-specific labels.
    /// </summary>
    public interface IHasLabel
    {

        /// <summary>
        /// Gets or sets the label in the currently-selected context, or if none has been selected, then according to the default context.
        /// </summary>
        /// <value>The label.</value>
        string Label
        {
            get; set;
        }

        /// <summary>
        /// Sets the label in the context indicated by the provided context, or if null or String.Empty has been selected, then in the default context.
        /// </summary>
        /// <param name="label">The label.</param>
        /// <param name="context">The context - use null or string.Empty for the default context.</param>
        void SetLabel(string label, string context);

        /// <summary>
        /// Gets the label from the context indicated by the provided context, or if null or String.Empty has been selected, then from the default context.
        /// </summary>
        /// <param name="context">The context - use null or string.Empty for the default context.</param>
        string GetLabel(string context);
    }
}