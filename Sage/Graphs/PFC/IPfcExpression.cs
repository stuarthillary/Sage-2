/* This source code licensed under the GNU Affero General Public License */

using System.Collections;

namespace Highpoint.Sage.Graphs.PFC
{
    /// <summary>
    /// An interface implemented by anything in an SFC that is to be evaluated as an expression.
    /// </summary>
    /// <typeparam name="T">The return type of the expression.</typeparam>
    public interface IPfcExpression<T>
    {
        T Evaluate();
        T Evaluate(IDictionary parameters);
    }
}
