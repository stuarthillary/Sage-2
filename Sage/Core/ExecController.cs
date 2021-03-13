/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using System.Threading;
using _Debug = System.Diagnostics.Debug;
// ReSharper disable UnusedParameter.Global

namespace Highpoint.Sage.SimCore
{

    /// <summary>
    /// This object will govern the real-time frequency at which the Render event fires, and will also govern
    /// the simulation time that is allowed to pass between Render events. So with a frame rate of 20, there
    /// will be 20 Render events fired per second. With a scale of 2, 10^2, or 100 times that 1/20th of a
    /// second (therefore 2 seconds of simulation time) will be allowed to transpire between render events.
    /// </summary>
    public class ExecController : IDisposable
    {

        #region Private Fields
        private IExecutive _executive;
        private double _logScale;
        private double _linearScale;
        private int _frameRate;
        private readonly object _userData;
        private KickoffMgr _kickoffManager;
        private readonly ExecutiveEvent _doThrottle;
        private TimeSpan _maxNap;
        private DateTime _realWorldStartTime;
        private DateTime _simWorldStartTime;
        private Thread _renderThread;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecController"/> class. The model is used as UserData.
        /// <br></br>Frame rate must be from zero to 25. If zero, no constraint is imposed.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="scale">The (logarithmic) run time scale.</param>
        /// <param name="frameRate">The frame rate in render events per second. If zero, execution is unconstrained.</param>
        public ExecController(IModel model, double scale, int frameRate) : this(model.Executive, scale, frameRate, model) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecController"/> class. The caller may specify userData.
        /// <br></br>Frame rate must be from zero to 25. If zero, no contraint is imposed.
        /// </summary>
        /// <param name="exec">The executive being controlled.</param>
        /// <param name="scale">The (logarithmic) run time scale. If set to double.MinValue, the model runs at full speed.</param>
        /// <param name="frameRate">The frame rate in render events per second. If zero, execution is unconstrained.</param>
        /// <param name="userData">The user data.</param>
        public ExecController(IExecutive exec, double scale, int frameRate, object userData)
        {
            if (!Disable)
            {
                _userData = userData;
                _executive = exec;
                _executive.ExecutiveStarted += executive_ExecutiveStarted;
                Scale = scale;
                FrameRate = frameRate;
                _kickoffManager = new KickoffMgr(this, _executive);
                _doThrottle = ThrottleExecution;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecController"/> class. The caller may specify userData.
        /// <br></br>Frame rate must be from zero to 25. If zero, no contraint is imposed. For this constructor,
        /// the executive will be set while the model sets its ExecController.
        /// </summary>
        /// <param name="scale">The (logarithmic) run time scale. If set to double.MinValue, the model runs at full speed.</param>
        /// <param name="frameRate">The frame rate in render events per second. If zero, execution is unconstrained.</param>
        /// <param name="userData">The user data.</param>
        public ExecController(double scale, int frameRate, object userData = null)
        {
            if (!Disable)
            {
                _userData = userData;
                Scale = scale;
                FrameRate = frameRate;
                _doThrottle = ThrottleExecution;
            }
        }

        /// <summary>
        /// Sets the executive on which this controller will operate. This API should only be called once. The
        /// ExecController cannot be targeted to control a different executive.
        /// </summary>
        /// <param name="exec">The executive on which this controller will operate.</param>
        public void SetExecutive(IExecutive exec)
        {
            if (_executive == exec)
                return;
            if (_executive != null)
                throw new InvalidOperationException("Calling SetExecutive on an ExecController that's already attached to a different executive is illegal.");
            _executive = exec;
            _executive.ExecutiveStarted += executive_ExecutiveStarted;
            _kickoffManager = new KickoffMgr(this, _executive);
        }

        public bool Disable
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the logarithmic scale of run speed to sim speed. For example, for a sim that runs 
        /// 100 x faster than a real-world clock, use a scale of 2.0.
        /// </summary>
        /// <value>The scale.</value>
        public double Scale
        {
            get
            {
                return _logScale;
            }
            set
            {
                if (Math.Abs(value - double.MinValue) < double.Epsilon)
                {
                    _linearScale = 0;
                    _logScale = double.MinValue;
                    FrameRate = 0;
                }
                else
                {
                    _linearScale = Math.Pow(10.0, value);
                    _logScale = value;
                    _maxNap = FrameRate > 0 ? TimeSpan.FromSeconds(1.0 / FrameRate) : TimeSpan.MaxValue;
                }
            }
        }

        /// <summary>
        /// Gets or sets the linear scale of run speed to sim speed. For example, for a sim that runs 
        /// 100 x faster than a real-world clock, the linear scale would be 100.
        /// </summary>
        /// <value>The scale.</value>
        public double LinearScale => _linearScale;

        /// <summary>
        /// Gets or sets the frame rate - an integer that represents the preferred number of rendering callbacks received per second.
        /// </summary>
        /// <value>The frame rate.</value>
        public int FrameRate
        {
            get
            {
                return _frameRate;
            }
            set
            {
                if (value > 25)
                {
                    throw new ArgumentException("Frame rate cannot be more than 25 frames per second.");
                }
                _frameRate = value;
                _maxNap = _frameRate > 0 ? TimeSpan.FromSeconds(1.0 / _frameRate) : TimeSpan.MaxValue;
            }
        }

        /// <summary>
        /// A user-friendly representation of the simulation speed.
        /// </summary>
        /// <value>The rate string.</value>
        public string RateString
        {
            get
            {
                try
                {
                    TimeSpan ts = TimeSpan.FromSeconds(_logScale >= 0 ? Math.Pow(10, _logScale) : Math.Pow(10, -_logScale));
                    double num;
                    string units;
                    if ((num = (ts.TotalDays / 365247.7)) > 1.0)
                    {
                        units = "millennia";
                    }
                    else if ((num = (ts.TotalDays / 36524.77)) > 1.0)
                    {
                        units = "centuries";
                    }
                    else if ((num = (ts.TotalDays / 365.2477)) > 1.0)
                    {
                        units = "years";
                    }
                    else if ((num = ts.TotalDays) > 1.0)
                    {
                        units = "days";
                    }
                    else if ((num = ts.TotalHours) >= 1.0)
                    {
                        units = "hours";
                    }
                    else if ((num = ts.TotalMinutes) >= 1.0)
                    {
                        units = "minutes";
                    }
                    else if ((num = ts.TotalSeconds) >= 1.0)
                    {
                        units = "seconds";
                    }
                    else if ((num = ts.TotalMilliseconds) >= 1.0)
                    {
                        units = "milliseconds";
                    }
                    else
                    {
                        units = "milliseconds";
                    }

                    if (_logScale >= 0)
                    {
                        return string.Format("Up to {0:f2} {1} of simulation time per second of user time.", num, units);
                    }
                    else
                    {
                        return string.Format("Up to {0:f2} {1} of user time per second of simulation time.", num, units);
                    }
                }
                catch
                {
                    return string.Format("{0}.", (_logScale < 0 ? "Controller scale is out of range low" : "Simulation speed is unconstrained"));
                }
            }
        }

        /// <summary>
        /// This event is expected to drive rendering at the prescribed frame rate.
        /// </summary>
        public event ExecEventReceiver Render;

        public void Dispose()
        {
            _renderThread?.Abort();
        }

        internal void Begin(IExecutive iExecutive, object userData)
        {
            if (iExecutive != _executive)
                throw new InvalidOperationException("ExecController is starting within a model whose executive is not the same one to which it was initialized.");
            _executive.ClockAboutToChange -= _doThrottle; // In case we were listening from an earlier run.
            _executive.ClockAboutToChange += _doThrottle;
            if (_renderThread != null && _renderThread.ThreadState == ThreadState.Running)
            {
                _abortRendering = true;
                // ReSharper disable once EmptyEmbeddedStatement
                while (_renderThread.IsAlive)
                    ;
                _abortRendering = false;
            }
            _renderThread = new Thread(RunRendering)
            {
                IsBackground = true,
                Name = "Rendering Thread"
            };
            _realWorldStartTime = DateTime.Now;
            _simWorldStartTime = iExecutive.Now;
            _renderThread.Start();
        }

        private void ThrottleExecution(IExecutive exec)
        {
            if (Math.Abs(_linearScale) > double.Epsilon)
            {
                IList events = _executive.EventList;
                if (events.Count > 0)
                {
                    long realWorldElapsedTicks = DateTime.Now.Ticks - _realWorldStartTime.Ticks;
                    DateTime timeOfNextEvent = ((IExecEvent)events[0]).When;
                    long simElapsedTicks = timeOfNextEvent.Ticks - _simWorldStartTime.Ticks;
                    long targetRealWorldElapsedTicks = simElapsedTicks / (long)_linearScale;

                    if (realWorldElapsedTicks < targetRealWorldElapsedTicks)
                    {
                        TimeSpan realWorldNap = Utility.TimeSpanOperations.Min(_maxNap,
                            TimeSpan.FromTicks(targetRealWorldElapsedTicks - realWorldElapsedTicks));
                        TimeSpan simNap = TimeSpan.FromTicks((long)(realWorldNap.Ticks * _linearScale));
                        _executive.RequestDaemonEvent(RetardExecution, _executive.Now + simNap,
                            0.0, realWorldNap);
                    }
                }
            }
        }

        /// <summary>
        /// Retards the executive by putting it to sleep until real time has caught up with the scale.
        /// </summary>
        /// <param name="exec"></param>
        /// <param name="userData"></param>
        private void RetardExecution(IExecutive exec, object userData)
        {
            if (Math.Abs(_linearScale) > double.Epsilon)
                Thread.Sleep((TimeSpan)userData);
        }


        private bool _renderPending;
        private bool _abortRendering = false;
        private void RunRendering()
        {
            _Debug.Assert(Thread.CurrentThread.Equals(_renderThread));

            while (!_abortRendering)
            {
                if (_executive.State.Equals(ExecState.Running))
                {
                    int nTicksToSleep = 500; // Check to see if we've changed frame rate from zero, every half-second.
                    if (_frameRate > 0)
                    {

                        nTicksToSleep = (int)TimeSpan.FromSeconds(1.0 / _frameRate).TotalMilliseconds;
                        Thread.Sleep(nTicksToSleep);

                        if (!_renderPending)
                        {
                            // If there's already one pending, we skip it and wait for the next one.
                            if (_frameRate > 0 && _executive.State.Equals(ExecState.Running))
                            {
                                // Race condition? Yes, race condition. Could complete between this and the next.
                                _executive.RequestImmediateEvent(DoRender, null,
                                    ExecEventType.Synchronous);
                            }
                            _renderPending = true;
                        }
                    }
                    else
                    {
                        Thread.Sleep(nTicksToSleep);
                    }
                }
                else if (_executive.State.Equals(ExecState.Paused))
                {
                    Thread.Sleep(500);
                    _realWorldStartTime = DateTime.Now;
                    _simWorldStartTime = _executive.Now;
                }
                else if (_executive.State.Equals(ExecState.Stopped))
                {
                    break;
                }
                else if (_executive.State.Equals(ExecState.Finished))
                {
                    break;
                }
            }
        }

        private void DoRender(IExecutive exec, object userData)
        {
            if (_frameRate > 0)
                Render?.Invoke(exec, userData);
            _renderPending = false;
        }

        #region (Private) Kickoff support.
        private void executive_ExecutiveStarted(IExecutive exec)
        {
            exec.EventAboutToFire += _kickoffManager.Kickoff;
        }

        #endregion

        private class KickoffMgr
        {
            private readonly IExecutive _exec;
            private readonly ExecController _parent;
            public KickoffMgr(ExecController parent, IExecutive exec)
            {
                _exec = exec;
                _parent = parent;
            }

            public void Kickoff(long key, ExecEventReceiver eer, double priority, DateTime when, object userData, ExecEventType eventType)
            {
                _exec.EventAboutToFire -= Kickoff;
                _parent.Begin(_parent._executive, _parent._userData);
            }
        }
    }
}
