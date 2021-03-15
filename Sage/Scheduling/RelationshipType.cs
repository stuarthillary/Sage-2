/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Scheduling
{
    /// <summary>
    /// Describes the relationship between two milestones, a dependent and an independent.
    /// </summary>
    public enum RelationshipType
    {
        /// <summary>
        /// 
        /// </summary>
        LTE,
        EQ,
        GTE
        //    , 
        //LTE_O, 
        //EQ_O, 
        //GTE_O 
    };
}


//_Debug.WriteLine("Independent : " + m_independent.Name + " @ " + m_independent.DateTime.ToString());
//_Debug.WriteLine("Dependent   : " + m_dependent.Name   + " @ " +   m_dependent.DateTime.ToString());
//_Debug.WriteLine("Delta       : " + m_delta.ToString());
//_Debug.WriteLine(this.ToString());
