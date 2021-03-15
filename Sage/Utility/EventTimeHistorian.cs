/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.SimCore;
using System;
namespace Highpoint.Sage.Utility
{
    // TODO: Consider eliminating the shift bits thing, and going to division.

    /// <summary>
    /// An EventTimeHistorian keeps track of the times at which the last 'N' events that were submitted to it
    /// occurred, and provides the average inter-event duration for those events. Historians with specific
    /// event-type-related data needs (other than simply the time of occurrence) can inherit from this class.
    /// </summary>
    public class EventTimeHistorian
    {

        #region Private Fields
        private readonly IExecutive _exec;
        private readonly DateTime[] _eventTimes;
        private int _head;
        private int _nLogged;
        private readonly int _nPastEventCapacity;
        private readonly int _shiftBits;
        private bool _filled;
        #endregion Private Fields

        /// <summary>
        /// Initializes a new instance of the <see cref="EventTimeHistorian"/> class, tracking the last m events, where
        /// m is the lowest (2^n)+1 that is greater than numPastEventsTracked. (n is any whole nonnegative number.)
        /// </summary>
        /// <param name="exec">The executive that is to be tracked.</param>
        /// <param name="numPastEventsTracked">The number of past events that will be tracked.</param>
        public EventTimeHistorian(IExecutive exec, int numPastEventsTracked)
        {
            _shiftBits = (int)Math.Round(Math.Log(numPastEventsTracked) / Math.Log(2.0));
            _nPastEventCapacity = (int)Math.Pow(2.0, _shiftBits) + 1;
            _exec = exec;
            _eventTimes = new DateTime[_nPastEventCapacity];
            _head = -1;
            _filled = false;
        }

        /// <summary>
        /// Logs the fact that an event was just fired.
        /// </summary>
        public void LogEvent()
        {
            _nLogged++;
            _head++;
            if (_head == _nPastEventCapacity)
            {
                _head = 0;
                _filled = true;
            }
            _eventTimes[_head] = _exec.Now;
        }

        /// <summary>
        /// Gets the max number of past events that can be tracked.
        /// </summary>
        /// <value>The past event capacity.</value>
        public int PastEventCapacity => _nPastEventCapacity;

        /// <summary>
        /// Gets the number of past events received.
        /// </summary>
        /// <value>The past events received.</value>
        public int PastEventsReceived => _nLogged;

        /// <summary>
        /// Gets the average intra event duration for the past n events. If n is -1, .
        /// </summary>
        /// <param name="numPastEvents">The number of past events to be considered. If -1, then the entire set of tracked events (numPastEventsTracked) is considered.</param>
        /// <returns>TimeSpan.</returns>
        /// <exception cref="OverflowException"></exception>
        public TimeSpan GetAverageIntraEventDuration(int numPastEvents = -1)
        {
            if (numPastEvents == -1)
                numPastEvents = Math.Min(_nLogged, _nPastEventCapacity);

            if (numPastEvents > _eventTimes.Length)
                throw new OverflowException(string.Format(_caller_Requested_Too_Many_Data_Points, numPastEvents, _eventTimes.Length));
            if (!_filled)
            {
                if (_nLogged < 2)
                    return TimeSpan.Zero;
                int tail = _head - numPastEvents + 1;
                if (tail < 0)
                    tail += _nPastEventCapacity;
                int nEvents = Math.Min(_nLogged, (_nPastEventCapacity));
                long deltaTicks = _eventTimes[_head].Ticks - _eventTimes[tail].Ticks;
                long avgIntraEventDuration = deltaTicks / (nEvents - 1);
                return TimeSpan.FromTicks(avgIntraEventDuration);
            }
            else
            {
                int tail = _head + 1;
                if (tail == _nPastEventCapacity)
                    tail = 0;
                return TimeSpan.FromTicks((_eventTimes[_head] - _eventTimes[tail]).Ticks >> _shiftBits);
            }
        }

        private static readonly string _caller_Requested_Too_Many_Data_Points = "Caller tried to obtain statistics on the last {0} events in an historian that is only tracking {1} events.";
    }
}
