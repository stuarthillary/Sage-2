/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Utility;
using System;

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// Simple Metronome class is an object that uses a model's executive
    /// to create a series of 'tick' events with a consistent period - Simulation Objects
    /// that are written to expect a uniform discrete time notification can use a metronome
    /// to achieve that. Multiple metronomes may be defined within a model, with different
    /// periods, start times and/or finish times.
    /// </summary>
    public class SimpleMetronome : MetronomeBase
    {

        private static readonly HashtableOfLists _channels = new HashtableOfLists();

        /// <summary>
        /// Creates a metronome with the specified parameters. It uses a static factory method 
        /// because if there is already an existing metronome with the same parameters, that 
        /// metronome is returned, rather than creating another one - after all, the whole 
        /// point is to avoid a large number of tick events on the executive's event list.
        /// </summary>
        /// <param name="exec">The executive that will issue the ticks.</param>
        /// <param name="startAt">The time at which the metronome's ticks are to start.</param>
        /// <param name="finishAfter">The time at which the metronome's ticks are to stop.</param>
        /// <param name="period">The period of the ticking.</param>
        /// <returns>A metronome that meets the criteria.</returns>
        public static SimpleMetronome CreateMetronome(IExecutive exec, DateTime startAt, DateTime finishAfter, TimeSpan period)
        {
            SimpleMetronome retval = null;
            foreach (SimpleMetronome ms in _channels)
            {
                if (ms.Executive.Equals(exec) && ms.StartAt.Equals(startAt) && ms.FinishAt.Equals(finishAfter) && ms.Period.Equals(period))
                {
                    retval = ms;
                    break;
                }
            }
            if (retval == null)
            {
                retval = new SimpleMetronome(exec, startAt, finishAfter, period);
                _channels.Add(exec, retval);
                exec.ExecutiveFinished += exec_ExecutiveFinished;
            }
            return retval;
        }

        /// <summary>
        /// Creates a metronome with the specified parameters. It uses a static factory method 
        /// because if there is already an existing metronome with the same parameters, that 
        /// metronome is returned, rather than creating another one - after all, the whole 
        /// point is to avoid a large number of tick events on the executive's event list.
        /// </summary>
        /// <param name="exec">The executive that will issue the ticks.</param>
        /// <param name="period">The period of the ticking.</param>
        /// <returns>SimpleMetronome.</returns>
        public static SimpleMetronome CreateMetronome(IExecutive exec, TimeSpan period)
        {
            SimpleMetronome retval = null;

            foreach (SimpleMetronome ms in _channels)
            {
                if (ms.Period.Equals(period) && ms.StartAt == DateTime.MinValue && ms.FinishAt == DateTime.MaxValue)
                {
                    retval = ms;
                    break;
                }
            }
            if (retval == null)
            {
                retval = new SimpleMetronome(exec, period);
                _channels.Add(exec, retval);
            }
            return retval;
        }

        /// <summary>
        /// Constructor for the SimpleMetronome class.
        /// </summary>
        /// <param name="exec">The executive that will be serving the events.</param>
        /// <param name="startAt">The start time for the event train.</param>
        /// <param name="finishAfter">The end time for the event train.</param>
        /// <param name="period">The periodicity of the event train.</param>
        private SimpleMetronome(IExecutive exec, DateTime startAt, DateTime finishAfter, TimeSpan period)
            : base(exec, startAt, finishAfter, period) { }
        /// <summary>
        /// Constructor for the SimpleMetronome class. Assumes auto-start, and auto-finish.
        /// </summary>
        /// <param name="exec">The executive that will be serving the events.</param>
        /// <param name="period">The periodicity of the event train.</param>
        private SimpleMetronome(IExecutive exec, TimeSpan period)
            : base(exec, period) { }
        /// <summary>
        /// The tick event that is fired by this metronome. Simulation objects expecting to
        /// receive periodic notifications will receive them from this event. Note that there is
        /// no inferred sequence to these notifications. If a dependency order is required, then
        /// the Metronome_Dependencies class should be used.
        /// </summary>
        public event ExecEventReceiver TickEvent;

        protected override void FireEvents(IExecutive exec, object userData)
        {
            TickEvent?.Invoke(exec, userData);
        }

        private static void exec_ExecutiveFinished(IExecutive exec)
        {
            _channels.Remove(exec);
        }
    }
}
