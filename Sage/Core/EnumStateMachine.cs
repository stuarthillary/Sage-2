/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections.Generic;

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// The EnumStateMachine represents a simple state machine whose states are the values of the enum,
    /// whose transitions are all allowable, and which tracks the amount of run-time spent in each state.
    /// A developer can wrap this class in another to limit the permissible transitions, add handlers,
    /// etc.
    /// </summary>
    /// <typeparam name="TEnum">The type of the t enum.</typeparam>
    public class EnumStateMachine<TEnum> where TEnum : struct
    {
        public delegate void StateMachineEvent(TEnum from, TEnum to);
        private readonly IExecutive _exec;
        private readonly TEnum _initialState;
        private TEnum _currentState;
        private DateTime _lastStateChange;
        private Dictionary<TEnum, TimeSpan> _stateTimes;
        private readonly bool _trackTransitions;
        private List<TransitionRecord> _transitions;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumStateMachine{TEnum}"/> class.
        /// </summary>
        /// <param name="exec">The executive whose time sequence will be followed in collecting statistics for this machine.</param>
        /// <param name="initialState">The state in which the machine initially resides.</param>
        /// <param name="trackTransitions">if set to <c>true</c>, the state machine will track the from, to, and time of each transition.</param>
        public EnumStateMachine(IExecutive exec, TEnum initialState, bool trackTransitions = true)
        {
            _exec = exec;
            _currentState = _initialState = initialState;
            if (exec.State == ExecState.Running || exec.State == ExecState.Paused)
            {
                // Subject state machine was created while the model was running.
                ResetStatistics(_exec.Now);
            }
            else
            {
                // Subject state machine was created before the model started running.
                _exec.ExecutiveStarted += executive => ResetStatistics(_exec.Now);
            }
            _exec.ExecutiveFinished += executive => UpdateStateTimes();
            _trackTransitions = trackTransitions;
            _stateTimes = new Dictionary<TEnum, TimeSpan>();
            Array ia = Enum.GetValues(typeof(TEnum));
            foreach (TEnum val in ia)
            {
                _stateTimes.Add(val, TimeSpan.Zero);
            }
            if (_trackTransitions)
                _transitions = new List<TransitionRecord>();
        }

        /// <summary>
        /// The amount of time the machine spent in the specified state.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>TimeSpan.</returns>
        public TimeSpan TimeSpentInState(TEnum state)
        {
            return _stateTimes[state];
        }

        /// <summary>
        /// Resets the statistics kept on this machine's last run.
        /// </summary>
        /// <param name="when">The when.</param>
        private void ResetStatistics(DateTime when)
        {
            if (_trackTransitions)
                _transitions = new List<TransitionRecord>();
            _stateTimes = new Dictionary<TEnum, TimeSpan>();
            Array ia = Enum.GetValues(typeof(TEnum));
            foreach (TEnum val in ia)
            {
                _stateTimes.Add(val, TimeSpan.Zero);
            }
            _lastStateChange = when;

        }

        /// <summary>
        /// Transitions the state of this machine to the specified state.
        /// </summary>
        /// <param name="toState">The requested destination state.</param>
        /// <returns><c>true</c> if the transition was successful, <c>false</c> otherwise.</returns>
        public virtual bool ToState(TEnum toState)
        { // TODO: Make this protected, and update all uses to employ implementation-specific transition methods.
            if (!toState.Equals(_currentState))
            {
                if (_trackTransitions)
                    _transitions.Add(new TransitionRecord { From = _currentState, To = toState, When = _exec.Now });
                UpdateStateTimes();
                _lastStateChange = _exec.Now;
                _currentState = toState;
            }
            return true; // Eventually return false if the transition was disallowed.
        }

        private void UpdateStateTimes()
        {
            TimeSpan ts = _exec.Now - _lastStateChange;
            _stateTimes[_currentState] += ts;
        }

        /// <summary>
        /// Gets the current state of the machine.
        /// </summary>
        /// <value>The state of the current.</value>
        public TEnum CurrentState
        {
            get
            {
                return _currentState;
            }
        }

        /// <summary>
        /// Gets the state times in a dictionary of DateTimes, keyed on the enum value that represents the state.
        /// </summary>
        /// <value>The state times.</value>
        public IReadOnlyDictionary<TEnum, TimeSpan> StateTimes
        {
            get
            {
                return _stateTimes;
            }
        }

        /// <summary>
        /// Gets the list of transitions experienced in the last run of the executive.
        /// </summary>
        /// <value>The transitions.</value>
        public IReadOnlyList<TransitionRecord> Transitions
        {
            get
            {
                return _transitions;
            }
        }

        /// <summary>
        /// Records data on Transitions
        /// </summary>
        public struct TransitionRecord
        {
            /// <summary>
            /// The state from which the transition occurred.
            /// </summary>
            /// <value>From.</value>
            public TEnum From
            {
                get; set;
            }
            /// <summary>
            /// The state to which the transition occurred.
            /// </summary>
            /// <value>To.</value>
            public TEnum To
            {
                get; set;
            }
            /// <summary>
            /// When the transition occurred.
            /// </summary>
            /// <value>The when.</value>
            public DateTime When
            {
                get; set;
            }

        }
    }

}
