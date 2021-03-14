/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using System;

namespace Highpoint.Sage.ItemBased
{

    /// <summary>
    /// A class that generates a <see cref="Highpoint.Sage.ItemBased.PulseEvent"/> at a specified periodicity.
    /// </summary>
    public class Ticker : IPulseSource
    {

        #region Private Fields

        private readonly IPeriodicity _periodicity;
        private readonly IModel _model;
        private bool _running = false;
        private readonly bool _autoStart;
        private readonly long _nPulses;
        private long _nPulsesRemaining;
        private readonly ExecEventReceiver _execEventReceiver;

        #endregion 

        /// <summary>
        /// Creates a new instance of the <see cref="T:Ticker"/> class.
        /// </summary>
        /// <param name="model">The model in which this object runs.</param>
        /// <param name="periodicity">The periodicity of the ticker.</param>
        /// <param name="autoStart">if set to <c>true</c> the ticker will start automatically, immediately on model start, and cycle indefinitely.</param>
		public Ticker(IModel model, IPeriodicity periodicity, bool autoStart) : this(model, periodicity, autoStart, long.MaxValue) { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:Ticker"/> class.
        /// </summary>
        /// <param name="model">The model in which this object runs.</param>
        /// <param name="periodicity">The periodicity of the ticker.</param>
        /// <param name="autoStart">if set to <c>true</c> the ticker will start automatically, immediately on model start, and cycle indefinitely.</param>
        /// <param name="nPulses">The number of pulses to be served.</param>
		public Ticker(IModel model, IPeriodicity periodicity, bool autoStart, long nPulses)
        {
            _periodicity = periodicity;
            _model = model;
            _autoStart = autoStart;
            _nPulses = nPulses;
            _execEventReceiver = new ExecEventReceiver(OnExecEvent);
            if (autoStart)
            {
                _model.Starting += new ModelEvent(OnModelStarting);
            }
            _model.Stopping += new ModelEvent(OnModelStopping);
        }

        private void OnModelStarting(IModel model)
        {
            if (_autoStart)
                Start();
        }
        private void OnModelStopping(IModel model)
        {
            Stop();
        }
        /// <summary>
        /// Starts this instance.
        /// </summary>
		public void Start()
        {
            _nPulsesRemaining = _nPulses;
            _running = true;
            ScheduleNextEvent();
        }
        /// <summary>
        /// Stops this instance.
        /// </summary>
		public void Stop()
        {
            _running = false;
        }                                   // TODO: Need an IDateTimeDistribution & an ITimeSpanDistribution.

        private void ScheduleNextEvent()
        {
            if (!_running)
                return;
            if (_nPulsesRemaining == 0)
                return;
            TimeSpan waitDuration = _periodicity.GetNext();
            _model.Executive.RequestEvent(_execEventReceiver, _model.Executive.Now + waitDuration, 0.0, null);
            _nPulsesRemaining--;
        }
        private void OnExecEvent(IExecutive exec, object userData)
        {
            //Console.WriteLine(exec.Now + " : firing ticker.");
            if (PulseEvent != null)
                PulseEvent();
            if (_running)
                ScheduleNextEvent();
        }

        /// <summary>
        /// Fired when this Ticker pulses.
        /// </summary>
 		public event PulseEvent PulseEvent;
    }
}