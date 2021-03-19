/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Randoms;
using Highpoint.Sage.SimCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

namespace Highpoint.Sage.Utility
{

    [TestClass]
    public class HistorianTester
    {
        private readonly RandomServer _rs;
        public HistorianTester()
        {
            _rs = new RandomServer();
        }

        [TestInitialize]
        public void Init()
        {
        }
        [TestCleanup]
        public void destroy()
        {
            Debug.WriteLine("Done.");
        }

        int NUM_SAMPLES = 9000;
        TimeSpan _actualAverage = TimeSpan.Zero;
        TimeSpan _accumulatedDeviation = TimeSpan.Zero;
        readonly DateTime _startDate = new DateTime(2006, 01, 27, 09, 26, 00);

        [TestMethod]
        public void TestEventTimeHistorian()
        {
            IRandomChannel irc = _rs.GetRandomChannel();
            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            EventTimeHistorian myHistorian = new EventTimeHistorian(exec, 256);
            DateTime when = _startDate;

            // We set up NUM_SAMPLES events with random (0->50) minute intervals.
            TimeSpan totalTimeSpan = TimeSpan.Zero;
            for (int i = 0; i < NUM_SAMPLES; i++)
            {
                exec.RequestEvent(DoEvent, when, 0.0, myHistorian, ExecEventType.Synchronous);
                double d = irc.NextDouble();
                TimeSpan delta = TimeSpan.FromMinutes(d * 50.0);
                totalTimeSpan += delta;
                if (i < 30)
                    Console.WriteLine("Delta #" + i + ", " + delta.ToString());
                when += delta;
            }

            _actualAverage = TimeSpan.FromTicks(totalTimeSpan.Ticks / NUM_SAMPLES);
            Console.WriteLine("Average timeSpan was " + _actualAverage + ".");

            exec.Start();

            Console.WriteLine("After {0} events, the average interval was {1}.", myHistorian.PastEventsReceived, myHistorian.GetAverageIntraEventDuration());

        }

        private int _numExecEventsFired;
        private void DoEvent(IExecutive exec, object userData)
        {
            EventTimeHistorian eth = ((EventTimeHistorian)userData);
            eth.LogEvent();
            if (exec.Now > _startDate)
            {
                _numExecEventsFired++;
                TimeSpan aied = eth.GetAverageIntraEventDuration();
                _accumulatedDeviation += (_actualAverage - aied);
                Console.WriteLine(
                    "Average interval = " + aied +
                    ", Average deviation = " + TimeSpan.FromTicks(_accumulatedDeviation.Ticks / _numExecEventsFired).TotalMinutes);
            }
        }
    }
}
