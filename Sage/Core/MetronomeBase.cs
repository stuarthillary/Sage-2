/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Diagnostics;

namespace Highpoint.Sage.SimCore
{

    /// <summary>
    /// Abstract base class from which concrete Metronomes derive. Provides basic services
    /// and event handlers for a Metronome, which is an object that uses a model's executive
    /// to create a series of 'tick' events with a consistent period - Simulation Objects
    /// that are written to expect a uniform discrete time notification can use a metronome
    /// to achieve that. Multiple metronomes may be defined within a model, with different
    /// periods, start times and/or finish times.
    /// </summary>
    public abstract class MetronomeBase
    {
        private readonly ExecEventReceiver _execEvent;
        private readonly DateTime _startAt;
        private readonly DateTime _finishAfter;
        private readonly TimeSpan _period;
        private readonly IExecutive _executive;
        private readonly bool _autoFinish = true;
        private readonly bool _autoStart = true;
        private bool _abortRequested = false;

        /// <summary>
        /// Abstract base class constructor for the Metronome_Base class. Assumes both autostart
        /// and autofinish for this metronome.
        /// </summary>
        /// <param name="exec">The executive that will be serving the events.</param>
        /// <param name="period">The periodicity of the event train.</param>
        protected MetronomeBase(IExecutive exec, TimeSpan period)
        {
            _startAt = DateTime.MinValue;
            _finishAfter = DateTime.MaxValue;
            _period = period;
            _executive = exec;
            _autoFinish = true;
            _autoStart = true;
            _execEvent = OnExecEvent;
            _executive.EventAboutToFire += executive_EventAboutToFire;
            TotalTicksExpected = int.MaxValue;

        }

        /// <summary>
        /// Abstract base class constructor for the Metronome_Base class.
        /// </summary>
        /// <param name="exec">The executive that will be serving the events.</param>
        /// <param name="startAt">The start time for the event train.</param>
        /// <param name="finishAfter">The end time for the event train.</param>
        /// <param name="period">The periodicity of the event train.</param>
        protected MetronomeBase(IExecutive exec, DateTime startAt, DateTime finishAfter, TimeSpan period)
        {
            _startAt = startAt;
            _finishAfter = finishAfter;
            _period = period;
            _executive = exec;
            _autoFinish = false;
            _autoStart = false;
            _execEvent = OnExecEvent;
            TotalTicksExpected = (int)((_finishAfter.Ticks - _startAt.Ticks) / _period.Ticks);
            _executive.ExecutiveStarted += executive_ExecutiveStarted;
        }

        public int TotalTicksExpected
        {
            get; private set;
        }

        private void executive_EventAboutToFire(long key, ExecEventReceiver eer, double priority, DateTime when, object userData, ExecEventType eventType)
        {
            _executive.RequestEvent(_execEvent, when, priority + double.Epsilon, null);
            _executive.EventAboutToFire -= executive_EventAboutToFire;
        }

        private void executive_ExecutiveStarted(IExecutive exec)
        {
            TickIndex = 0;
            Debug.Assert(_executive.Now <= _startAt, "Start Time Error"
                , "A metronome was told to start at " + _startAt + ", but the executive started at time " + exec.Now + ".");
            _executive.RequestEvent(_execEvent, _startAt, 0.0, null);

        }

        private void OnExecEvent(IExecutive exec, object userData)
        {
            if (!_abortRequested)
            {
                Debug.Assert(exec == _executive, "Executive Mismatch"
                    , "A metronome was called by an executive that is not the one it was initially created for.");
                FireEvents(exec, userData);
                if (!_abortRequested)
                {
                    TickIndex++;
                    DateTime nextEventTime = exec.Now + _period;
                    if (nextEventTime <= _finishAfter)
                    {
                        if (_autoFinish)
                        {
                            exec.RequestDaemonEvent(_execEvent, nextEventTime, 0.0, null);
                        }
                        else
                        {
                            exec.RequestEvent(_execEvent, nextEventTime, 0.0, null);
                        }
                    }
                }
            }
        }

        public DateTime StartAt => _startAt;
        public DateTime FinishAt => _finishAfter;
        public TimeSpan Period => _period;
        public bool AutoStart => _autoStart;
        public bool AutoFinish => _autoFinish;
        public int TickIndex
        {
            get; private set;
        }
        public IExecutive Executive => _executive;
        protected abstract void FireEvents(IExecutive exec, object userData);
        public void Abort()
        {
            _abortRequested = true;
        }
    }
}
