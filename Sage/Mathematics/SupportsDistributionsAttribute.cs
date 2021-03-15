/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Reflection;

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// An attribute that decorates any class that can have a distribution as a member.
    /// </summary>
    public class SupportsDistributionsAttribute : Attribute
    {

        #region Private Fields

        private readonly Type _distroInterface;
        private readonly string _whereToPutTheDistribution;

        #endregion

        /// <summary>
        /// Creates a new instance of the <see cref="T:SupportsDistributionsAttribute"/> class.
        /// </summary>
        /// <param name="distributionInterface">The distribution interface that is implemented by the member in the decorated class.</param>
        /// <param name="memberNameOfDistribution">The member name  in the decorated class, of the distribution.</param>
        public SupportsDistributionsAttribute(Type distributionInterface, string memberNameOfDistribution)
        {
            _distroInterface = distributionInterface;
            _whereToPutTheDistribution = memberNameOfDistribution;
        }

        /// <summary>
        /// Sets the value of the declared member of the target object (which is an instance of the decorated class) to a provided distribution.
        /// </summary>
        /// <param name="target">The target object.</param>
        /// <param name="distro">The distribution.</param>
        public void SetDistribution(object target, object distro)
        {
            BindingFlags bindingAttr = BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.GetField | BindingFlags.NonPublic;
            //MemberInfo mi = target.GetType().GetMember(m_whereToPutTheDistribution,bindingAttr);
            FieldInfo fi = target.GetType().GetField(_whereToPutTheDistribution, bindingAttr);

            fi?.SetValue(target, distro);
        }
    }

}