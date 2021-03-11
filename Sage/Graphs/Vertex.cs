/* This source code licensed under the GNU Affero General Public License */


using Highpoint.Sage.Persistence;
using Highpoint.Sage.SimCore; // For executive.
using System;
using System.Collections;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Graphs
{
    public delegate void VertexEvent(IDictionary graphContext, Vertex theVertex);
    public delegate void TriggerDelegate(IDictionary graphContext);

    public class Vertex : IVertex
    {

        public enum WhichVertex { Pre, Post };

        #region Public Events
        public event StaticEdgeEvent PreEdgeAddedEvent;
        public event StaticEdgeEvent PostEdgeAddedEvent;
        public event StaticEdgeEvent PreEdgeRemovedEvent;
        public event StaticEdgeEvent PostEdgeRemovedEvent;

        public event VertexEvent BeforeVertexFiringEvent;
        public event VertexEvent AfterVertexFiringEvent;
        #endregion

        internal int NumPreEdges = 0;
        internal int NumPostEdges = 0;

        // TODO: Tune this implementation for efficiency. Clarity is key, now.
        protected ArrayList PreEdges = new ArrayList(2);
        protected ArrayList PostEdges = new ArrayList(2);

        #region Private Fields
        private static readonly bool _diagnostics = Diagnostics.DiagnosticAids.Diagnostics("Vertex");
        private static readonly bool _managePostMortemData = Diagnostics.DiagnosticAids.Diagnostics("Graph.KeepPostMortems");
        private static readonly ArrayList _emptyCollection = ArrayList.ReadOnly(new ArrayList());

        private string _name;
        private Edge _principalEdge;
        private readonly PreEdgesSatisfiedKey _preEdgesSatisfiedKey = new PreEdgesSatisfiedKey();

        private static int _vertexNum = 0;

        private VertexSynchronizer _synchronizer;
        private IEdgeFiringManager _edgeFiringManager = null;
        private IEdgeReceiptManager _edgeReceiptManager = null;

        private WhichVertex _role;
        private bool _roleIsKnown = false;

        private TriggerDelegate _triggerDelegate;
        #endregion

        #region Constructors
        public Vertex(Edge principalEdge) : this(principalEdge, "Vertex" + (_vertexNum++)) { }

        public Vertex(Edge principalEdge, string name)
        {
            _name = name;
            _principalEdge = principalEdge;
            _triggerDelegate = new TriggerDelegate(DefaultVertexFiringMethod);
        }
        #endregion


        public WhichVertex Role
        {
            get
            {
                if (!_roleIsKnown)
                {
                    _role = (_principalEdge.PreVertex == this ? WhichVertex.Pre : WhichVertex.Post);
                    _roleIsKnown = true;
                }
                return _role;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }


        /// <summary>
        /// The edge firing manager is responsible for determining which successor edges fire,
        /// following satisfaction of a vertex. If this is null, it is assumed that all
        /// edges are to fire. If it is non-null, then each successor edge is presented to
        /// the EdgeFiringManager on it's FireIfAppropriate(Edge e) API to determine if it
        /// should fire.
        /// </summary>
        public IEdgeFiringManager EdgeFiringManager
        {
            get
            {
                return _edgeFiringManager;
            }
            set
            {
                _edgeFiringManager = value;
            }
        }

        /// <summary>
        /// The edge receipt manager is notified of the satisfaction (firing) of pre-edges, and
        /// is responsible for determining when the vertex is to fire. If it is null, then it is
        /// assumed that only if all incoming edges have fired, is the vertex to fire.
        /// </summary>
        public IEdgeReceiptManager EdgeReceiptManager
        {
            get
            {
                return _edgeReceiptManager;
            }
            set
            {
                _edgeReceiptManager = value;
            }
        }


        #region Add, Remove and Access Pre and Post edges including Principal edge.
        public Edge PrincipalEdge
        {
            get
            {
                return _principalEdge;
            }
        }

        public IList PredecessorEdges
        {
            get
            {
                return ArrayList.ReadOnly(PreEdges);
            }
        }

        public IList SuccessorEdges
        {
            get
            {
                return ArrayList.ReadOnly(PostEdges);
            }
        }

        public void AddPreEdge(Edge preEdge)
        {
            if (PreEdges.Contains(preEdge))
                return;
            bool hasVm = (_vm != null);
            if (hasVm)
                _vm.Suspend();
            if (_diagnostics)
                _Debug.WriteLine(String.Format("{0} adding preEdge {1}.", Name, preEdge.Name));
            PreEdges.Add(preEdge);
            System.Threading.Interlocked.Increment(ref NumPreEdges);
            if (PreEdgeAddedEvent != null)
                PreEdgeAddedEvent(PrincipalEdge);
            if (StructureChangeHandler != null)
                StructureChangeHandler(this, StructureChangeType.AddPreEdge, false);
            if (hasVm)
                _vm.Resume();
        }

        public void RemovePreEdge(Edge preEdge)
        {
            if (!PreEdges.Contains(preEdge))
                return;
            bool hasVm = (_vm != null);
            if (hasVm)
                _vm.Suspend();
            if (_diagnostics)
                _Debug.WriteLine(String.Format("{0} removing preEdge {1}.", Name, preEdge.Name));
            PreEdges.Remove(preEdge);
            System.Threading.Interlocked.Decrement(ref NumPreEdges);
            if (PreEdgeRemovedEvent != null)
                PreEdgeRemovedEvent(PrincipalEdge);
            if (StructureChangeHandler != null)
                StructureChangeHandler(this, StructureChangeType.RemovePreEdge, false);
            if (hasVm)
                _vm.Resume();
        }

        public void AddPostEdge(Edge postEdge)
        {
            if (PostEdges.Contains(postEdge))
                return;
            bool hasVm = (_vm != null);
            if (hasVm)
                _vm.Suspend();
            if (_diagnostics)
                _Debug.WriteLine(String.Format("{0} adding postEdge {1}.", Name, postEdge.Name));
            PostEdges.Add(postEdge);
            System.Threading.Interlocked.Increment(ref NumPostEdges);
            if (PostEdgeAddedEvent != null)
                PostEdgeAddedEvent(PrincipalEdge);
            if (StructureChangeHandler != null)
                StructureChangeHandler(this, StructureChangeType.AddPostEdge, false);
            if (hasVm)
                _vm.Resume();
        }

        public void RemovePostEdge(Edge postEdge)
        {
            if (!PostEdges.Contains(postEdge))
                return;
            bool hasVm = (_vm != null);
            if (hasVm)
                _vm.Suspend();
            if (_diagnostics)
                _Debug.WriteLine(String.Format("{0} removing postEdge {1}.", Name, postEdge.Name));
            PostEdges.Remove(postEdge);
            System.Threading.Interlocked.Decrement(ref NumPostEdges);
            if (PostEdgeRemovedEvent != null)
                PostEdgeRemovedEvent(PrincipalEdge);
            if (StructureChangeHandler != null)
                StructureChangeHandler(this, StructureChangeType.RemovePostEdge, false);
            if (hasVm)
                _vm.Resume();
        }
        #endregion


        #region Handlers for actually firing the vertex
        /// <summary>
        /// This property represents the firing method will be called when it is time to fire the vertex. The developer may
        /// substitute a delegate that performs some activity prior to actually firing the vertex. This
        /// substituted delegate must, after doing whatever it does, call the DefaultVertexFiringMethod(graphContext)...
        /// </summary>
        public TriggerDelegate FireVertex
        {
            get { return _triggerDelegate; }
            set { _triggerDelegate = value; }
        }

        /// <summary>
        /// This is the default method used to fire this vertex.
        /// </summary>
        /// <param name="graphContext">The graph context for execution.</param>
        public void DefaultVertexFiringMethod(IDictionary graphContext)
        {
            if (_synchronizer != null)
            {
                _synchronizer.NotifySatisfied(this, graphContext);
                return;
            }
            else
            {
                _FireVertex(graphContext);
            }
        }

        /// This is here as a target for an event handler in case the vertex firing is desired to be
        /// done asynchronously.
        /// <param name="exec">The executive by which this event is being serviced.</param>
        /// <param name="graphContext">The graph context for execution.</param>
        internal void _AsyncFireVertexHandler(IExecutive exec, object graphContext)
        {
            _FireVertex((IDictionary)graphContext);
        }

        /// <summary>
        /// This method is called when it's time to fire the vertex, and even the vertex's
        /// synchronizer (if it has one) has been satisfied.
        /// </summary>
        /// <param name="graphContext">The graphContext of the current event thread.</param>
        internal void _FireVertex(IDictionary graphContext)
        {

            // Start by notifying anyone who cares that we're about to fire the vertex.
            if (BeforeVertexFiringEvent != null)
                BeforeVertexFiringEvent(graphContext, this);

            #region Manage Post-Mortem Data
#if DEBUG
            if (_managePostMortemData)
            {
                PmData pmData = (PmData)graphContext["PostMortemData"];
                if (pmData == null)
                {
                    pmData = new PmData();
                    graphContext.Add("PostMortemData", pmData);
                }
                pmData.VerticesFired.Add(this);
            }
#endif //DEBUG
            #endregion

            if (_diagnostics)
                _Debug.WriteLine("Firing vertex " + Name);

            if (_edgeFiringManager != null)
                _edgeFiringManager.Start(graphContext);

            // If this is a preVertex, we want to make sure the principal edge is fired first,
            // otherwise it doesn't matter. We fire all successor (post) edges.
            if (Role.Equals(WhichVertex.Pre))
            {
                if (_edgeFiringManager == null)
                {
                    _principalEdge.PreVertexSatisfied(graphContext);
                }
                else
                {
                    _edgeFiringManager.FireIfAppropriate(graphContext, _principalEdge);
                }
            }

            foreach (Edge e in PostEdges)
            {
                if (e.Equals(_principalEdge))
                    continue; // We've already fired it.
                if (_edgeFiringManager == null)
                    e.PreVertexSatisfied(graphContext);
                else
                    _edgeFiringManager.FireIfAppropriate(graphContext, e);
            }

            // Finish with a notification of completion.
            if (AfterVertexFiringEvent != null)
                AfterVertexFiringEvent(graphContext, this);
        }

        #endregion

        /// <summary>
        /// This method is called when an incoming pre-edge has been fired, and it could therefore
        /// be time to fire this vertex.
        /// </summary>
        /// <param name="graphContext">The graphContext in whose context this traversal of the graph
        /// is to take place.</param>
        /// <param name="theEdge">The edge that was just fired.</param>
        public void PreEdgeSatisfied(IDictionary graphContext, Edge theEdge)
        {
            if (_edgeReceiptManager == null)
            {
                #region Default Edge Firing Handling
                if (PreEdges.Count < 2)
                { // If there's only one pre-edge, it must be okay to fire the vertex.
                    FireVertex(graphContext);
                }
                else
                {
                    ArrayList preEdgesSatisfied = (ArrayList)graphContext[_preEdgesSatisfiedKey];
                    if (preEdgesSatisfied == null)
                    {
                        preEdgesSatisfied = new ArrayList();
                        graphContext[_preEdgesSatisfiedKey] = preEdgesSatisfied;
                    }

                    if (PreEdges == null)
                        throw new ApplicationException("Edge (" + theEdge + ") signaled completion to " + this + ", a node with no predecessor edges.");

                    if (!PreEdges.Contains(theEdge))
                        throw new ApplicationException("Unknown edge (" + theEdge + ") signaled completion to " + this);

                    if (preEdgesSatisfied.Contains(theEdge))
                        throw new ApplicationException("Edge (" + theEdge + ") signaled completion twice, to " + this);

                    preEdgesSatisfied.Add(theEdge);

                    if (preEdgesSatisfied.Count == PreEdges.Count)
                    {
                        graphContext.Remove(this); // Remove the preEdgesSatisfied arraylist. Implicit recycle.
                        FireVertex(graphContext);
                    }
                }
                #endregion
            }
            else
            {
                _edgeReceiptManager.OnPreEdgeSatisfied(graphContext, theEdge);
            }
        }

        #region Synchronizer Management
        /// <summary>
        /// A synchronizer, ip present, defines a relationship among vertices wherein all vertices
        /// wait until they are all ready to fire, and then they fire in the specified order.
        /// </summary>
        public VertexSynchronizer Synchronizer
        {
            get
            {
                return _synchronizer;
            }
        }

        /// <summary>
        /// This is used as an internal accessor to set synchronizer to null, or other values. The
        /// property is public, but read-only.
        /// </summary>
        /// <param name="synch"></param>
        internal void SetSynchronizer(VertexSynchronizer synch)
        {
            if (synch != null && _synchronizer != null)
                throw new ApplicationException(Name + " already has a synchronizer assigned!");
            bool hasVm = (_vm != null);
            if (hasVm)
                _vm.Suspend();
            _synchronizer = synch;
            if (StructureChangeHandler != null)
                StructureChangeHandler(this, StructureChangeType.NewSynchronizer, false);
            if (hasVm)
                _vm.Resume();
        }

        #endregion

        #region IVisitable Members
        public virtual void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
        #endregion

        #region IXmlPersistable Members
        public Vertex() { }
        public virtual void SerializeTo(XmlSerializationContext xmlsc)
        {
            xmlsc.StoreObject("Name", _name);
            xmlsc.StoreObject("PostEdges", PostEdges);
            xmlsc.StoreObject("PreEdges", PreEdges);
            xmlsc.StoreObject("PrincipalEdge", _principalEdge);
            xmlsc.StoreObject("Role", _role);
            xmlsc.StoreObject("RoleIsKnown", _roleIsKnown);
            xmlsc.StoreObject("Synchronizer", _synchronizer);
            xmlsc.StoreObject("TriggerDelegate", _triggerDelegate);
            // What about Trigger Delegate?
        }

        public virtual void DeserializeFrom(XmlSerializationContext xmlsc)
        {
            _name = (string)xmlsc.LoadObject("Name");
            ArrayList tmpPostEdges = (ArrayList)xmlsc.LoadObject("PostEdges");
            foreach (Edge edge in tmpPostEdges)
            {
                if (!PostEdges.Contains(edge))
                    PostEdges.Add(edge);
                NumPostEdges++;
            }
            ArrayList tmpPreEdges = (ArrayList)xmlsc.LoadObject("PreEdges");
            foreach (Edge edge in tmpPreEdges)
            {
                if (!PreEdges.Contains(edge))
                    PreEdges.Add(edge);
                NumPreEdges++;
            }

            _principalEdge = (Edge)xmlsc.LoadObject("PrincipalEdge");
            _role = (WhichVertex)xmlsc.LoadObject("Role");
            _roleIsKnown = (bool)xmlsc.LoadObject("RoleIsKnown");
            _synchronizer = (VertexSynchronizer)xmlsc.LoadObject("Synchronizer");
            _triggerDelegate = (TriggerDelegate)xmlsc.LoadObject("TriggerDelegate");
            //			_Debug.WriteLine("Deserializing " + m_name + " : it has " + m_postEdges.Count + " post edges in object w/ hashcode " 
            //				+ m_postEdges.GetHashCode() + ". (BTW, this has hashcode " + this.GetHashCode() + ").");
        }

        #endregion

        /// <summary>
        /// Returns the name of this vertex.
        /// </summary>
        /// <returns>The name of this vertex.</returns>
        public override string ToString()
        {
            return _name;
        }

        #region IHasValidity Members

        private Validity.ValidationService _vm = null;
        public Validity.ValidationService ValidationService
        {
            get
            {
                return _vm;
            }
            set
            {
                _vm = value;
            }
        }

        public Validity.Validity SelfState
        {
            get
            {
                return Validity.Validity.Valid;
            }
            set
            {
            }
        }

        public void NotifyOverallValidityChange(Validity.Validity newValidity)
        {
            //Console.WriteLine(Name + " is becoming " + newValidity);

            //if ( ValidityChangeEvent != null ) ValidityChangeEvent(this,newValidity);
        }

        public event Validity.ValidityChangeHandler ValidityChangeEvent
        {
            add { }
            remove { }
        }

        public IList GetChildren()
        {
            return _emptyCollection;
        }

        public IList GetSuccessors()
        {
            ArrayList retval = new ArrayList(PostEdges);
            if (_synchronizer != null)
            {
                bool vrtxComesAfterMe = false;
                foreach (Vertex v in _synchronizer.Members)
                {
                    if (vrtxComesAfterMe)
                        retval.Add(v);
                    if (Equals(v))
                        vrtxComesAfterMe = true;
                }
            }
            return retval;
        }

        public Validity.IHasValidity GetParent() { return null; }

        #endregion

        #region IPartOfGraphStructure Members

        public event StructureChangeHandler StructureChangeHandler;

        //		public void PropagateStructureChange(object obj, StructureChangeType sct, bool isPropagated){
        //			if ( StructureChangeHandler != null ) StructureChangeHandler(obj,sct,isPropagated);
        //		}
        #endregion
    }
}
