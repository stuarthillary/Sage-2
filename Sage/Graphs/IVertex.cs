/* This source code licensed under the GNU Affero General Public License */


using Highpoint.Sage.Graphs.Validity;
using Highpoint.Sage.Persistence;
using Highpoint.Sage.SimCore; // For executive.
using System.Collections;

namespace Highpoint.Sage.Graphs
{
    public interface IVertex : IVisitable, IXmlPersistable, IHasName, IHasValidity
    {
        Vertex.WhichVertex Role
        {
            get;
        }
        Edge PrincipalEdge
        {
            get;
        }
        IList PredecessorEdges
        {
            get;
        }
        IList SuccessorEdges
        {
            get;
        }
        IEdgeFiringManager EdgeFiringManager
        {
            get;
        }
        IEdgeReceiptManager EdgeReceiptManager
        {
            get;
        }
        void PreEdgeSatisfied(IDictionary graphContext, Edge theEdge);
        TriggerDelegate FireVertex
        {
            get;
        }
    }

}
