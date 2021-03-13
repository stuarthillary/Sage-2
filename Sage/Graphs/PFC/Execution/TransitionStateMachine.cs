/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.SimCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Highpoint.Sage.Graphs.PFC.Execution
{

    public delegate void TransitionStateMachineEvent(TransitionStateMachine tsm, object userData);

    public delegate bool ExecutableCondition(object graphContext, TransitionStateMachine tsm);

    public class TransitionStateMachine
    {
        private class TsmData
        {
            private long _nextExpressionEvaluation;
            private TransitionState _state;
            public TsmData()
            {
                _nextExpressionEvaluation = 0L;
                _state = TransitionState.Inactive;
            }
            public long NextExpressionEvaluation
            {
                get
                {
                    return _nextExpressionEvaluation;
                }
                set
                {
                    _nextExpressionEvaluation = value;
                }
            }
            public TransitionState State
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

        }

        #region Private Fields

        private IPfcTransitionNode _myTransition = null;
        private readonly List<StepStateMachine> _predecessors;
        private readonly List<StepStateMachine> _successors;
        private ExecutableCondition _executableCondition = null;
        private TimeSpan _scanningPeriod = ExecutionEngineConfiguration.DEFAULT_SCANNING_PERIOD;
        private static readonly bool _diagnostics = Diagnostics.DiagnosticAids.Diagnostics("PfcTransitionStateMachine");
        #endregion

        public TransitionStateMachine(IPfcTransitionNode myTransition)
        {
            _myTransition = myTransition;
            _predecessors = new List<StepStateMachine>();
            _successors = new List<StepStateMachine>();
        }

        public TimeSpan ScanningPeriod
        {
            get
            {
                return _scanningPeriod;
            }
            set
            {
                _scanningPeriod = value;
            }
        }

        public TransitionState GetState(PfcExecutionContext pfcec)
        {
            if (pfcec.IsStepCentric)
            {
                pfcec = (PfcExecutionContext)pfcec.Parent;
            }
            return GetTsmData(pfcec).State;
        }

        public IPfcTransitionNode MyTransition
        {
            get
            {
                return _myTransition;
            }
            internal set
            {
                _myTransition = value;
            }
        }

        public List<StepStateMachine> PredecessorStateMachines
        {
            get
            {
                return _predecessors;
            }
        }

        public List<StepStateMachine> SuccessorStateMachines
        {
            get
            {
                return _successors;
            }
        }

        internal void PredecessorStateChange(PfcExecutionContext pfcec)
        {

            if (pfcec.IsStepCentric)
            {
                pfcec = (PfcExecutionContext)pfcec.Parent;
            }
            else
            {
                Debugger.Break(); // Only step-centrics should call this.
            }

            switch (GetState(pfcec))
            {

                case TransitionState.Active:
                    if (AnyPredIsQuiescent(pfcec))
                    {
                        SetState(TransitionState.NotBeingEvaluated, pfcec);
                        HaltConditionScanning(pfcec);
                    }
                    else if (AllPredsAreIdle(pfcec))
                    {
                        SetState(TransitionState.Inactive, pfcec);
                        HaltConditionScanning(pfcec);
                    }

                    break;

                case TransitionState.Inactive:
                    if (AllPredsAreNotIdle(pfcec))
                    {
                        if (NoPredIsQuiescent(pfcec))
                        {
                            SetState(TransitionState.Active, pfcec);
                            StartConditionScanning(pfcec);
                        }
                        else
                        {
                            SetState(TransitionState.NotBeingEvaluated, pfcec);
                            HaltConditionScanning(pfcec);
                        }
                    }
                    break;

                case TransitionState.NotBeingEvaluated:
                    if (NoPredIsQuiescent(pfcec))
                    {
                        SetState(TransitionState.Active, pfcec);
                        StartConditionScanning(pfcec);
                    }
                    break;

                default:
                    break;
            }
        }

        public event TransitionStateMachineEvent TransitionStateChanged;

        private TsmData GetTsmData(PfcExecutionContext parentPfcec)
        {
            Debug.Assert(!parentPfcec.IsStepCentric); // State is stored in the parent of the trans, a PFC.
            if (!parentPfcec.Contains(this))
            {
                parentPfcec.Add(this, new TsmData());
            }
            return (TsmData)parentPfcec[this];
        }

        private void SetState(TransitionState transitionState, PfcExecutionContext parentPfcec)
        {
            if (SuccessorStateMachines.Count == 0 && transitionState == TransitionState.Inactive)
            {
                ((ProcedureFunctionChart)_myTransition.Parent).FirePfcCompleting(parentPfcec);
            }
            Debug.Assert(!parentPfcec.IsStepCentric); // State is stored in the parent of the trans, a PFC.
            TsmData tsmData = GetTsmData(parentPfcec);
            if (tsmData.State != transitionState)
            {
                tsmData.State = transitionState;
                if (TransitionStateChanged != null)
                {
                    TransitionStateChanged(this, parentPfcec);
                }
            }
        }

        #region Condition Scanning
        private void StartConditionScanning(PfcExecutionContext pfcec)
        {
            if (_diagnostics)
            {
                Console.WriteLine("Starting condition-scanning on transition " + _myTransition.Name + " in EC " + pfcec.Name + ".");
            }
            HaltConditionScanning(pfcec);
            IExecutive exec = _myTransition.Model.Executive;
            TsmData tsmData = GetTsmData(pfcec);
            tsmData.NextExpressionEvaluation = exec.RequestEvent(new ExecEventReceiver(EvaluateCondition), exec.Now + _scanningPeriod, 0.0, pfcec, ExecEventType.Synchronous);
        }

        private void HaltConditionScanning(PfcExecutionContext pfcec)
        {
            TsmData tsmData = GetTsmData(pfcec);
            if (tsmData.NextExpressionEvaluation != 0L)
            {
                _myTransition.Model.Executive.UnRequestEvent(tsmData.NextExpressionEvaluation);
                tsmData.NextExpressionEvaluation = 0L;
            }
        }

        private void EvaluateCondition(IExecutive exec, object userData)
        {
            PfcExecutionContext pfcec = (PfcExecutionContext)userData;
            TsmData tsmData = GetTsmData(pfcec);
            tsmData.NextExpressionEvaluation = 0L;
            if (tsmData.State == TransitionState.Active && ExecutableCondition(pfcec, this))
            {
                PredecessorStateMachines.ForEach(delegate (StepStateMachine ssm)
                {
                    if (ssm.GetState(pfcec) != StepState.Complete)
                    {
                        ssm.Stop(pfcec);
                    }
                    ssm.Reset(pfcec);
                });
                // When the last predecessor goes to Idle, I will go to Inactive.
                Debug.Assert(AllPredsAreIdle(pfcec));
                Debug.Assert(tsmData.State == TransitionState.Inactive);
                if (_diagnostics)
                {
                    Console.WriteLine("Done condition-scanning on transition " + _myTransition.Name + " in EC " + pfcec.Name + ".");
                }
                SuccessorStateMachines.ForEach(delegate (StepStateMachine ssm)
                {
                    RunSuccessor(ssm, pfcec);
                });
            }
            else
            {
                // Either I'm NotBeingEvaluated, or the evaluation came out false.
                // NOTE: Must halt event stream when "NotBeingEvaluated".
                tsmData.NextExpressionEvaluation = exec.RequestEvent(new ExecEventReceiver(EvaluateCondition), exec.Now + _scanningPeriod, 0.0, pfcec, ExecEventType.Synchronous);
            }
        }

        private void RunSuccessor(StepStateMachine ssm, IDictionary graphContext)
        {
            _myTransition.Model.Executive.RequestEvent(new ExecEventReceiver(_RunSuccessor), _myTransition.Model.Executive.Now, 0.0, new object[] { ssm, graphContext }, ExecEventType.Detachable);
        }

        private void _RunSuccessor(IExecutive exec, object userData)
        {
            StepStateMachine ssm = ((object[])userData)[0] as StepStateMachine;
            PfcExecutionContext parentPfcec = ((object[])userData)[1] as PfcExecutionContext;

            Debug.Assert(!parentPfcec.IsStepCentric);
            ssm.Start(parentPfcec);// Must run ones' successor in the context of out parent, not the predecessor step.
        }

        /// <summary>
        /// Gets or sets the executable condition, the executable condition that this transition will evaluate.
        /// </summary>
        /// <value>The executable condition.</value>
        public ExecutableCondition ExecutableCondition
        {
            get
            {
                if (_executableCondition != null)
                {
                    return _executableCondition;
                }
                else
                {
                    return _myTransition.ExpressionExecutable;
                }
            }
            set
            {
                _executableCondition = value;
            }
        }
        #endregion

        #region PredecessorAssessment Methods

        private bool AnyPredIsQuiescent(PfcExecutionContext parentPfcec)
        {
            bool anyPredIsQuiescent = false;
            PredecessorStateMachines.ForEach(delegate (StepStateMachine ssm)
            {
                anyPredIsQuiescent |= ssm.IsInQuiescentState(parentPfcec);
            });
            return anyPredIsQuiescent;
        }

        private bool NoPredIsQuiescent(PfcExecutionContext parentPfcec)
        {
            return PredecessorStateMachines.TrueForAll(delegate (StepStateMachine ssm)
            {
                return !ssm.IsInQuiescentState(parentPfcec);
            });
        }

        private bool AllPredsAreIdle(PfcExecutionContext parentPfcec)
        {
            return PredecessorStateMachines.TrueForAll(delegate (StepStateMachine ssm)
            {
                return ssm.GetState(parentPfcec) == StepState.Idle;
            });
        }

        private bool AllPredsAreNotIdle(PfcExecutionContext parentPfcec)
        {
            return PredecessorStateMachines.TrueForAll(delegate (StepStateMachine ssm)
            {
                return ssm.GetState(parentPfcec) != StepState.Idle;
            });
        }

        #endregion

        /// <summary>
        /// Gets the name of the transition this state machine will execute.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get
            {
                return _myTransition.Name;
            }
        }

    }
}
