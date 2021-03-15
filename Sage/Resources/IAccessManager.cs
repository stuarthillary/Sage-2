/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Resources
{
    /// <summary>
    /// An object that manages multiple access regulators. They are managed in stacks,
    /// with one stack for each specific resource under management, and one stack for
    /// any requests for resources that do not have specific regulators assigned to them.
    /// </summary>
    public interface IAccessManager : IAccessRegulator
    {
        /// <summary>
        /// Pushes an access regulator onto the stack that is associated with a particular resource, or
        /// the default stack, if no resource is specified.
        /// </summary>
        /// <param name="accReg">Access Regulator to be pushed.</param>
        /// <param name="subject">The resource to which this regulator is to apply, or null, if it applies to all of them.</param>
        void PushAccessRegulator(IAccessRegulator accReg, IResource subject);
        /// <summary>
        /// Pops the top access regulator from the stack associated with the specified resource, or from the
        /// default stack if subject is set as null.
        /// </summary>
        /// <param name="subject">The resource to be regulated, or null if all are to be regulated.</param>
        /// <returns>The AccessRegulator being popped, or null, if the stack was empty.</returns>
        IAccessRegulator PopAccessRegulator(IResource subject);
    }
}
