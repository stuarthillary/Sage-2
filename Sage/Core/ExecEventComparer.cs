/* This source code licensed under the GNU Affero General Public License */

using System.Collections;
// ReSharper disable RedundantDefaultMemberInitializer

namespace Highpoint.Sage.SimCore
{
    internal class ExecEventComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            ExecEvent ee1 = (ExecEvent)x;
            ExecEvent ee2 = (ExecEvent)y;
            /*if ( EE1.m_ticks < EE2.m_ticks) return -1;
            if ( EE1.m_ticks > EE2.m_ticks ) return  1;
            if ( EE1.m_priority < EE2.m_priority ) return  1;
            if ( EE1.m_key == EE2.m_key ) return 0;
            return -1;*/
            if (ee1.When < ee2.When)
                return -1;
            if (ee1.When > ee2.When)
                return 1;
            if (ee1.Priority < ee2.Priority)
                return 1;
            if (ee1.Key == ee2.Key)
                return 0;
            return -1;
            /*
            if ( ((ExecEvent)x).m_ticks < ((ExecEvent)y).m_ticks ) return -1;
            if ( ((ExecEvent)x).m_ticks > ((ExecEvent)y).m_ticks ) return  1;
            if ( ((ExecEvent)x).m_priority < ((ExecEvent)y).m_priority ) return  1;
            if ( ((ExecEvent)x).m_key == ((ExecEvent)y).m_key ) return 0;
            return -1;
            */
        }
    }
}
