/* This source code licensed under the GNU Affero General Public License */


using System.Collections;

namespace Highpoint.Sage.Graphs
{
    public class SynchronizerSorter : IComparer
    {
        #region IComparer Members

        public int Compare(object x, object y)
        {
            VertexSynchronizer vsx = (VertexSynchronizer)x;
            VertexSynchronizer vsy = (VertexSynchronizer)y;

            System.Text.StringBuilder sbx = new System.Text.StringBuilder();
            System.Text.StringBuilder sby = new System.Text.StringBuilder();

            foreach (Vertex vx in vsx.Members)
                sbx.Append(vx.Name);
            foreach (Vertex vy in vsy.Members)
                sby.Append(vy.Name);

            return Comparer.Default.Compare(sbx.ToString(), sby.ToString());
        }

        #endregion

    }

}
