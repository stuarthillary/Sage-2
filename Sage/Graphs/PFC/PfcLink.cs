/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using System.Collections.Generic;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Graphs.PFC {

    public class PfcLink : PfcElement, IPfcLinkElement  {

        #region Private Members

        private IPfcNode _predecessor = null;
        private IPfcNode _successor = null;
        private bool _isLoopback = false;

        #endregion Private Members

        #region Constructors

        internal PfcLink() : this(null, "", "", Guid.NewGuid()) { }

        internal PfcLink(IProcedureFunctionChart parent, string name, string description, Guid guid ):base(parent,name,description,guid){}

        #endregion Constructors

        #region IPfcLinkElement Members

        /// <summary>
        /// Gets the predecessor IPfcNode to this Link node.
        /// </summary>
        /// <value>The predecessor.</value>
        public IPfcNode Predecessor {
            get { return _predecessor; }
            set {
                if (_predecessor == null || value == null) {
                    _predecessor = value;
                } else {
                    throw new PfcStructureViolationException(string.Format(linkErrorString, value.Name, Name, Name, "predecessor"));
                }
            }
        }

        /// <summary>
        /// Gets the successor IPfcNode to this Link node.
        /// </summary>
        /// <value>The successor.</value>
        public IPfcNode Successor {
            get { return _successor; }
            set {
                if (_successor == null || value == null) {
                    _successor = value;
                } else {
                    throw new PfcStructureViolationException(string.Format(linkErrorString, value.Name, Name, Name, "successor"));
                }
            }
        }

        /// <summary>
        /// Gets or sets the priority of this link. The higher the number representing a
        /// link among its peers, the higher priority it has. The highest-priority link is said
        /// to define the 'primary' path through the graph. Default priority is 0.
        /// </summary>
        /// <value>The priority of the link.</value>
        public int? Priority { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this link creates a loopback along one or more paths.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is a loopback; otherwise, <c>false</c>.
        /// </value>
        public bool IsLoopback { get { return _isLoopback; } set { _isLoopback = value; } }

        /// <summary>
        /// Detaches this link from its predecessor and successor.
        /// </summary>
        public void Detach() {
            Predecessor.Successors.Remove(this);
            _predecessor = null;
            Successor.Predecessors.Remove(this);
            _successor = null;
        }

        #endregion

        #region Implementation of Abstract Subclass "PfcElement"
        /// <summary>
        /// Resets this instance. Performed in a run-time context.
        /// </summary>
        public override void Reset() {
            throw new Exception("This method has not been implemented.");
        }

        /// <summary>
        /// Updates the portion of the structure of the SFC that relates to this element.
        /// This is called after any structural changes in the Sfc, but before the resultant data
        /// are requested externally.
        /// </summary>
        public override void UpdateStructure() {
            Console.WriteLine("PfcLink.UpdateStructure has not been implemented.");
        }

        /// <summary>
        /// Determines whether this instance is connected to anything upstream or downstream.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this instance is connected; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsConnected() {
            return _predecessor != null && _successor != null;
        }

        /// <summary>
        /// A PfcLink is a part of one of these types of aggregate links, depending on the type of its predecessor
        /// or successor, and the number of (a) successors its predecessor has, and (b) predecessors its successor has.
        /// </summary>
        public AggregateLinkType AggregateLinkType {
            get {
                if (Predecessor.SuccessorNodes.Count == 1 && Successor.PredecessorNodes.Count == 1) {
                    return AggregateLinkType.Simple;
                } else {
                    if (Predecessor.SuccessorNodes.Count > 1) {
                        // It is a divergent link.
                        if (Predecessor.ElementType.Equals(PfcElementType.Step)) {
                            return AggregateLinkType.SeriesDivergent;
                        } else if (Predecessor.ElementType.Equals(PfcElementType.Transition)) {
                            return AggregateLinkType.ParallelDivergent;
                        }
                    } else if (Successor.PredecessorNodes.Count > 1) {
                        // It is a convergent link.
                        if (Predecessor.ElementType.Equals(PfcElementType.Step)) {
                            return AggregateLinkType.ParallelConvergent;
                        } else if (Predecessor.ElementType.Equals(PfcElementType.Transition)) {
                            return AggregateLinkType.SeriesConvergent;
                        }
                    }
                }

                _Debug.Assert(false, string.Format("Unable to determine the aggregate link type of {0}.",ToString()));

                return AggregateLinkType.Unknown;
            }
        }

        /// <summary>
        /// Gets the type of this element.
        /// </summary>
        /// <value>The type of the element.</value>
        public override PfcElementType ElementType {
            get { return PfcElementType.Link; }
        }

        #endregion Implementation of Abstract Subclass "SfcElement"

        private static readonly string linkErrorString = "Trying to link {0} to {1}, where {2} already has a {3}.";


        /// <summary>
        /// Class LinkComparer orders links first by priority (default is zero) then by predecessor name, then by Guid. 
        /// (Using Guid is a last resort to ensure repeatability.)
        /// </summary>
        /// <seealso cref="IPfcLinkElement" />
        public class LinkComparer : IComparer<IPfcLinkElement> {
            public int Compare(IPfcLinkElement x, IPfcLinkElement y) {
                int retval = Comparer.Default.Compare(x.Priority, y.Priority) * -1; // High priorities happen first. Ergo, high-to-low.
                if (retval == 0 && x.Successor!= null && y.Successor!= null)
                {
                    retval = Comparer<string>.Default.Compare(x.Successor.Name, y.Successor.Name);
                }
                if (retval == 0) {
                    retval = Utility.GuidOps.Compare(x.Guid, y.Guid);
                }
                return retval;
            }
        }

    }
    
    /// <summary>
    /// StructureViolationException is thrown when a Sequential Function Chart is has just undergone an illegal change in structure.
	/// </summary>
	[Serializable]
	public class PfcStructureViolationException : Exception {
        // For best practice guidelines regarding the creation of new exception types, see
        //    https://msdn.microsoft.com/en-us/library/5b2yeyab(v=vs.110).aspx
        #region protected ctors
        /// <summary>
        /// Initializes a new instance of this class with serialized data. 
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown. </param>
        /// <param name="context">The <see cref="System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected PfcStructureViolationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
		#endregion
		#region public ctors
		/// <summary>
		/// Creates a new instance of this class.
		/// </summary>
		public PfcStructureViolationException() { }
		/// <summary>
		/// Creates a new instance of this class with a specific message.
		/// </summary>
		/// <param name="message">The exception message.</param>
		public PfcStructureViolationException(string message) : base(message) { }
		/// <summary>
		/// Creates a new instance of this class with a specific message and an inner exception.
		/// </summary>
		/// <param name="message">The exception message.</param>
		/// <param name="innerException">The exception inner exception.</param>
        public PfcStructureViolationException(string message, Exception innerException) : base(message, innerException) { }
		#endregion
	}

}
