/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.Graphs
{
    /// <summary>
    /// Thrown when a UtilityReference is being set, but is already in use. A UtilityReference is a reference that can be used by 
    /// whomever needs to do so, for short periods. The cloning mechanism, for example, uses it during cloning. 
    /// </summary>
    public class UtilityReferenceInUseException : Exception {
		object m_ref;
		public UtilityReferenceInUseException(object reference):base("Utility reference already in use by " + reference){
			m_ref = reference;
		}
		public object Reference { get { return m_ref ;} }
	}
}
