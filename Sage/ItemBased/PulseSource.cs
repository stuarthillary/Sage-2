/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility;
using System;

namespace Highpoint.Sage.ItemBased
{

    /// <summary>
    /// An event that a pulse source fires. Anyone wanting to receive a 'Do It!' command implements this delegate.
    /// </summary>
    public delegate void PulseEvent();

    public class PulseSource : IPulseSource, IDisposable
    {
        private readonly IModel _model = null;
        private IPeriodicity _periodicity;
        private readonly bool _initialPulse;
        private readonly ExecEventReceiver _doPulse = null;
        public PulseSource(IModel model, IPeriodicity periodicity, bool initialPulse)
        {
            _model = model;
            _periodicity = periodicity;
            _initialPulse = initialPulse;
            _doPulse = new ExecEventReceiver(DoPulse);
            if (_model.Executive.State.Equals(ExecState.Stopped) || _model.Executive.State.Equals(ExecState.Finished))
            {
                _model.Executive.ExecutiveStarted += new ExecutiveEvent(StartPulsing);
            }
            else
            {
                StartPulsing(model.Executive);
            }
        }

        private void StartPulsing(IExecutive exec)
        {
            if (_initialPulse)
            {
                DoPulse(exec, null);
            }
            else
            {
                DoPause(exec, null);
            }
        }

        private void DoPause(IExecutive exec, object userData)
        {
            DateTime nextPulse = exec.Now + TimeSpanOperations.Max(TimeSpan.Zero, _periodicity.GetNext());
            exec.RequestDaemonEvent(_doPulse, nextPulse, 0.0, null);
        }

        private void DoPulse(IExecutive exec, object userData)
        {
            if (PulseEvent != null)
                PulseEvent();
            DoPause(exec, null);
        }

        public event PulseEvent PulseEvent;

        public IPeriodicity Periodicity
        {
            get
            {
                return _periodicity;
            }
            set
            {
                _periodicity = value;
            }
        }

        public void Dispose()
        {
            _model.Executive.ExecutiveStarted -= new ExecutiveEvent(StartPulsing);
            _model.Executive.UnRequestEvents(this);
        }
    }
}