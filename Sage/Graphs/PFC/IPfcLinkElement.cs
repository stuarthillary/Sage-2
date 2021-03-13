/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Graphs.PFC
{
    /// <summary>
    /// Implemented by an object that is an SFC SfcLink.
    /// </summary>
    public interface IPfcLinkElement : IPfcElement
    {

        /// <summary>
        /// Gets the predecessor IPfcNode to this Link node.
        /// </summary>
        /// <value>The predecessor.</value>
        IPfcNode Predecessor
        {
            get;
        }

        /// <summary>
        /// Gets the successor IPfcNode to this Link node.
        /// </summary>
        /// <value>The successor.</value>
        IPfcNode Successor
        {
            get;
        }

        /// <summary>
        /// Gets or sets the priority of this link. The higher the number representing a 
        /// link among its peers, the higher priority it has. The highest-priority link is said
        /// to define the 'primary' path through the graph. Default priority is 0.
        /// </summary>
        /// <value>The priority of the link.</value>
        int? Priority
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this link creates a loopback along one or more paths.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is a loopback; otherwise, <c>false</c>.
        /// </value>
        bool IsLoopback
        {
            get; set;
        }

        /// <summary>
        /// Detaches this link from its predecessor and successor.
        /// </summary>
        void Detach();

        /// <summary>
        /// A PfcLink is a part of one of these types of aggregate links, depending on the type of its predecessor
        /// or successor, and the number of (a) successors its predecessor has, and (b) predecessors its successor has.
        /// </summary>
        AggregateLinkType AggregateLinkType
        {
            get;
        }

    }
}
