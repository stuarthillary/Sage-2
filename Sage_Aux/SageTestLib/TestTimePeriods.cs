/* This source code licensed under the GNU Affero General Public License */
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

// TODO: Debug failing tests.

namespace Highpoint.Sage.Scheduling
{

    /// <summary>
    /// Summary description for zTestTimePeriods.
    /// </summary>
    [TestClass]
    public class TimePeriodTester
    {
        public TimePeriodTester()
        {
            _now = new DateTime(2001, 05, 16, 12, 0, 0);
            _fiveMinutes = TimeSpan.FromMinutes(5);
            _tenMinutes = TimeSpan.FromMinutes(10);
            _fifteenMinutes = TimeSpan.FromMinutes(15);
            _twentyMinutes = TimeSpan.FromMinutes(20);
            _fiveMinsAgo = _now - _fiveMinutes;
            _fiveMinsOn = _now + _fiveMinutes;
            _tenMinsAgo = _now - _tenMinutes;
            _tenMinsOn = _now + _tenMinutes;
        }

        private readonly DateTime _now = DateTime.Now;
        private readonly TimeSpan _fiveMinutes;
        private readonly TimeSpan _tenMinutes;
        private readonly TimeSpan _fifteenMinutes;
        private readonly TimeSpan _twentyMinutes;
        private readonly DateTime _fiveMinsAgo;
        private readonly DateTime _fiveMinsOn;
        private readonly DateTime _tenMinsAgo;
        private readonly DateTime _tenMinsOn;

        [TestInitialize]
        public void Init()
        {
        }
        [TestCleanup]
        public void destroy()
        {
            Debug.WriteLine("Done.");
        }

        [TestMethod]
        public void TestTimePeriodBasics()
        {

            TimePeriod tp;

            Debug.WriteLine("Now = " + _now);

            #region Fixed Start Time
            // Test creation of a fixed start time TimePeriod.
            tp = new TimePeriod(_fiveMinsAgo, _fiveMinsOn, TimeAdjustmentMode.FixedStart);
            Assert.IsTrue(tp.Duration.Equals(_tenMinutes), "TimePeriod Failure - initial duration on fixed start.");

            // Test modification of duration.
            tp.Duration = _fiveMinutes;
            Assert.IsTrue(tp.EndTime.Equals(_now), "TimePeriod Failure - end time on fixed start.");

            // Test modification of end time.
            tp.EndTime = _fiveMinsOn;
            Assert.IsTrue(tp.Duration.Equals(_tenMinutes), "TimePeriod Failure - duration on fixed start.");
            #endregion

            #region Fixed End Time
            // Test creation of a fixed end time TimePeriod.
            tp = new TimePeriod(_fiveMinsAgo, _fiveMinsOn, TimeAdjustmentMode.FixedEnd);
            Assert.IsTrue(tp.Duration.Equals(_tenMinutes), "TimePeriod Failure - initial duration on fixed end.");

            // Test modification of duration.
            tp.Duration = _fiveMinutes;
            Assert.IsTrue(tp.StartTime.Equals(_now), "TimePeriod Failure - start time on fixed end.");

            // Test modification of start time.
            tp.StartTime = _now;
            Assert.IsTrue(tp.Duration.Equals(_fiveMinutes), "TimePeriod Failure - duration on fixed end.");
            #endregion

            #region Fixed Duration
            // Test creation of a fixed duration TimePeriod.
            tp = new TimePeriod(_fiveMinsAgo, _fiveMinsOn, TimeAdjustmentMode.FixedDuration);
            Assert.IsTrue(tp.Duration.Equals(_tenMinutes), "TimePeriod Failure - initial duration on fixed duration.");

            // Test modification of start time.
            tp.StartTime = _tenMinsAgo;
            //            Assert.IsTrue(tp.EndTime.Equals(Now), "TimePeriod Failure - initial duration on fixed duration.");

            // Test modification of end time.
            tp.EndTime = _fiveMinsOn;
            //            Assert.IsTrue(tp.StartTime.Equals(FiveMinsAgo), "TimePeriod Failure - start time on fixed duration.");
            #endregion

            #region Infer Start Time
            // Test creation of a fixed start time TimePeriod.
            tp = new TimePeriod(_tenMinutes, _fiveMinsOn, TimeAdjustmentMode.InferStartTime);
            Assert.IsTrue(tp.StartTime.Equals(_fiveMinsAgo), "TimePeriod Failure - initial start time on inferred start time.");

            // Test modification of duration.
            tp.Duration = _fiveMinutes;
            Assert.IsTrue(tp.StartTime.Equals(_now), "TimePeriod Failure - changed duration on inferred start time.");

            // Test modification of end time.
            tp.EndTime = _tenMinsOn;
            //            Assert.IsTrue(tp.StartTime.Equals(FiveMinsOn), "TimePeriod Failure - changed end time on inferred start time.");
            #endregion

            #region Infer End Time
            // Test creation of a fixed end time TimePeriod.
            tp = new TimePeriod(_fiveMinsAgo, _tenMinutes, TimeAdjustmentMode.InferEndTime);
            Assert.IsTrue(tp.EndTime.Equals(_fiveMinsOn), "TimePeriod Failure - initial end time on inferred end.");

            // Test modification of start time.
            tp.StartTime = _now;
            //            Assert.IsTrue(tp.EndTime.Equals(TenMinsOn), "TimePeriod Failure - changed start time on fixed end.");

            // Test modification of duration.
            tp.Duration = _fiveMinutes;
            Assert.IsTrue(tp.EndTime.Equals(_fiveMinsOn), "TimePeriod Failure - changed duration on fixed end.");
            #endregion

            #region Infer Duration
            // Test creation of a fixed duration TimePeriod.
            tp = new TimePeriod(_fiveMinsAgo, _fiveMinsOn, TimeAdjustmentMode.InferDuration);
            Assert.IsTrue(tp.Duration.Equals(_tenMinutes), "TimePeriod Failure - initial duration on inferred duration.");

            // Test modification of start time.
            tp.StartTime = _now;
            Assert.IsTrue(tp.Duration.Equals(_fiveMinutes), "TimePeriod Failure - changed end time on fixed duration.");

            // Test modification of end time.
            tp.EndTime = _tenMinsOn;
            Assert.IsTrue(tp.Duration.Equals(_tenMinutes), "TimePeriod Failure - changed start time on fixed duration.");
            #endregion
        }

        [TestMethod]
        public void TestTimePeriodEnvelope()
        {

            TimePeriod tp1 = new TimePeriod(_fiveMinsAgo, _now, TimeAdjustmentMode.FixedDuration);
            foreach (IMilestone ms in new IMilestone[] { tp1.StartMilestone, tp1.EndMilestone })
            {
                Console.WriteLine("Relationships involving " + ms.Name + " are:");
                foreach (MilestoneRelationship mr in ms.Relationships)
                {
                    Console.WriteLine("\t" + mr.ToString());
                }
            }

            TimePeriod tp2 = new TimePeriod(_now, _fiveMinsOn, TimeAdjustmentMode.FixedDuration);
            TimePeriod tp3 = new TimePeriod(_fiveMinsOn, _tenMinsOn, TimeAdjustmentMode.FixedDuration);


            Console.WriteLine("Creating a time period envelope and adding " + tp1.ToString() + " and " + tp2.ToString() + " to it.");
            TimePeriodEnvelope tpe = new TimePeriodEnvelope();
            tpe.AddTimePeriod(tp1);
            tpe.AddTimePeriod(tp2);

            Assert.IsTrue(tpe.Duration.Equals(_tenMinutes), "TimePeriodEnvelope Failure a");

            tpe.AddTimePeriod(tp3);
            Assert.IsTrue(tpe.Duration.Equals(_fifteenMinutes), "TimePeriodEnvelope Failure b");

            Console.WriteLine("Removing " + tp1.ToString() + " from it.");
            tpe.RemoveTimePeriod(tp1);
            Assert.IsTrue(tpe.Duration.Equals(_tenMinutes), "TimePeriodEnvelope Failure c");


        }
        [TestMethod]
        public void TestNestedTimePeriodEnvelope()
        {

            TimePeriod tp1 = new TimePeriod("FivePast", Guid.NewGuid(), _fiveMinsAgo, _now, TimeAdjustmentMode.FixedDuration);
            foreach (IMilestone ms in new IMilestone[] { tp1.StartMilestone, tp1.EndMilestone })
            {
                Console.WriteLine("Relationships involving " + ms.Name + " are:");
                foreach (MilestoneRelationship mr in ms.Relationships)
                {
                    Console.WriteLine("\t" + mr.ToString());
                }
            }

            TimePeriod tp2 = new TimePeriod("FiveNext", Guid.NewGuid(), _now, _fiveMinsOn, TimeAdjustmentMode.FixedDuration);
            TimePeriod tp3 = new TimePeriod("FiveFuture", Guid.NewGuid(), _fiveMinsOn, _tenMinsOn, TimeAdjustmentMode.FixedDuration);

            TimePeriodEnvelope tpe = new TimePeriodEnvelope("Root", Guid.NewGuid());
            TimePeriodEnvelope tpe2 = new TimePeriodEnvelope("RootsChild", Guid.NewGuid());
            tpe.AddTimePeriod(tpe2);
            tpe2.AddTimePeriod(tp1);
            tpe2.AddTimePeriod(tp2);
            Assert.IsTrue(tpe.Duration.Equals(_tenMinutes), "TimePeriodEnvelope Failure a");

            tpe2.AddTimePeriod(tp3);

            Assert.IsTrue(tpe.Duration.Equals(_fifteenMinutes), "TimePeriodEnvelope Failure b");

            Console.WriteLine("Removing " + tp1.ToString() + " from it.");
            tpe2.RemoveTimePeriod(tp1);

            Assert.IsTrue(tpe.Duration.Equals(_tenMinutes), "TimePeriodEnvelope Failure c");

        }
    }
}
