/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Text;

namespace Highpoint.Sage.Graphs.PFC.Expressions
{
    /// <summary>
    /// A Macro that expands into an expression that evaluates true only if all (explicitly
    /// named) predecessors to the owner transition are true. If there are no predecessors,
    /// then the expression is simply &quot;True&quot;.
    /// </summary>
    public class PredecessorsComplete : Macro
    {

        /// <summary>
        /// The name by which this macro is known.
        /// </summary>
        public static readonly string NAME = MACRO_START + "PredecessorsComplete'";
        public static readonly string ALTNAME = "'" + Encoding.Unicode.GetString(new byte[] { 188, 3 }) + "PredecessorsComplete'";

        /// <summary>
        /// Initializes a new instance of the <see cref="T:PredecessorsComplete"/> class.
        /// </summary>
        public PredecessorsComplete()
        {
            _guid = new Guid("8F9CB586-F5A9-410b-90FA-4B12064218F1");
            _name = NAME;
        }

        /// <summary>
        /// Evaluates the macro using the specified arguments.
        /// </summary>
        /// <param name="args">The arguments. This macro requires one argument,
        /// the transition that owns it, and it must be of type <see cref="T:IPfcTransitionNode"/></param>
        /// <returns>
        /// The evaluated representation of the macro.
        /// </returns>
        protected override string Evaluate(object[] args)
        {
            IPfcTransitionNode node = (IPfcTransitionNode)args[0];

            if (node.PredecessorNodes.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("( ");
                node.PredecessorNodes.ForEach(delegate (IPfcNode pred)
                {
                    sb.Append("'" + pred.Name + "/BSTATUS' = '$recipe_state:Complete' AND ");
                });
                string retval = sb.ToString();
                retval = retval.Substring(0, retval.Length - " AND ".Length);

                retval += " )";
                return retval;
            }
            else
            {
                return "TRUE";
            }
        }
    }
}