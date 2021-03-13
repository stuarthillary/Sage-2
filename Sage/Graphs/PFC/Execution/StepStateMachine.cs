/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Highpoint.Sage.Graphs.PFC.Execution
{
    public delegate void StepStateMachineEvent(StepStateMachine ssm, object userData);

    public class StepStateMachine
    {

        #region Static Configuration Fields
        private static readonly bool[,] _transition_Matrix = new bool[12, 12] { {
            //         IDL    RNG    CMP    ABG    ABD    STG    STD    PSG    PSD    HDG    HLD    RSG
            /* IDL */  false, true , false, false, false, false, false, false, false, false, false, false },{
            /* RNG */  false, false, true , true , false, true , false, true , false, true , false, false },{
            /* CMP */  true , false, false, false, false, false, false, false, false, false, false, false },{
            /* ABG */  false, false, false, false, true , false, false, false, false, false, false, false },{
            /* ABD */  true , false, false, false, false, false, false, false, false, false, false, false },{
            /* STG */  false, false, false, false, false, false, true , false, false, false, false, false },{
            /* STD */  true , false, false, false, false, false, false, false, false, false, false, false },{
            /* PSG */  false, false, false, false, false, false, false, false, true , false, false, false },{
            /* PSD */  false, true , false, false, false, false, false, false, false, false, false, false },{
            /* HDG */  false, false, false, false, false, false, false, false, false, false, true , false },{
            /* HLD */  false, false, false, false, false, false, false, false, false, false, false, true  },{
            /* RSG */  false, true , false, false, false, false, false, false, false, false, false, false }
        };

        private static readonly StepState[] _follow_On_States = new StepState[]{   
          /*Idle            -->*/ StepState.Idle,
          /*Running         -->*/ StepState.Running,
          /*Complete        -->*/ StepState.Complete,
          /*Aborting        -->*/ StepState.Aborted,
          /*Aborted         -->*/ StepState.Aborted,
          /*Stopping        -->*/ StepState.Stopped,
          /*Stopped         -->*/ StepState.Stopped,
          /*Pausing         -->*/ StepState.Paused,
          /*Paused          -->*/ StepState.Paused,
          /*Holding         -->*/ StepState.Holding,
          /*Held            -->*/ StepState.Held,
          /*Restarting      -->*/ StepState.Running
        };

        private static readonly StepState _initial_State = StepState.Idle;
        #endregion

        #region Private Fields
        private IPfcStepNode _myStep = null;
        private List<TransitionStateMachine> _successorStateMachines;
        private static Guid _leafLevelActionMask = new Guid("067769d2-573b-475e-bffe-4a8a8a04cd01");
        private static readonly bool _diagnostics = Diagnostics.DiagnosticAids.Diagnostics("PfcStepStateMachine");
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="StepStateMachine"/> class.
        /// </summary>
        /// <param name="myStep">My step.</param>
        public StepStateMachine(IPfcStepNode myStep)
        {
            _myStep = myStep;
            _successorStateMachines = new List<TransitionStateMachine>();
        }

        public void Start(PfcExecutionContext parentPfcec)
        {
            Debug.Assert(!parentPfcec.IsStepCentric); // Must be called with parent.
            Debug.Assert(parentPfcec.PFC.Equals(MyStep.Parent));

            #region Create a context under the parent PFCEC to run this iteration of this step.
            SsmData ssmData = GetSsmData(parentPfcec); // This will create a new SSMData element.

            if (ssmData.ExecutionInstanceCount == 0)
            {
                ssmData.InitializeExecutionInstanceUid(parentPfcec.Guid, MyStep.Guid);
            }
            Guid myExecutionInstanceGuid = ssmData.GetNextExecutionInstanceUid();

            PfcExecutionContext myPfcec = new PfcExecutionContext(_myStep, _myStep.Name, null, myExecutionInstanceGuid, parentPfcec);
            myPfcec.InstanceCount = ssmData.ExecutionInstanceCount - 1;
            ssmData.ActiveStepInstanceEc = myPfcec;
            if (_diagnostics)
            {
                Console.WriteLine("PFCEC " + myPfcec.Name + "(instance " + myPfcec.InstanceCount + ") created.");
            }
            #endregion

            if (_diagnostics)
            {
                Console.WriteLine("Starting step " + _myStep.Name + " with ec " + myPfcec.Name + ".");
            }

            GetStartPermission(myPfcec);

            // Once we have permission to start (based on state), we will create a new execContext for this execution.
            DoTransition(StepState.Running, myPfcec);
        }

        public void Stop(PfcExecutionContext parentPfcec)
        {
            Debug.Assert(!parentPfcec.IsStepCentric); // Must be called with parent.
            Debug.Assert(parentPfcec.PFC.Equals(MyStep.Parent));

            PfcExecutionContext pfcec = GetActiveInstanceExecutionContext(parentPfcec);
            if (_diagnostics)
            {
                Console.WriteLine("Stopping step " + _myStep.Name + " with ec " + pfcec.Name + ".");
            }

            DoTransition(StepState.Stopping, pfcec);
        }

        public void Reset(PfcExecutionContext parentPfcec)
        {
            Debug.Assert(!parentPfcec.IsStepCentric); // Must be called with parent.
            Debug.Assert(parentPfcec.PFC.Equals(MyStep.Parent));

            PfcExecutionContext pfcec = GetActiveInstanceExecutionContext(parentPfcec);
            if (_diagnostics)
            {
                Console.WriteLine("Resetting step " + _myStep.Name + " with ec " + pfcec.Name + ".");
            }
            DoTransition(StepState.Idle, pfcec);
        }

        /// <summary>
        /// Gets the state of this step.
        /// </summary>
        /// <value>The state.</value>
        public StepState GetState(PfcExecutionContext parentPfcec)
        {
            SsmData ssmData = GetSsmData(parentPfcec);
            return ssmData.State;
        }

        public PfcExecutionContext GetActiveInstanceExecutionContext(PfcExecutionContext pfcEc)
        {
            return GetSsmData(pfcEc).ActiveStepInstanceEc;
        }

        public List<PfcExecutionContext> GetExecutionContexts(PfcExecutionContext pfcEc)
        {
            return GetSsmData(pfcEc).InstanceExecutionContexts;
        }

        private class SsmData
        {
            private StepState _state = _initial_State;
            private readonly Queue<IDetachableEventController> _qIdec = new Queue<IDetachableEventController>();
            private Guid _nextExecutionInstanceUid = Guid.Empty;
            private int _numberOfIterations = 0;
            private PfcExecutionContext _currentStepInstanceEc = null;
            private readonly List<PfcExecutionContext> _lstStepInstanceECs = new List<PfcExecutionContext>();
            public SsmData()
            {
            }

            public StepState State
            {
                get
                {
                    return _state;
                }
                set
                {
                    _state = value;
                }
            }
            public Queue<IDetachableEventController> QueueIdec
            {
                get
                {
                    return _qIdec;
                }
            }
            public int ExecutionInstanceCount
            {
                get
                {
                    return _numberOfIterations;
                }
            }
            public PfcExecutionContext ActiveStepInstanceEc
            {
                [DebuggerStepThrough]
                get
                {
                    return _currentStepInstanceEc;
                }
                set
                {
                    Debug.Assert(value == null || _currentStepInstanceEc == null);
                    if (value != null)
                    {
                        InstanceExecutionContexts.Add(value);
                    }
                    _currentStepInstanceEc = value;
                }
            }
            public List<PfcExecutionContext> InstanceExecutionContexts
            {
                get
                {
                    return _lstStepInstanceECs;
                }
            }
            internal Guid GetNextExecutionInstanceUid()
            {
                Debug.Assert(!_nextExecutionInstanceUid.Equals(Guid.Empty));
                Guid retval = _nextExecutionInstanceUid;
                _nextExecutionInstanceUid = GuidOps.Increment(_nextExecutionInstanceUid);
                _numberOfIterations++;
                return retval;
            }

            internal void InitializeExecutionInstanceUid(Guid parentExecutionContextUid, Guid myStepUid)
            {
                Debug.Assert(_nextExecutionInstanceUid.Equals(Guid.Empty));
                _nextExecutionInstanceUid = GuidOps.XOR(parentExecutionContextUid, myStepUid);
            }
        }

        private SsmData GetSsmData(PfcExecutionContext pfcec)
        {
            if (MyStep.Equals(pfcec.Step))
            {
                pfcec = (PfcExecutionContext)pfcec.Parent;
            }

            if (!pfcec.Contains(this))
            {
                SsmData retval = new SsmData();
                pfcec.Add(this, retval);
            }

            return (SsmData)pfcec[this];
        }

        /// <summary>
        /// Gets the PFC step that this state machine represents.
        /// </summary>
        /// <value>The step.</value>
        public IPfcStepNode MyStep
        {
            get
            {
                return _myStep;
            }
            internal set
            {
                _myStep = value;
            }
        }

        public bool StructureLocked
        {
            get
            {
                return true;
            }
            set
            {
                ;
            }
        }

        public event StepStateMachineEvent StepStateChanged;

        /// <summary>
        /// Gets the successor state machines.
        /// </summary>
        /// <value>The successor state machines.</value>
        public List<TransitionStateMachine> SuccessorStateMachines
        {
            get
            {
                return _successorStateMachines;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this step state machine is in a final state - Aborted, Stopped or Complete.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is in final state; otherwise, <c>false</c>.
        /// </value>
        public bool IsInFinalState(PfcExecutionContext pfcec)
        {
            SsmData ssmData = GetSsmData(pfcec);
            return ssmData.State == StepState.Complete || ssmData.State == StepState.Aborted || ssmData.State == StepState.Stopped;
        }

        /// <summary>
        /// Gets a value indicating whether this step state machine is in a quiescent state - Held or Paused.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is in quiescent state; otherwise, <c>false</c>.
        /// </value>
        public bool IsInQuiescentState(PfcExecutionContext pfcec)
        {
            SsmData ssmData = GetSsmData(pfcec);
            return ssmData.State == StepState.Held || ssmData.State == StepState.Paused;
        }

        /// <summary>
        /// Gets the name of the step that this Step State Machine represents.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get
            {
                return _myStep.Name;
            }
        }

        internal void GetStartPermission(PfcExecutionContext pfcec)
        {
            IDetachableEventController currentEventController = _myStep.Model.Executive.CurrentEventController;
            SsmData ssmData = GetSsmData(pfcec);
            if (!ssmData.State.Equals(StepState.Idle))
            {
                ssmData.QueueIdec.Enqueue(currentEventController);
                if (_diagnostics)
                {
                    Console.WriteLine(_myStep.Model.Executive.Now + " : suspending awaiting start of " + _myStep.Name + " ...");
                }
                currentEventController.Suspend();
                if (_diagnostics)
                {
                    Console.WriteLine(_myStep.Model.Executive.Now + " : resuming the starting of     " + _myStep.Name + " ...");
                }
            }
        }

        /// <summary>
        /// Creates pfc execution contexts, one per action under the step that is currently running. Each
        /// is given an instance count of zero, as a step can run its action only once, currently.
        /// </summary>
        /// <param name="parentContext">The parent context, that of the step that is currently running.</param>
        /// <param name="kids">The procedure function charts that live in the actions under the step that is currently running.</param>
        /// <param name="kidContexts">The pfc execution contexts that will correspond to the running of each of the child PFCs.</param>
        protected virtual void CreateChildContexts(PfcExecutionContext parentContext, out IProcedureFunctionChart[] kids, out PfcExecutionContext[] kidContexts)
        {
            int kidCount = MyStep.Actions.Count;
            kids = new ProcedureFunctionChart[kidCount];
            kidContexts = new PfcExecutionContext[kidCount];
            int i = 0;
            foreach (KeyValuePair<string, IProcedureFunctionChart> kvp in MyStep.Actions)
            {
                IProcedureFunctionChart kid = kvp.Value;
                kids[i] = kid;
                Guid kidGuid = GuidOps.XOR(parentContext.Guid, kid.Guid);
                while (parentContext.Contains(kidGuid))
                {
                    kidGuid = GuidOps.Increment(kidGuid);
                }
                kidContexts[i] = new PfcExecutionContext(kid, kvp.Key, null, kidGuid, parentContext);
                kidContexts[i].InstanceCount = 0;
                i++;
            }
        }

        #region Test State Machine Methods
        private void TestStateMachine_DoTransition(StepState fromState, StepState toState, PfcExecutionContext myPfcec)
        {
            switch (fromState)
            {

                case StepState.Idle:
                    switch (toState)
                    {
                        case StepState.Running:
                            break;
                        default:
                            break;
                    }
                    break;
                case StepState.Running:
                    switch (toState)
                    {
                        case StepState.Complete:
                            break;
                        case StepState.Aborting:
                            break;
                        case StepState.Stopping:
                            break;
                        case StepState.Pausing:
                            break;
                        case StepState.Holding:
                            break;
                        default:
                            break;
                    }
                    break;
                case StepState.Complete:
                    switch (toState)
                    {
                        case StepState.Idle:
                            break;
                        default:
                            break;
                    }
                    break;
                case StepState.Aborting:
                    switch (toState)
                    {
                        case StepState.Aborted:
                            break;
                        default:
                            break;
                    }
                    break;
                case StepState.Aborted:
                    switch (toState)
                    {
                        case StepState.Idle:
                            break;
                        default:
                            break;
                    }
                    break;
                case StepState.Stopping:
                    switch (toState)
                    {
                        case StepState.Stopped:
                            break;
                        default:
                            break;
                    }
                    break;
                case StepState.Stopped:
                    switch (toState)
                    {
                        case StepState.Idle:
                            break;
                        default:
                            break;
                    }
                    break;
                case StepState.Pausing:
                    switch (toState)
                    {
                        case StepState.Paused:
                            break;
                        default:
                            break;
                    }
                    break;
                case StepState.Paused:
                    switch (toState)
                    {
                        case StepState.Running:
                            break;
                        default:
                            break;
                    }
                    break;
                case StepState.Holding:
                    switch (toState)
                    {
                        case StepState.Held:
                            break;
                        default:
                            break;
                    }
                    break;
                case StepState.Held:
                    switch (toState)
                    {
                        case StepState.Restarting:
                            break;
                        default:
                            break;
                    }
                    break;
                case StepState.Restarting:
                    switch (toState)
                    {
                        case StepState.Running:
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }
        #endregion

        private void StateChangeCompleted(PfcExecutionContext pfcec)
        {
            if (StepStateChanged != null)
            {
                StepStateChanged(this, pfcec);
            }
            SuccessorStateMachines.ForEach(delegate (TransitionStateMachine tsm)
            {
                tsm.PredecessorStateChange(pfcec);
            });

            SsmData ssmData = GetSsmData(pfcec);
            if ((ssmData.State == StepState.Idle) && (ssmData.QueueIdec.Count > 0))
            {
                ssmData.QueueIdec.Dequeue().Resume();
            }
        }

        private void DoRunning(PfcExecutionContext pfcec)
        {
            if (_diagnostics)
            {
                string msg = "Starting to run {0} action{1} under step {2} with ec {3}.";
                string nKids = "1";
                string plural = "";
                string stepName = _myStep.Name;
                string ecName = pfcec.Name;
                int nActions = (_myStep.Actions?.Count ?? 0) + _myStep.LeafLevelAction.GetInvocationList().Length;
                nKids = nActions.ToString();
                plural = nActions == 1 ? "" : "s";
                if (nActions == 0)
                    msg = "There are no actions to run under step {2} with ec {3}.";
                Console.WriteLine(msg, nKids, plural, stepName, ecName);
            }

            IModel model = _myStep.Model;
            SsmData ssmData = GetSsmData(pfcec);
            Debug.Assert(model.Executive.CurrentEventType == ExecEventType.Detachable);
            if (model != null && model.Executive != null)
            {
                if (_myStep.Actions != null && _myStep.Actions.Count > 0)
                {
                    IProcedureFunctionChart[] kids;
                    PfcExecutionContext[] kidContexts;
                    CreateChildContexts(ssmData.ActiveStepInstanceEc, out kids, out kidContexts);
                    foreach (IProcedureFunctionChart action in _myStep.Actions.Values)
                    {
                        for (int i = 0; i < kidContexts.Length; i++)
                        {
                            model.Executive.RequestEvent(new ExecEventReceiver(kids[i].Run), model.Executive.Now, 0.0, kidContexts[i], ExecEventType.Detachable);
                        }
                    }
                    new PfcStepJoiner(ssmData.ActiveStepInstanceEc, kids).RunAndWait();
                }
                else
                {
                    //PfcExecutionContext iterPfc = CreateIterationContext(pfcec);
                    _myStep.LeafLevelAction(pfcec, this);
                }
            }

            DoTransition(StepState.Complete, pfcec);
        }

        private void DoTransition(StepState toState, PfcExecutionContext myPfcec)
        {
            SsmData ssmData = GetSsmData(myPfcec);
            StepState fromState = ssmData.State;
            if (_transition_Matrix[(int)fromState, (int)toState])
            {
                ssmData.State = toState;

                bool timePeriodContainer = myPfcec.TimePeriod is Scheduling.TimePeriodEnvelope;

                if (!timePeriodContainer)
                {
                    if (fromState == StepState.Running && toState == StepState.Complete)
                    {
                        myPfcec.TimePeriod.EndTime = myPfcec.Model.Executive.Now;
                    }
                }

                // Get permission from Step to run.
                if (fromState == StepState.Idle && toState == StepState.Running)
                {
                    _myStep.GetPermissionToStart(myPfcec, this);
                }

                //Console.WriteLine("{2} from {0} to {1}", fromState, toState, this.Name);
                if (!timePeriodContainer)
                {
                    if (fromState == StepState.Idle && toState == StepState.Running)
                    {
                        myPfcec.TimePeriod.StartTime = myPfcec.Model.Executive.Now;
                    }
                }

                if (fromState == StepState.Complete && toState == StepState.Idle)
                {
                    ssmData.ActiveStepInstanceEc = null;
                }

                StateChangeCompleted(myPfcec);

                if (fromState == StepState.Idle && toState == StepState.Running)
                {
                    DoRunning(myPfcec);
                }

                StepState followOnState = _follow_On_States[(int)toState];
                if (followOnState != toState)
                {
                    DoTransition(followOnState, myPfcec);
                }

            }
            else
            {
                string msg = string.Format("Illegal attempt to transition from {0} to {1} in step state machine for {2}.", fromState, toState, Name);
                throw new ApplicationException(msg);
            }
        }

        /// <summary>
        /// PFCStepJoiner, when RunAndWait is called, halts the step that owns the rootStepPfcec, and waits for completion of
        /// each child PFC (these are to have been actions of the root step) before resuming the parent step.
        /// </summary>
        private class PfcStepJoiner
        {

            #region Private Fields
            private readonly IModel _model;
            private readonly PfcExecutionContext _rootStepEc;
            private readonly TransitionStateMachineEvent _onTransitionStateChanged;
            private IDetachableEventController _idec;
            private readonly List<IProcedureFunctionChart> _pendingActions;
            #endregion Private Fields

            public PfcStepJoiner(PfcExecutionContext rootStepPfcec, IProcedureFunctionChart[] childPfCs)
            {
                Debug.Assert(rootStepPfcec.IsStepCentric);
                _rootStepEc = rootStepPfcec;
                _model = _rootStepEc.Model;
                _idec = null;
                _onTransitionStateChanged = new TransitionStateMachineEvent(OnTransitionStateChanged);
                _pendingActions = new List<IProcedureFunctionChart>(childPfCs);
                _pendingActions.ForEach(delegate (IProcedureFunctionChart kid)
                {
                    kid.GetFinishTransition().MyTransitionStateMachine.TransitionStateChanged += _onTransitionStateChanged;
                });
            }

            public void RunAndWait()
            {
                _idec = _model.Executive.CurrentEventController;
                _idec.Suspend();
            }

            private void OnTransitionStateChanged(TransitionStateMachine tsm, object userData)
            {
                PfcExecutionContext completedStepsParentPfcec = (PfcExecutionContext)userData;
                if (completedStepsParentPfcec.Parent.Payload.Equals(_rootStepEc) && tsm.GetState(completedStepsParentPfcec) == TransitionState.Inactive)
                {
                    tsm.TransitionStateChanged -= _onTransitionStateChanged;
                    _pendingActions.Remove(tsm.MyTransition.Parent);
                    if (_pendingActions.Count == 0)
                    {
                        _idec.Resume();
                    }
                }
            }
        }
    }
}
