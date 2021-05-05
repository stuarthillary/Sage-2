/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Graphs.Validity
{

    /// <summary>
    /// A callback that is expected to be invoked when something's state of validity changes.
    /// </summary>
    /// <param name="ihv">The object that has validity.</param>
    /// <param name="newState">The new value that the object's validity has taken on.</param>
	public delegate void ValidityChangeHandler(IHasValidity ihv, Validity newState);


    /// <summary>
    /// A class that abstracts and manages validity relationships in a directed acyclic graph. If
    /// an object is invalid, its parent, grandparent (etc.), and downstream objects in that graph
    /// are also seen to be invalid.
    /// </summary>
    public class ValidationService
    {

        #region >>> Private Fields <<<
        private static readonly bool _diagnostics = Diagnostics.DiagnosticAids.Diagnostics("ValidationService");
        private static readonly ArrayList _empty_List = ArrayList.ReadOnly(new ArrayList());
        private static readonly Utility.WeakList _knownServices = new Utility.WeakList();
        private readonly IHasValidity _root;
        private int _suspensions;
        private bool _dirty;
        private readonly StructureChangeHandler _structureChangeListener;
        private Hashtable _htNodes;
        private readonly Stack _suspendResumeStack;
        private Hashtable _oldValidities = null; // For holding pre-refresh validities so that refresh can fire the right change events.
        #endregion

        /// <summary>
        /// Gets a list of the known ValidationServices.
        /// </summary>
        /// <value>The known services.</value>
        public static IList KnownServices
        {
            get
            {
                return _knownServices;
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="T:ValidationService"/> class.
        /// </summary>
        /// <param name="root">The root task of the directed acyclic graph.</param>
		public ValidationService(Tasks.Task root)
        {
            _root = root.PreVertex;
            _suspensions = 0;
            _dirty = true;
            _structureChangeListener = new StructureChangeHandler(OnStructureChange);
            if (_diagnostics)
                _suspendResumeStack = new Stack();
            _knownServices.Add(this);
            Refresh();

        }

        /// <summary>
        /// Suspends validation computations to control cascaded recomputations. Suspend recomputation, make a bunch 
        /// of changes, and resume. If anything needs to be recalculated, it will be. This also implements a nesting
        /// capability, so if a suspend has been done 'n' times (perhaps in a call stack), the recomputation will only
        /// be done once all 'n' suspends have been resumed.
        /// </summary>
		public void Suspend()
        {
            if (_suspensions == 0 && _htNodes != null)
            {
                _oldValidities = new Hashtable();
                foreach (ValidityNode vn in _htNodes.Values)
                    _oldValidities.Add(vn.Mine, vn.OverallValid);
            }

            _suspensions++;

            if (_diagnostics)
            {
                System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(0, true);
                System.Diagnostics.StackFrame sf = st.GetFrame(1);
                string where = sf.GetMethod() + " [" + sf.GetFileName() + ", line " + sf.GetFileLineNumber() + "]";
                _suspendResumeStack.Push(where);
                //_Debug.WriteLine("Suspend (" + _suspensions + ") : " + where);
            }
        }

        /// <summary>
        /// Resumes this instance. See <see cref="Highpoint.Sage.Graphs.Validity.ValidationService.Suspend()"/>.
        /// </summary>
		public void Resume()
        {
            _suspensions--;
            if (_suspensions < 0)
            {
                if (_diagnostics)
                {
                    System.Diagnostics.Debugger.Break();
                }
                else
                {
                    _suspensions = 0;
                }
            }
            if (_diagnostics)
            {
                System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(0, true);
                System.Diagnostics.StackFrame sf = st.GetFrame(1);
                string where = sf.GetMethod() + " [" + sf.GetFileName() + ", line " + sf.GetFileLineNumber() + "]";
                where = where.Split(new char[] { ',' }, 2)[0];
                string stackThinks = (string)_suspendResumeStack.Pop();
                stackThinks = stackThinks.Split(new char[] { ',' }, 2)[0];
                // TODO: Move this into an Errors & Warnings collection on the model.
                if (!where.Equals(stackThinks, StringComparison.Ordinal))
                {
                    string msg = "ERROR - Validation Service's \"Resume\" location (" + where + ")doesn't match the opposite \"Suspend\" location!";
                    if (_root is SimCore.IModelObject)
                    {
                        ((SimCore.IModelObject)_root).Model.AddWarning(new SimCore.GenericModelWarning("ValidationStackMismatch", msg, where, this));
                    }
                    else
                    {
                        _Debug.WriteLine(msg);
                    }
                }
                //_Debug.WriteLine("Resume  (" + _suspensions + ") : " + where);
            }
            Refresh();
        }

        /// <summary>
        /// Performs the validity computation.
        /// </summary>
        /// <param name="force">if set to <c>true</c> the service ignores whether anything in the graph has changed since the last refresh.</param>
		public void Refresh(bool force)
        {
            if (force)
                _dirty = true;

            _suspensions = 0; // TODO: Check if this scould capture & restore the _suspensions value.

            Refresh();
        }


        /// <summary>
        /// Performs the validity computation if there are no suspensions in progress, and anything in the graph has changed since the last refresh. 
        /// </summary>
		public void Refresh()
        {
            if (_suspensions == 0 && _dirty)
            {
                if (_diagnostics)
                    _Debug.WriteLine("Refreshing after a structure change.");

                if (_htNodes != null)
                {
                    foreach (IHasValidity ihv in _htNodes.Keys)
                    {
                        ihv.StructureChangeHandler -= _structureChangeListener;
                    }
                }

                if (_htNodes != null && _oldValidities == null)
                {
                    _oldValidities = new Hashtable();
                    foreach (ValidityNode vn in _htNodes.Values)
                        _oldValidities.Add(vn.Mine, vn.OverallValid);
                }

                _htNodes = new Hashtable();
                AddNode(_root); // And recursively down from there.

                foreach (ValidityNode vn in _htNodes.Values)
                    vn.EstablishMappingsToValidityNodes();

                foreach (ValidityNode vn in _htNodes.Values)
                    vn.CreateValidityNetwork();

                foreach (ValidityNode vn in _htNodes.Values)
                    vn.Initialize(vn.Mine.SelfState);

                _dirty = false;

                if (_oldValidities != null)
                {
                    // After recreating the graph of validityNodes, we need to update anyone who was watching.
                    foreach (ValidityNode vn in _htNodes.Values)
                    {
                        if (!_oldValidities.Contains(vn.Mine))
                            continue;
                        bool oldValidity = (bool)_oldValidities[vn.Mine];
                        if (vn.OverallValid != oldValidity)
                        {
                            vn.Mine.NotifyOverallValidityChange(vn.OverallValid ? Validity.Valid : Validity.Invalid);
                        }
                    }
                    _oldValidities = null;
                }
            }
        }


        /// <summary>
        /// Creates a status report that describes the validity state of the graph.
        /// </summary>
        /// <returns>A status report that describes the validity state of the graph.</returns>
		public string StatusReport()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (ValidityNode vn in _htNodes.Values)
            {
                sb.Append(vn.Name + " : Self " + vn.SelfValid + ", Preds = " + vn.PredecessorsValid + "(" + vn.InvalidPredecessorCount + ") Children = " + vn.ChildrenValid + "(" + vn.InvalidChildCount + ").\r\n");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Creates a status report that describes the validity state of the graph, at and below the provided node.
        /// </summary>
        /// <param name="ihv">The provided node that is to be the root of this report.</param>
        /// <returns></returns>
        public string StatusReport(IHasValidity ihv)
        {
            ValidityNode vn = (ValidityNode)_htNodes[ihv];
            if (vn == null)
            {
                return "Unknown object - " + ihv;
            }
            else
            {
                return (vn.Name + " : Self " + vn.SelfValid + ", Preds = " + vn.PredecessorsValid + "(" + vn.InvalidPredecessorCount + ") Children = " + vn.ChildrenValid + "(" + vn.InvalidChildCount + ").\r\n");
            }
        }

        private void AddNode(IHasValidity ihv)
        {
            if (!_htNodes.Contains(ihv))
            {
                ValidityNode vn = new ValidityNode(this, ihv);
                _htNodes.Add(ihv, vn);
                IList list = ihv.GetSuccessors();
                if (list.Count > 0)
                {
                    foreach (object obj in list)
                    {
                        IHasValidity successor = (IHasValidity)obj;
                        AddNode(successor);
                    }
                }
            }
            ihv.StructureChangeHandler += _structureChangeListener;
        }


        private void OnStructureChange(object obj, StructureChangeType chType, bool isPropagated)
        {

            #region Rule #1. If a vertex loses or gains a predecessor, it invalidates all of its sucessors.
            if (StructureChangeTypeSvc.IsPreEdgeChange(chType))
            {
                if (obj is Vertex)
                {
                    Vertex vertex = (Vertex)obj;
                    ArrayList successors = new ArrayList();
                    successors.AddRange(vertex.GetSuccessors());
                    while (successors.Count > 0)
                    {
                        IHasValidity ihv = (IHasValidity)successors[0];
                        successors.RemoveAt(0);
                        if (ihv is Tasks.Task)
                        {
                            ((Tasks.Task)ihv).SelfValidState = false;
                        }
                        else
                        {
                            successors.AddRange(ihv.GetSuccessors());
                        }
                    }
                }
            }
            #endregion

            _dirty = true;
        }

        /// <summary>
        /// Notifies the specified object in the graph of its self state change.
        /// </summary>
        /// <param name="ihv">The specified object in the graph.</param>
		public void NotifySelfStateChange(IHasValidity ihv)
        {
            ValidityNode vn = (ValidityNode)_htNodes[ihv];
            if (vn != null)
            {
                vn.NotifySelfStateChange(ihv.SelfState);
            }
        }

        #region >>> Peer Getters (parent, predecessors, successors and children) <<<
        /// <summary>
        /// Gets the predecessors of the specified object in the graph.
        /// </summary>
        /// <param name="ihv">The specified object in the graph.</param>
        /// <returns>The predecessors of the specified object in the graph.</returns>
        public IList GetPredecessorsOf(IHasValidity ihv)
        {
            ValidityNode vn = (ValidityNode)_htNodes[ihv];
            if (vn == null)
                return _empty_List;

            ArrayList retval = new ArrayList();
            foreach (ValidityNode subNode in vn.Predecessors)
            {
                IHasValidity ihv2 = subNode.Mine;
                retval.Add(ihv2);
            }

            return retval;
        }

        /// <summary>
        /// Gets the successors of the specified object in the graph.
        /// </summary>
        /// <param name="ihv">The specified object in the graph.</param>
        /// <returns>The successors of the specified object in the graph.</returns>
		public IList GetSuccessorsOf(IHasValidity ihv)
        {
            ValidityNode vn = (ValidityNode)_htNodes[ihv];
            if (vn == null)
                return _empty_List;

            ArrayList retval = new ArrayList();
            foreach (ValidityNode subNode in vn.Successors)
            {
                IHasValidity ihv2 = subNode.Mine;
                retval.Add(ihv2);
            }

            return retval;
        }

        /// <summary>
        /// Gets the children of the specified object in the graph.
        /// </summary>
        /// <param name="ihv">The specified object in the graph.</param>
        /// <returns>The children of the specified object in the graph.</returns>
		public IList GetChildrenOf(IHasValidity ihv)
        {
            ValidityNode vn = (ValidityNode)_htNodes[ihv];
            if (vn == null)
                return _empty_List;

            ArrayList retval = new ArrayList();
            foreach (ValidityNode subNode in vn.Children)
            {
                IHasValidity ihv2 = subNode.Mine;
                retval.Add(ihv2);
            }

            return retval;
        }

        /// <summary>
        /// Gets the parent of the specified object in the graph.
        /// </summary>
        /// <param name="ihv">The specified object in the graph.</param>
        /// <returns>The parent of the specified object in the graph.</returns>
		public IHasValidity GetParentOf(IHasValidity ihv)
        {
            ValidityNode vn = (ValidityNode)_htNodes[ihv];
            if (vn == null)
                return null;
            if (vn.Parent == null)
                return null;
            return vn.Parent.Mine;
        }

        #endregion

        #region >>> ValidityState Getters (overall, self, predecessors and children) <<<
        /// <summary>
        /// Gets the overall state of the validity of the specified object in the graph.
        /// </summary>
        /// <param name="ihv">The specified object in the graph.</param>
        /// <returns>The</returns>
        public Validity GetValidityState(IHasValidity ihv)
        {
            ValidityNode vn = (ValidityNode)_htNodes[ihv];
            if (vn == null)
                return Validity.Invalid;
            return vn.OverallValid ? Validity.Valid : Validity.Invalid;
        }

        /// <summary>
        /// Gets the state of the self validity of the specified object in the graph.
        /// </summary>
        /// <param name="ihv">The specified object in the graph.</param>
        /// <returns>The state of the self validity of the specified object in the graph.</returns>
		public Validity GetSelfValidityState(IHasValidity ihv)
        {
            ValidityNode vn = (ValidityNode)_htNodes[ihv];
            if (vn == null)
                return Validity.Invalid;
            return vn.SelfValid ? Validity.Valid : Validity.Invalid;
        }

        /// <summary>
        /// Gets the state of validity of the predecessors of the specified object in the graph.
        /// </summary>
        /// <param name="ihv">The specified object in the graph.</param>
        /// <returns>The state of validity of the predecessors of the specified object in the graph.</returns>
		public Validity GetPredecessorValidityState(IHasValidity ihv)
        {
            ValidityNode vn = (ValidityNode)_htNodes[ihv];
            if (vn == null)
                return Validity.Invalid;
            return vn.PredecessorsValid ? Validity.Valid : Validity.Invalid;
        }

        /// <summary>
        /// Gets the aggregate validity state of the children of the specified object in the graph.
        /// </summary>
        /// <param name="ihv">The specified object in the graph.</param>
        /// <returns>The aggregate validity state of the children of the specified object in the graph.</returns>
		public Validity GetChildValidityState(IHasValidity ihv)
        {
            ValidityNode vn = (ValidityNode)_htNodes[ihv];
            if (vn == null)
                return Validity.Invalid;
            return vn.ChildrenValid ? Validity.Valid : Validity.Invalid;
        }

        /// <summary>
        /// Gets the invalid predecessor count of the specified object in the graph.
        /// </summary>
        /// <param name="ihv">The specified object in the graph.</param>
        /// <returns>The invalid predecessor count of the specified object in the graph.</returns>
		public int GetInvalidPredecessorCountOf(IHasValidity ihv)
        {
            ValidityNode vn = (ValidityNode)_htNodes[ihv];
            if (vn == null)
                return int.MinValue;
            return vn.InvalidPredecessorCount;
        }

        /// <summary>
        /// Gets the invalid child count of the specified object in the graph.
        /// </summary>
        /// <param name="ihv">The specified object in the graph.</param>
        /// <returns>The invalid child count of the specified object in the graph.</returns>
		public int GetInvalidChildCountOf(IHasValidity ihv)
        {
            ValidityNode vn = (ValidityNode)_htNodes[ihv];
            if (vn == null)
                return int.MinValue;
            return vn.InvalidChildCount;
        }
        #endregion

        /// <summary>
        /// Gets a list of the IHasValidity objects that are known to this ValidationService.
        /// </summary>
        /// <value>The known validity holders.</value>
        public IList KnownValidityHolders
        {
            get
            {
                ArrayList retval = new ArrayList();
                foreach (ValidityNode vn in _htNodes.Values)
                    retval.Add(vn.Mine);
                return retval;
            }
        }

        private class ValidityNode : SimCore.IHasName
        {

            #region >>> Private Fields <<<
            private readonly IHasValidity _mine;
            private readonly ValidationService _validationService;
            private readonly ArrayList _predecessors;
            private readonly ArrayList _successors;
            private readonly ArrayList _children;
            private ValidityNode _parent;
            private int _nInvalidPredecessors;
            private int _nInvalidChildren;
            #endregion

            public ValidityNode(ValidationService validationService, IHasValidity mine)
            {
                _nInvalidChildren = 0;
                _nInvalidPredecessors = 0;
                _validationService = validationService;
                _mine = mine;
                _mine.ValidationService = _validationService;
                _successors = new ArrayList(mine.GetSuccessors());
                _children = new ArrayList(mine.GetChildren());
                _predecessors = new ArrayList();
                if (_mine is SimCore.IHasName)
                {
                    _name = ((SimCore.IHasName)_mine).Name;
                }
                else
                {
                    _name = _mine.GetType().ToString();
                }
            }

            private string _name;
            public string Name
            {
                get
                {
                    return _name;
                }
            }

            public IHasValidity Mine
            {
                get
                {
                    return _mine;
                }
            }

            #region >>> ValidityState Getters (overall, self, predecessors and children) <<<
            public bool OverallValid
            {
                get
                {
                    return SelfValid && ChildrenValid && PredecessorsValid;
                }
            }

            public bool SelfValid
            {
                get
                {
                    return (_mine.SelfState == Validity.Valid);
                }
            }

            public bool PredecessorsValid
            {
                get
                {
                    return (_nInvalidPredecessors == 0);
                }
            }

            public bool ChildrenValid
            {
                get
                {
                    return _nInvalidChildren == 0;
                }
            }
            #endregion

            public void NotifySelfStateChange(Validity newValidity)
            {
                if (ChildrenValid && PredecessorsValid)
                { // i.e. This selfState change will cause an overall state change.
                    if (newValidity == Validity.Valid)
                    {
                        // We just became valid overall.
                        if (_parent != null)
                            _parent.InvalidChildCount--;
                        foreach (ValidityNode vn in Successors)
                            vn.InvalidPredecessorCount--;

                    }
                    else if (newValidity == Validity.Invalid)
                    {
                        // We just became invalid overall.
                        if (_parent != null)
                            _parent.InvalidChildCount++;
                        foreach (ValidityNode vn in Successors)
                            vn.InvalidPredecessorCount++;
                    }
                    _mine.NotifyOverallValidityChange(newValidity);
                }
            }

            /// <summary>
            /// Gets or sets the invalid predecessor count.
            /// </summary>
            /// <value></value>
            public int InvalidPredecessorCount
            {
                get
                {
                    return _nInvalidPredecessors;
                }
                set
                {
                    bool wasValid = OverallValid;
                    _nInvalidPredecessors = value;

                    if (OverallValid != wasValid)
                        ReactToInvalidation();
                }
            }

            /// <summary>
            /// Gets or sets the invalid child count.
            /// </summary>
            /// <value></value>
            public int InvalidChildCount
            {
                get
                {
                    return _nInvalidChildren;
                }
                set
                {
                    bool wasValid = OverallValid;
                    _nInvalidChildren = value;

                    if (OverallValid != wasValid)
                        ReactToInvalidation();
                }
            }

            /// <summary>
            /// Reacts to invalidation by incrementing parent's invalid child count, successors'
            /// invalid predecessor count, and then telling my Mine element to notify listeners
            /// of its invalidation.
            /// </summary>
            private void ReactToInvalidation()
            {
                _mine.NotifyOverallValidityChange(OverallValid ? Validity.Valid : Validity.Invalid);

                int delta = OverallValid ? -1 : +1;
                if (_parent != null)
                    _parent.InvalidChildCount += delta;
                foreach (ValidityNode vn in Successors)
                    vn.InvalidPredecessorCount += delta;

                _mine.NotifyOverallValidityChange(OverallValid ? Validity.Valid : Validity.Invalid);
            }

            /// <summary>
            /// Establishes mappings between validity nodes and the elements they monitor.
            /// </summary>
            public void EstablishMappingsToValidityNodes()
            {
                if (_successors.Count > 0)
                {
                    if (!(_successors[0] is ValidityNode))
                    {
                        for (int i = 0; i < _successors.Count; i++)
                        {
                            _successors[i] = _validationService._htNodes[_successors[i]];
                        }
                    }
                }

                if (_children.Count > 0)
                {
                    if (!(_children[0] is ValidityNode))
                    {
                        for (int i = 0; i < _children.Count; i++)
                        {
                            _children[i] = _validationService._htNodes[_children[i]];
                        }
                    }
                }

                IHasValidity myParent = _mine.GetParent();
                if (_parent == null && myParent != null)
                    _parent = (ValidityNode)_validationService._htNodes[myParent];

            }

            public void CreateValidityNetwork()
            {
                foreach (ValidityNode succ in _successors)
                {
                    succ._predecessors.Add(this);
                }
            }

            public void Initialize(Validity initialSelfState)
            {
                if (initialSelfState == Validity.Invalid)
                {
                    if (_parent != null)
                        _parent.InvalidChildCount++;
                    foreach (ValidityNode vn in Successors)
                        vn.InvalidPredecessorCount++;
                }
                // If it's valid, it has no effect on parents & predecessors from the initial zero counts.
            }

            #region >>> Peer Getters (parent, predecessors, successors and children) <<<
            public ArrayList Predecessors
            {
                get
                {
                    return _predecessors;
                }
            }
            public ArrayList Successors
            {
                get
                {
                    return _successors;
                }
            }
            public ArrayList Children
            {
                get
                {
                    return _children;
                }
            }
            public ValidityNode Parent
            {
                get
                {
                    return _parent;
                }
            }
            #endregion
        }
    }
}
