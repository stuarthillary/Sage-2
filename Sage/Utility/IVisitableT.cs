/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// An interface that is implemented by any object that can be visited. It
    /// typically does very little more than call iv.Visit(this);
    /// </summary>
    public interface IVisitable<out T>
    {
        /// <summary>
        /// Requests that the IVisitable allow the visitor to visit.
        /// </summary>
        /// <param name="iv">The visitor.</param>
        void Accept(IVisitor<T> iv);
    }

}