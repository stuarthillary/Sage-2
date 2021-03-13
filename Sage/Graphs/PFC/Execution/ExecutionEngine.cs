/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.SimCore;
using System;
using System.Collections;
using System.Collections.Generic;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Graphs.PFC.Execution
{

    internal class ExecutionEngine
    {

        #region Private Fields
        private StepStateMachine _startStep;
        private IModel _model;
        private Dictionary<IPfcStepNode, StepStateMachine> _stepStateMachines;
        private Dictionary<IPfcTransitionNode, TransitionStateMachine> _transitionStateMachines;
        private ExecutionEngineConfiguration _executionEngineConfiguration;
        #endregion Private Fields

        public ExecutionEngine(IProcedureFunctionChart pfc) : this(pfc, new ExecutionEngineConfiguration()) { }

        public ExecutionEngine(IProcedureFunctionChart pfc, ExecutionEngineConfiguration eec)
        {
            _executionEngineConfiguration = eec;
            _model = pfc.Model;
            _stepStateMachines = new Dictionary<IPfcStepNode, StepStateMachine>();
            _transitionStateMachines = new Dictionary<IPfcTransitionNode, TransitionStateMachine>();

            foreach (IPfcStepNode pfcStepNode in pfc.Steps)
            {
                StepStateMachine ssm = new StepStateMachine(pfcStepNode);
                ssm.StructureLocked = _executionEngineConfiguration.StructureLockedDuringRun;
                _stepStateMachines.Add(pfcStepNode, ssm);
                ssm.MyStep = pfcStepNode;
                ((PfcStep)pfcStepNode).MyStepStateMachine = ssm;
            }

            foreach (IPfcTransitionNode pfcTransNode in pfc.Transitions)
            {
                TransitionStateMachine tsm = new TransitionStateMachine(pfcTransNode);
                tsm.ScanningPeriod = _executionEngineConfiguration.ScanningPeriod;
                _transitionStateMachines.Add(pfcTransNode, tsm);
                tsm.MyTransition = pfcTransNode;
                ((PfcTransition)pfcTransNode).MyTransitionStateMachine = tsm;
            }

            StepStateMachineEvent ssme = new StepStateMachineEvent(anSSM_StepStateChanged);
            foreach (IPfcStepNode step in pfc.Steps)
            {
                step.MyStepStateMachine.StepStateChanged += ssme;
                foreach (IPfcTransitionNode transNode in step.SuccessorNodes)
                {
                    step.MyStepStateMachine.SuccessorStateMachines.Add(transNode.MyTransitionStateMachine);
                }
                if (step.MyStepStateMachine.SuccessorStateMachines.Count == 0)
                {
                    string message =
                        $"Step {step.Name} in PFC {step.Parent.Name} has no successor transition. A PFC must end with a termination transition. (Did you acquire an Execution Engine while the Pfc was still under construction?)";
                    throw new ApplicationException(message);
                }
            }

            TransitionStateMachineEvent tsme = new TransitionStateMachineEvent(aTSM_TransitionStateChanged);
            foreach (IPfcTransitionNode trans in pfc.Transitions)
            {
                TransitionStateMachine thisTsm = _transitionStateMachines[trans];
                thisTsm.TransitionStateChanged += tsme;
                foreach (IPfcStepNode stepNode in trans.SuccessorNodes)
                {
                    thisTsm.SuccessorStateMachines.Add(_stepStateMachines[stepNode]);
                }
                foreach (IPfcStepNode stepNode in trans.PredecessorNodes)
                {
                    thisTsm.PredecessorStateMachines.Add(_stepStateMachines[stepNode]);
                }
            }

            List<IPfcStepNode> startSteps = pfc.GetStartSteps();
            _Debug.Assert(startSteps.Count == 1);
            _startStep = _stepStateMachines[startSteps[0]];
        }

        void aTSM_TransitionStateChanged(TransitionStateMachine tsm, object userData)
        {
            TransitionStateChanged?.Invoke(tsm, userData);
        }

        void anSSM_StepStateChanged(StepStateMachine ssm, object userData)
        {
            StepStateChanged?.Invoke(ssm, userData);
        }

        /// <summary>
        /// Runs this execution engine's PFC. If this is not called by a detachable event, it calls back for a new
        /// execEvent, on a detachable event controller.
        /// </summary>
        /// <param name="exec">The exec.</param>
        /// <param name="userData">The user data.</param>
        public void Run(IExecutive exec, object userData)
        {

            if (exec.CurrentEventType != ExecEventType.Detachable)
            {
                _model.Executive.RequestEvent(
                    delegate (IExecutive exec1, object userData1)
                    {
                        Run(exec, (IDictionary)userData1);
                    }, exec.Now, exec.CurrentPriorityLevel, userData, ExecEventType.Detachable);
            }
            else
            {
                // We already got this permission as a part of the permission to start the PFC.
                //m_startStep.GetStartPermission((IDictionary)userData);
                _startStep.Start((PfcExecutionContext)userData);
            }
        }

        public StepStateMachine StateMachineForStep(IPfcStepNode step)
        {
            return _stepStateMachines[step];
        }

        public TransitionStateMachine StateMachineForTransition(IPfcTransitionNode trans)
        {
            return _transitionStateMachines[trans];
        }

        public event StepStateMachineEvent StepStateChanged;
        public event TransitionStateMachineEvent TransitionStateChanged;

    }
}
