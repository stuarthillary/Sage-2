/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Dependencies;
using System;
using System.Collections;

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// Delegate Initializer is implemented by any method that wishes to be called for initialization by an InitializationManager.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="parameters">The parameters.</param>
    public delegate void Initializer(IModel model, object[] parameters);

    public delegate void InitializationEvent(int generation);
    public delegate void InitializationAction(Initializer initializer, object[] parameters);

    /// <summary>
    /// The InitializationManager provides methods and mechanisms for running the initialization of a model.
    /// </summary>
    public class InitializationManager : IModelService
    {

        #region Private Fields

        private static readonly object _token = new object();
        private ArrayList _zeroDependencyInitializers;
        private GraphSequencer _gs;
        private Hashtable _verts;
        private IModel _model;
        private int _generation = -1;
        private readonly Action<IModel> _initAction;

        #endregion 

        public event InitializationAction InitializationAction;
        public event InitializationEvent InitializationBeginning;
        public event InitializationEvent InitializationCompleted;

        /// <summary>
        /// Gets or sets a value indicating whether this instance has been initialized yet.
        /// </summary>
        /// <value><c>true</c> if this instance is initialized; otherwise, <c>false</c>.</value>
        public bool IsInitialized
        {
            get; set;
        }

        /// <summary>
        /// Gets a value indicating whether the service is to be automatically initialized inline when
        /// the service is added to the model, or if the user (i.e. the custom model class) will do so later.
        /// </summary>
        /// <value><c>true</c> if initialization is to occur inline, otherwise, <c>false</c>.</value>
        public bool InlineInitialization => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializationManager"/> class. An entity created this way will 
        /// perform initialization actions when the application's state machine transitions into a specified state.
        /// </summary>
        /// <param name="initState">The state, in the model's state machine, whose entry-to will invoke initialization.</param>
        public InitializationManager(Enum initState)
        {
            _initAction = m =>
            {
                m.StateMachine.InboundTransitionHandler(initState).Commit +=
                    new CommitTransitionEvent(model_ModelInitializing);
                Clear();
            };

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializationManager"/> class. An entity created this way will 
        /// perform initialization actions when the application's state machine transitions from one specified state to another.
        /// </summary>
        /// <param name="initFromState">The transition source state.</param>
        /// <param name="initToState">The transition destination state.</param>
        public InitializationManager(Enum initFromState, Enum initToState)
        {
            _initAction = m =>
            {
                _model = m;
                _model.StateMachine.TransitionHandler(initFromState, initToState).Commit +=
                    new CommitTransitionEvent(model_ModelInitializing);
                Clear();
            };
        }


        public void InitializeService(IModel model)
        {
            _initAction(model);
        }

        public void Clear()
        {
            _gs = new GraphSequencer();
            _verts = new Hashtable();
            _zeroDependencyInitializers = new ArrayList();
        }

        public int Generation
        {
            get
            {
                return _generation;
            }
        }

        public void AddInitializationTask(Initializer initializer, params object[] parameters)
        {

            bool zeroDependencies = true;
            foreach (object obj in parameters)
            {
                if ((obj is Guid) || (obj is Guid[]) || (obj is Guid[][]) || (obj is Guid[][][]))
                {
                    zeroDependencies = false;
                    break;
                }
            }

            if (zeroDependencies)
            {
                _zeroDependencyInitializers.Add(new object[] { initializer, parameters });
            }
            else
            {
                //if ( n%1000 == 0 ) Console.WriteLine(n);
                //n++;
                Guid myGuid = Guid.Empty;
                try
                {
                    myGuid = (Guid)initializer.Target.GetType().GetProperty("Guid").GetValue(initializer.Target, new object[] { });
                }
                catch (NullReferenceException)
                {
                    Console.WriteLine("Failed to find a \"Guid\" property on a " + initializer.Target.GetType().Name + ".");
                    return;
                }

                if (myGuid.Equals(Guid.Empty))
                {
                    throw new InitializationException(REGISTERING_GUID_EMPTY);
                }

                Dv myDv = (Dv)_verts[myGuid];
                if (myDv == null)
                {
                    myDv = new Dv(myGuid);
                    _verts.Add(myGuid, myDv);
                }
                myDv.Initializer = initializer;

                foreach (object obj in parameters)
                {
                    if (obj is Guid[])
                    {
                        foreach (Guid g in (Guid[])obj)
                            GetDvForGuid(g).AddPredecessor(myDv);
                    }
                    else
                    {
                        if (obj is Guid)
                        {
                            Guid g = (Guid)obj;

                            if (g.Equals(Guid.Empty))
                            {

                            }
                            else
                            {
                                GetDvForGuid(g).AddPredecessor(myDv);
                            }
                        }
                    }
                }

                myDv.Parameters = parameters;

                _gs.AddVertex(myDv);
            }
        }

        private Dv GetDvForGuid(Guid guid)
        {
            Dv dv = (Dv)_verts[guid];
            if (dv == null)
            {
                dv = new Dv(guid);
                _verts.Add(dv.MyGuid, dv);
            }
            return dv;
        }

        private void model_ModelInitializing(IModel model, object userData)
        {

            lock (_token)
            {

                _generation++;

                IList dependentList = _gs.GetServiceSequenceList();
                IList independentList = _zeroDependencyInitializers;

                if (_generation == 0)
                    ValidateModel(dependentList);

                Clear();

                InitializationBeginning?.Invoke(_generation);

                // First call into the ones that don't need it.
                foreach (object[] oa in independentList)
                {
                    Initializer initializer = (Initializer)oa[0];
                    object[] parameters = (object[])oa[1];
                    InitializationAction?.Invoke(initializer, parameters);
                    initializer(model, parameters);
                }

                // Next walk through the ones that do, in an appropriate sequence.
                try
                {
                    foreach (Dv dv in dependentList)
                    {
                        InitializationAction?.Invoke(dv.Initializer, dv.Parameters);
                        dv.PerformInitialization(model);
                    }

                }
                catch (GraphCycleException gce)
                {

                    IList cycleMembers = (ArrayList)gce.Members;
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();

                    for (int i = 0; i < cycleMembers.Count; i++)
                    {
                        if (i != 0)
                            sb.Append("->");
                        object target = ((Dv)cycleMembers[i]).Initializer.Target;
                        Type mbrType = target.GetType();
                        string name = null;
                        System.Reflection.PropertyInfo nameProp = mbrType.GetProperty("Name");
                        if (nameProp == null)
                        {
                            name = "(unknown " + cycleMembers[i].GetType().Name + ")";
                        }
                        else
                        {
                            name = (string)nameProp.GetValue(target, new object[] { });
                        }
                        sb.Append(name);
                    }

                    throw new ApplicationException("Failure to initialize due to a cyclical dependency involving [...->" + sb + "->...].");

                    //string cycle = FindCycle(cyclist);
                    //Console.WriteLine("Cycle is " + cycle + ".");
                }

                // If more initializers have been added during this generation, run another generation of initialization.
                if (_gs.GetServiceSequenceList().Count > 0 || _zeroDependencyInitializers.Count > 0)
                    model_ModelInitializing(model, userData);

                if (_generation == 0)
                {
                    InitializationCompleted?.Invoke(_generation);
                }

                _generation--;

            } // End of lock.

            // When all initialization iterations have completed...
        }

        private void ValidateModel(IList dependentVertices)
        {
            // TODO: This needs to be tested and reinstated.
            //			ArrayList al = new ArrayList();
            //			foreach ( DV dv in dependentVertices ) {
            //				if ( dv.ParentsList.Count == 0 ) {
            //					al.Add(dv);
            //				}
            //			}
            //
            //			if ( al.Count > 1 ) {
            //				string roots = "";
            //				for ( int i = 0; i < al.Count; i++ ) {
            //					roots+=string.Format("\"{0}\"[{1}]",((DV)al[i]).SortCriteria,((DV)al[i]).MyGuid);
            //					if ( i != al.Count-2 ) roots+=", "; else roots+=" and ";
            //				}
            //				throw new ApplicationException("Model has more than one root node - root nodes are " + roots + ". Perhaps an initializer argument is null or wrong, or maybe an initializer Arg Array is incomplete?");
            //			}
            //
            //			if ( al.Count == 0 ) {
            //				throw new ApplicationException("Model has zero root nodes. Perhaps the model itself is cited as a reference in some sub-node?");
            //			}
        }

        public static object[] Merge(object[] p1, object[] p2)
        {
            object[] p3 = new object[p1.Length + p2.Length];
            p1.CopyTo(p3, 0);
            p2.CopyTo(p3, p1.Length);
            return p3;
        }

        public static readonly string REGISTERING_GUID_EMPTY = "Detected an attempt to register an object for initialization, whose guid is Guid.Empty. This is not permitted, as the object's Guid is the way that others refer to it in dependency lists.";

        private class Dv : IDependencyVertex
        {
            private string _name;
            private Initializer _initializer;
            private Guid _myGuid;
            private readonly ArrayList _predecessors;
            private object[] _parameters;

            public Dv(Guid myGuid)
            {
                _myGuid = myGuid;
                _name = null;
                _initializer = null;
                _predecessors = new ArrayList();
                _parameters = null;
            }

            public Guid MyGuid
            {
                get
                {
                    return _myGuid;
                }
            }
            public Initializer Initializer
            {
                get
                {
                    return _initializer;
                }
                set
                {
                    _initializer = value;
                }
            }
            public object[] Parameters
            {
                get
                {
                    return _parameters;
                }
                set
                {
                    _parameters = value;
                }
            }

            public void PerformInitialization(IModel model)
            {
                if (_initializer != null)
                {
                    _initializer(model, _parameters);
                }
                else
                {
                    System.Diagnostics.Debugger.Break();
                    Console.WriteLine("Failed to find an initializer on a DV with a guid of " + _myGuid + ".");

                }
            }

            public string Name
            {
                get
                {
                    return _name;
                }
            }

            public void AddPredecessor(Dv thePredecessor)
            {
                _predecessors.Add(thePredecessor);
            }

            #region IDependencyVertex Members

            public IComparable SortCriteria
            {
                get
                {
                    if (_name == null)
                    {
                        object tgt = _initializer.Target;
                        _name = (string)tgt.GetType().GetProperty("Name").GetValue(tgt, new object[] { });
                    }
                    return _name;
                }
            }

            public ICollection PredecessorList
            {
                get
                {
                    return _predecessors; // Anything that is referred to in object 'X''s initialization must first be initialized.
                }
            }
            #endregion

            public override string ToString()
            {
                object tgt = _initializer.Target;
                string objType = (string)tgt.GetType().FullName;
                string name = (string)tgt.GetType().GetProperty("Name").GetValue(tgt, new object[] { });
                return objType + " named " + name;
            }


        }
    }
}
