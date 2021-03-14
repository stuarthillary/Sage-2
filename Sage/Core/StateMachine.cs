/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using System.Diagnostics;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// This delegate is implemented by a method that is intended to perform part
    /// of the preparation for a transition. It accepts the model, and returns a
    /// reason for failing the transition, or null if the implementer of the 
    /// delegate condones the completion of the transition.
    /// </summary>
    public delegate ITransitionFailureReason PrepareTransitionEvent(IModel model, object userData);
    /// <summary>
    /// This delegate is implemented by a method that is intended to be notified
    /// of the successful attempt to perform a transition, and to take part in
    /// the commitment of that transition attempt. 
    /// </summary>
    public delegate void CommitTransitionEvent(IModel model, object userData);
    /// <summary>
    /// This delegate is implemented by a method that is intended to be notified
    /// of the unsuccessful attempt to perform a transition, and to take part in
    /// the rollback of that transition attempt. 
    /// </summary>
    public delegate void RollbackTransitionEvent(IModel model, object userData, IList reasons);

    /// <summary>
    /// Implemented by a method that is to be called once the state machine
    /// completes transition to a specified state.
    /// </summary>
    public delegate void StateMethod(IModel model, object userData);

    /// <summary>
    /// A table-driven, two-phase-transaction state machine. The user configures
    /// the state machine with a number of states, and the state machine creates
    /// handlers for transition out of and into each state, as well as handlers
    /// for transitions between two specific states, and one universal transition
    /// handler. Each handler provides events that are fired when the state machine
    /// attempts and then either completes or rolls back a transition.  When a
    /// transition is requested, the state machine collects all of the outbound 
    /// transition handlers from the current state, all of the handlers into the 
    /// destination state, all handlers specified for both the current and destination
    /// states, and the universal handler. These handlers' 'Prepare' events are called
    /// in the order implied by their sequence numbers (if no sequence number was
    /// specified, it is assumed to be 0.0.) If all 'Prepare' handlers' event targets
    /// are called with none returning a TransitionFailureReason, then the State
    /// Machine calls all of the Commit events. If there was at least one
    /// TransitionFailureReason, then the 'Rollback' event handlers are called.
    /// </summary>
    public class StateMachine
    {
        private static readonly bool _diagnostics = Highpoint.Sage.Diagnostics.DiagnosticAids.Diagnostics("StateMachine");

        /// <summary>
        /// Generic states are states that all state machines should support, and in declaring their states, set equality from
        /// appropriate states to these states. This will support interoperability of many libraries into state machines
        /// declared differently for different solutions, but with some of the same states defined in their lifecycles.
        /// </summary>
        public enum GenericStates : int
        {
            /// <summary>
            /// The model is idle. It has been built, perhaps not completely, but has not gone through any validation. 
            /// </summary>
            Idle = 0,
            /// <summary>
            /// The model is structurally valid.
            /// </summary>
            Validated = 1,
            /// <summary>
            /// The model has been properly initialized, and is ready to be run.
            /// </summary>
            Initialized = 2,
            /// <summary>
            /// The model is currently running.
            /// </summary>
            Running = 3,
            /// <summary>
            /// The model has completed running. The executive will read the last event time, or DateTime.MaxValue. Post-run data
            /// may be available, and a call to Reset() is probably necessary to run it again.
            /// </summary>
            Finished = 4
        }

        #region Private Fields
        private readonly ITransitionHandler[,] _2DTransitions;
        private readonly ITransitionHandler[] _transitionsFrom;
        private readonly ITransitionHandler[] _transitionsTo;
        private readonly ITransitionHandler _universalTransition;
        private int _currentState;
        private int _nextState;
        private bool _transitionInProgress = false;
        private int _numStates;
        private IModel _model;
        private readonly Array _enumValues;
        private readonly StateMethod[] _stateMethods;
        private readonly Enum[] _followOnStates;
        private Hashtable _stateTranslationTable;
        private Enum[] _equivalentStates;

        private bool _stateMachineStructureLocked = false;
        private MergedTransitionHandler[][] _mergedTransitionHandlers;

        #endregion

        /// <summary>
        /// Creates a state machine that does not reference a Model. Many of the event
        /// delegates send a model reference with the notification. If the recipients
        /// all either (a) don't need this reference, (b) have it from elsewhere, or
        /// (c) the entity creating this state machine will set the Model later, then
        /// this constructor may be used.
        /// </summary>
        /// <param name="transitionMatrix">A matrix of booleans. 'From' states are the 
        /// row indices, and 'To' states are the column indices. The contents of a given
        /// cell in the matrix indicates whether that transition is permissible.</param>
        /// <param name="followOnStates">An array of enumerations of states, indicating
        /// which transition should occur automatically, if any, after transition into
        /// a given state has completed successfully.</param>
        /// <param name="initialState">Specifies the state in the state machine that is
        /// to be the initial state.</param>
        public StateMachine(bool[,] transitionMatrix, Enum[] followOnStates, Enum initialState)
            : this(null, transitionMatrix, followOnStates, initialState) { }

        /// <summary>
        /// Creates a state machine that references a Model. Many of the event
        /// delegates send a model reference with the notification.
        /// </summary>
        /// <param name="model">The model to which this State Machine belongs.</param>
        /// <param name="transitionMatrix">A matrix of booleans. 'From' states are the 
        /// row indices, and 'To' states are the column indices. The contents of a given
        /// cell in the matrix indicates whether that transition is permissible.</param>
        /// <param name="followOnStates">An array of enumerations of states, indicating
        /// which transition should occur automatically, if any, after transition into
        /// a given state has completed successfully.</param>
        /// <param name="initialState">Specifies the state in the state machine that is
        /// to be the initial state.</param>
        public StateMachine(IModel model, bool[,] transitionMatrix, Enum[] followOnStates, Enum initialState)
        {
            _model = model;
            _enumValues = Enum.GetValues(initialState.GetType());
            InitializeStateTranslationTable(initialState);
            if (transitionMatrix.GetLength(0) != transitionMatrix.GetLength(1))
            {
                throw new ApplicationException("Transition matrix must be square.");
            }
            _currentState = GetStateNumber(initialState);
            _2DTransitions = new ITransitionHandler[_numStates, _numStates];
            _transitionsFrom = new ITransitionHandler[_numStates];
            _transitionsTo = new ITransitionHandler[_numStates];
            _stateMethods = new StateMethod[_numStates];

            _followOnStates = followOnStates;

            for (int i = 0; i < _numStates; i++)
            {
                _transitionsFrom[i] = new TransitionHandler();
                _transitionsTo[i] = new TransitionHandler();
                for (int j = 0; j < _numStates; j++)
                {
                    if (transitionMatrix[i, j])
                    {
                        _2DTransitions[i, j] = new TransitionHandler();
                    }
                    else
                    {
                        _2DTransitions[i, j] = new InvalidTransitionHandler();
                    }
                }
            }

            _universalTransition = new TransitionHandler();
        }

        /// <summary>
        /// Allows the caller to set the model that this State Machine
        /// references. 
        /// </summary>
        /// <param name="model">The model that this state machine references.</param>
        public void SetModel(IModel model)
        {
            _model = model;
        }

        /// <summary>
        /// Provides a reference to the transition handler that helps govern the
        /// transition between two specified states.
        /// </summary>
        /// <param name="from">The 'from' state that will select the transition handler.</param>
        /// <param name="to">The 'to' state that will select the transition handler.</param>
        /// <returns>The transition handler that will govern the transition between two
        /// specified states.</returns>
        public ITransitionHandler TransitionHandler(Enum from, Enum to)
        {
            int iFrom = GetStateNumber(from);
            int iTo = GetStateNumber(to);
            return _2DTransitions[iFrom, iTo];
        }

        /// <summary>
        /// Provides a reference to the transition handler that helps govern all
        /// transitions OUT OF a specified state.
        /// </summary>
        /// <param name="from">The 'from' state that will select the transition handler.</param>
        /// <returns>A reference to the transition handler that helps govern the
        /// transitions OUT OF a specified state.</returns>
        public ITransitionHandler OutboundTransitionHandler(Enum from)
        {
            int iFrom = GetStateNumber(from);
            return _transitionsFrom[iFrom];
        }

        /// <summary>
        /// Provides a reference to the transition handler that helps govern all
        /// transitions INTO a specified state.
        /// </summary>
        /// <param name="to">The 'to' state that will select the transition handler.</param>
        /// <returns>A reference to the transition handler that helps govern the
        /// transitions INTO a specified state.</returns>
        public ITransitionHandler InboundTransitionHandler(Enum to)
        {
            int iTo = GetStateNumber(to);
            return _transitionsTo[iTo];
        }

        /// <summary>
        /// Provides a reference to the transition handler that helps govern all transitions.
        /// </summary>
        /// <returns>A reference to the transition handler that helps govern all transitions.</returns>
        public ITransitionHandler UniversalTransitionHandler()
        {
            return _universalTransition;
        }

        /// <summary>
        /// The current state of the state machine.
        /// </summary>
        public virtual Enum State
        {
            get
            {
                return (Enum)_enumValues.GetValue(_currentState);
            }
        }

        /// <summary>
        /// Forces the state machine into the new state. No transitions are done, no handlers are called - It's just POOF, new state. Use this with extreme caution!
        /// </summary>
        /// <param name="state">The state into which the state machine is to be placed.</param>
        public void ForceOverrideState(Enum state)
        {
            int tgtStateNum = -1;
            for (int i = 0; i < _enumValues.Length; i++)
            {
                if (_enumValues.GetValue(new int[] { i }).Equals(state))
                {
                    tgtStateNum = i;
                }
            }
            if (tgtStateNum > 0)
            {
                _currentState = tgtStateNum;
            }
            else
            {
                throw new ApplicationException("Unable to force override state machine to state \"" + state + "\" because it is an unknown state.");
            }

        }

        /// <summary>
        /// True if the state machine is in the process of performing a transition.
        /// </summary>
        public bool IsTransitioning
        {
            get
            {
                return _transitionInProgress;
            }
        }

        /// <summary>
        /// Provides the identity of the next state that the State Machine will enter.
        /// </summary>
        public virtual Enum NextState
        {
            get
            {
                // TODO: Is there a case where this would be ambiguous or wrong?
                return (Enum)_enumValues.GetValue(_nextState);
            }
        }

        /// <summary>
        /// Sets the method that will be called when the state machine enters a given state.
        /// </summary>
        /// <param name="newStateMethod">The method to be called.</param>
        /// <param name="forWhichState">The state in which the new method should be called.</param>
        /// <returns>The old state method, or null if there was none assigned.</returns>
        public StateMethod SetStateMethod(StateMethod newStateMethod, Enum forWhichState)
        {
            int iWhichState = GetStateNumber(forWhichState);
            StateMethod oldStateMethod = _stateMethods[iWhichState];
            _stateMethods[iWhichState] = newStateMethod;
            return oldStateMethod;
        }

        /// <summary>
        /// Commands the state machine to attempt transition to the indicated state.
        /// Returns a list of ITransitionFailureReasons. If this list is empty, the
        /// transition was successful.
        /// </summary>
        /// <param name="toWhatState">The desired new state of the State Machine.</param>
        /// <returns>A  list of ITransitionFailureReasons. (Empty if successful.)</returns>
        public IList DoTransition(Enum toWhatState)
        {
            return DoTransition(toWhatState, null);
        }

        public bool StructureLocked
        {
            get
            {
                return _stateMachineStructureLocked;
            }
            set
            {
                _stateMachineStructureLocked = value;
            }
        }

        /// <summary>
        /// Commands the state machine to attempt transition to the indicated state.
        /// Returns a list of ITransitionFailureReasons. If this list is empty, the
        /// transition was successful.
        /// </summary>
        /// <param name="toWhatState">The desired new state of the State Machine.</param>
        /// <param name="userData">The user data to pass into this transition request - it will be sent out of each state change notification and state method.</param>
        /// <returns>
        /// A  list of ITransitionFailureReasons. (Empty if successful.)
        /// </returns>
        public IList DoTransition(Enum toWhatState, object userData)
        {
            Debug.Assert(_model != null, "Did you forget to set the model on the State Machine?");
            try
            {
                _transitionInProgress = true;
                _nextState = GetStateNumber(toWhatState);

                if (_diagnostics)
                {
                    Trace.Write("State machine in model \"" + _model.Name + "\" servicing request to transition ");
                    _Debug.WriteLine("from \"" + State + "\" into \"" + toWhatState + "\".");
                    StackTrace st = new StackTrace();
                    _Debug.WriteLine(st.ToString());
                }

                // TODO: Determine if this is a good policy - it prohibits self-transitions.

                if (_nextState == _currentState)
                {
                    return new ArrayList();
                }

                MergedTransitionHandler mth = null;
                if (_stateMachineStructureLocked)
                {
                    if (_mergedTransitionHandlers == null)
                    {
                        _mergedTransitionHandlers = new MergedTransitionHandler[_numStates][];
                        for (int i = 0; i < _numStates; i++)
                        {
                            _mergedTransitionHandlers[i] = new MergedTransitionHandler[_numStates];
                        }
                    }
                    mth = _mergedTransitionHandlers[_currentState][_nextState];
                }
                if (mth == null)
                {
                    TransitionHandler outbound = (TransitionHandler)_transitionsFrom[_currentState];
                    ITransitionHandler across = (ITransitionHandler)_2DTransitions[_currentState, _nextState];
                    TransitionHandler inbound = (TransitionHandler)_transitionsTo[_nextState];
                    mth = new MergedTransitionHandler(outbound, (TransitionHandler)across, inbound, (TransitionHandler)_universalTransition);
                    if (_stateMachineStructureLocked)
                    {
                        _mergedTransitionHandlers[_currentState][_nextState] = mth;
                    }
                    if (across is InvalidTransitionHandler)
                    {
                        string reason = "Illegal State Transition requested from " + State + " to " + toWhatState;
                        SimpleTransitionFailureReason stfr = new SimpleTransitionFailureReason(reason, this);
                        throw new TransitionFailureException(stfr);
                    }
                }

                if (_diagnostics)
                    _Debug.WriteLine(mth.Dump());
                IList failureReasons = mth.DoPrepare(_model, userData);
                if (failureReasons.Count != 0)
                {
                    mth.DoRollback(_model, userData, failureReasons);
                    return failureReasons;
                }

                mth.DoCommit(_model, userData);

                if (_diagnostics)
                    _Debug.WriteLine("Exiting " + State);
                _currentState = _nextState;
                TransitionCompletedSuccessfully?.Invoke(_model, userData);
                if (_diagnostics)
                    _Debug.WriteLine("Entering " + State);

                _stateMethods[_currentState]?.Invoke(_model, userData);

                // After running the state method, see if there are any follow-on states to be processed.
                if (_followOnStates == null || _followOnStates[_currentState] == null || _followOnStates[_currentState].Equals(State))
                {
                    return null;
                }
                else
                {
                    return DoTransition(_followOnStates[_currentState], userData);
                }


            }
            finally
            {
                if (_diagnostics)
                    _Debug.WriteLine("Coming to a rest in state " + State);
                _transitionInProgress = false;
                _nextState = _currentState;
            }
        }

        /// <summary>
        /// Attempts to run the sequence of transitions. If any fail, the call returns in the state where the failure occurred,
        /// and the reason list contains whatever reasons were given for the failure. This is to be used if the progression is
        /// simple. If checks and responses need to be done, the developer should build a more step-by-step sequencing mechanism.
        /// </summary>
        /// <param name="states">The states.</param>
        /// <returns></returns>
        public IList RunTransitionSequence(params Enum[] states)
        {
            IList retval = null;
            try
            {
                foreach (Enum t in states)
                {
                    retval = DoTransition(t);
                }
            }
            catch (TransitionFailureException tfe)
            {
                if (retval == null)
                {
                    retval = new ArrayList();
                }
                retval.Add(tfe);
            }
            return retval;
        }

        /// <summary>
        /// Determines whether the specified state is quiescent - i.e. has no automatic follow-on state.
        /// </summary>
        /// <param name="whichState">the specified state.</param>
        /// <returns>
        /// 	<c>true</c> if the specified state is quiescent; otherwise, <c>false</c>.
        /// </returns>
        public bool IsStateQuiescent(Enum whichState)
        {
            return _followOnStates[GetStateNumber(whichState)].Equals(whichState);
        }

        /// <summary>
        /// Sets the model-specific enums (states) that equate to each of the StateMachine.GenericState values.
        /// </summary>
        /// <param name="idle">The equivalent state for the generic idle state.</param>
        /// <param name="validated">The equivalent state for the generic validated state.</param>
        /// <param name="initialized">The equivalent state for the generic initialized state.</param>
        /// <param name="running">The equivalent state for the generic running state.</param>
        /// <param name="finished">The equivalent state for the generic finished state.</param>
        public void SetGenericStateEquivalents(Enum idle, Enum validated, Enum initialized, Enum running, Enum finished)
        {
            _equivalentStates = new Enum[] { idle, validated, initialized, running, finished };
        }

        /// <summary>
        /// Gets the application defined Enum (state) that equates to the provided generic state.
        /// </summary>
        /// <param name="equivalentGenericState">The genericState whose equivalent is desired.</param>
        /// <returns>The enum that is equivalent, conceptually, the provided generic state.</returns>
        public Enum GetStateEquivalentTo(GenericStates equivalentGenericState)
        {
            if (_equivalentStates == null)
            {
                throw new ApplicationException("A library is trying to use generic state equivalents, but none have been defined.");
            }
            return _equivalentStates[(int)equivalentGenericState];
        }

        /// <summary>
        /// This event fires when a transition completes successfully, and reaches the intended new state.
        /// </summary>
        public event StateMethod TransitionCompletedSuccessfully;

        public void Detach(object obj)
        {

        }

        /// <summary>
        /// Prepares the state translation table
        /// </summary>
        /// <param name="state">Provides the enum type that contains the various states</param>
        private void InitializeStateTranslationTable(Enum state)
        {
            _stateTranslationTable = new Hashtable();
            Array values = Enum.GetValues(state.GetType());
            _numStates = values.GetLength(0);
            for (int i = 0; i < _numStates; i++)
            {
                _stateTranslationTable.Add(values.GetValue(i), i);
            }

            if (_diagnostics)
            {
                _Debug.WriteLine("Initializing state machine table to the following states");
                foreach (object val in Enum.GetValues(state.GetType()))
                {
                    _Debug.WriteLine(_stateTranslationTable[val] + ", " + val + " : " + val.GetType());
                }
            }
        }

        /// <summary>
        /// Gets the state number for the provided state.
        /// </summary>
        /// <param name="stateEnum">The enum that represents the provided state.</param>
        /// <returns>The state number.</returns>
        internal int GetStateNumber(Enum stateEnum)
        {
            object tmp = _stateTranslationTable[stateEnum];

            if (tmp == null)
            {
                IEnumerator enumerator = _stateTranslationTable.Keys.GetEnumerator();
                enumerator.MoveNext();
                object firstEnum = enumerator.Current;

                string msg = "Cannot translate " + stateEnum + " to an index. It is of type " +
                    stateEnum.GetType() + " and this state machine is running on states of type " +
                    firstEnum.GetType();

                if (stateEnum.GetType() == typeof(DefaultModelStates))
                {
                    msg += " You may have forgotten to provide a new GetStartEnum() method in your derived model.";
                }
                throw new ApplicationException(msg);
            }

            int num = (int)tmp;

            if (num < 0 || num >= _numStates)
            {
                throw new ApplicationException("There is no state with the specified index.");
            }

            return num;
        }

        #region >>> Test Support Method <<<
#if DEBUG // Eventually, 'TESTING', not DEBUG
        /// <summary>
        /// Test method that exposes a state machine's state's number.
        /// </summary>
        /// <param name="stateEnum">The state.</param>
        /// <returns>The number that represents that state.</returns>
        public int _TestGetStateNumber(Enum stateEnum)
        {
            return GetStateNumber(stateEnum);
        }
#endif
        #endregion

    }

}
