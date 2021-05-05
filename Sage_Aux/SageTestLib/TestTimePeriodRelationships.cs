/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;


namespace Highpoint.Sage.Scheduling
{

    /// <summary>
    /// Summary description for zTestTimePeriods.
    /// </summary>
    [TestClass]
    public class TimePeriodRelationshipTester
    {

        #region Private Fields
        private readonly DateTime _now = DateTime.Now;
        private readonly TimeSpan _fiveMinutes;
        private readonly TimeSpan _tenMinutes;
        private readonly TimeSpan _fifteenMinutes;
        private readonly TimeSpan _twentyMinutes;
        private readonly DateTime _fiveMinsAgo;
        private readonly DateTime _fiveMinsOn;
        private readonly DateTime _tenMinsAgo;
        private readonly DateTime _tenMinsOn;
        private readonly DateTime _twentyMinsOn;
        private readonly DateTime _twentyMinsAgo;


        private readonly TimePeriod.Relationship[] _relationships = new TimePeriod.Relationship[]{
                                                                TimePeriod.Relationship.StartsBeforeStartOf,
                                                                TimePeriod.Relationship.StartsOnStartOf,
                                                                TimePeriod.Relationship.StartsAfterStartOf,
                                                                TimePeriod.Relationship.StartsBeforeEndOf,
                                                                TimePeriod.Relationship.StartsOnEndOf,
                                                                TimePeriod.Relationship.StartsAfterEndOf,
                                                                TimePeriod.Relationship.EndsBeforeStartOf,
                                                                TimePeriod.Relationship.EndsOnStartOf,
                                                                TimePeriod.Relationship.EndsAfterStartOf,
                                                                TimePeriod.Relationship.EndsBeforeEndOf,
                                                                TimePeriod.Relationship.EndsOnEndOf,
                                                                TimePeriod.Relationship.EndsAfterEndOf };
        #endregion

        #region Constructors
        public TimePeriodRelationshipTester()
        {
            _now = DateTime.Now;
            _fiveMinutes = TimeSpan.FromMinutes(5);
            _tenMinutes = TimeSpan.FromMinutes(10);
            _fifteenMinutes = TimeSpan.FromMinutes(15);
            _twentyMinutes = TimeSpan.FromMinutes(20);
            _fiveMinsAgo = _now - _fiveMinutes;
            _fiveMinsOn = _now + _fiveMinutes;
            _tenMinsAgo = _now - _tenMinutes;
            _tenMinsOn = _now + _tenMinutes;
            _twentyMinsAgo = _now - _twentyMinutes;
            _twentyMinsOn = _now + _twentyMinutes;
        }
        #endregion

        #region Test Setup & TearDown
        [TestInitialize]
        public void Init()
        {
        }
        [TestCleanup]
        public void destroy()
        {
            Debug.WriteLine("Done.");
        }
        #endregion

        #region Basics
        [TestMethod]
        public void TestBasics()
        {
            TimePeriod tpA = null;
            TimePeriod tpB = null;
            foreach (TimePeriod.Relationship relationship in _relationships)
            {
                Debug.WriteLine("************************************************************************");
                Debug.WriteLine("          A " + relationship.ToString() + " B.");
                Debug.WriteLine("************************************************************************");
                foreach (TimeSpan slack in new TimeSpan[] { _fiveMinutes, _tenMinutes, _twentyMinutes })
                {
                    foreach (TimePeriodPart moveWhichPart in new TimePeriodPart[] { TimePeriodPart.StartTime, TimePeriodPart.EndTime })
                    {
                        foreach (string ofWhich in new string[] { "A", "B" })
                        {
                            foreach (TimeSpan movement in new TimeSpan[] { _fiveMinutes, _tenMinutes, _twentyMinutes })
                            {
                                SetUpTimePeriods(ref tpA, ref tpB, relationship, slack);
                                RunTest(tpA, tpB, relationship, moveWhichPart, ofWhich, movement);
                            }
                        }
                    }
                }
            }
        }


        private void RunTest(TimePeriod tpA, TimePeriod tpB, TimePeriod.Relationship relationship,
            TimePeriodPart moveWhichPart, string ofWhich, TimeSpan byHowMuch)
        {
            bool CONSOLE_OUTPUT = false;

            tpA.AddRelationship(relationship, tpB);

            DateTime earliest = DateTimeOperations.Min(tpA.StartTime, tpB.StartTime);
            DateTime left = earliest - new TimeSpan(0, 0, (earliest.Minute % 10), 0, 0);

            int spaces, width;
            string A, B;

            if (CONSOLE_OUTPUT)
            {
                Debug.WriteLine("           ....*....|....*....|....*....|....*....|....*....|");
                A = "Initial A : ";
                spaces = (int)((TimeSpan)(tpA.StartTime - left)).TotalMinutes;
                width = (int)tpA.Duration.TotalMinutes;
                for (int i = 0; i < spaces; i++)
                    A += " ";
                for (int i = 0; i < width; i++)
                    A += "a";
                for (int i = (spaces + width); i < 50; i++)
                    A += " ";
                Debug.WriteLine(A + tpA.ToString());


                B = "Initial B : ";
                spaces = (int)((TimeSpan)(tpB.StartTime - left)).TotalMinutes;
                width = (int)tpB.Duration.TotalMinutes;
                for (int i = 0; i < spaces; i++)
                    B += " ";
                for (int i = 0; i < width; i++)
                    B += "b";
                for (int i = (spaces + width); i < 50; i++)
                    B += " ";
                Debug.WriteLine(B + tpB.ToString());

                Debug.WriteLine("We'll move " + ofWhich + "." + moveWhichPart.ToString() + " by " + byHowMuch.ToString() + "...");
            }

            int selector = 0;
            if (ofWhich.Equals("A", StringComparison.Ordinal))
                selector += 0;
            if (ofWhich.Equals("B", StringComparison.Ordinal))
                selector += 1;
            selector <<= 1;
            if (moveWhichPart.Equals(TimePeriodPart.StartTime))
                selector += 0;
            if (moveWhichPart.Equals(TimePeriodPart.EndTime))
                selector += 1;


            tpA.AdjustmentMode = TimeAdjustmentMode.FixedDuration;
            tpB.AdjustmentMode = TimeAdjustmentMode.FixedDuration;
            if (CONSOLE_OUTPUT)
            {
                Console.WriteLine("3.");
                foreach (IMilestone ms in new IMilestone[] { tpB.StartMilestone, tpB.EndMilestone })
                {
                    Console.WriteLine(ms.Name);
                    foreach (MilestoneRelationship mr in ms.Relationships)
                    {
                        Console.WriteLine(mr.ToString());
                    }
                }
                Console.WriteLine("4.");
                foreach (IMilestone ms in new IMilestone[] { tpB.StartMilestone, tpB.EndMilestone })
                {
                    Console.WriteLine(ms.Name);
                    foreach (MilestoneRelationship mr in ms.Relationships)
                    {
                        Console.WriteLine(mr.ToString());
                    }
                }
            }

            switch (selector)
            {
                case 0:
                    tpA.AdjustmentMode = TimeAdjustmentMode.InferEndTime;
                    tpA.StartTime += byHowMuch;
                    break;
                case 1:
                    tpA.AdjustmentMode = TimeAdjustmentMode.InferStartTime;
                    tpA.EndTime += byHowMuch;
                    break;
                case 2:
                    tpB.AdjustmentMode = TimeAdjustmentMode.InferEndTime;
                    tpB.StartTime += byHowMuch;
                    break;
                case 3:
                    tpB.AdjustmentMode = TimeAdjustmentMode.InferStartTime;
                    tpB.EndTime += byHowMuch;
                    break;
            }

            if (CONSOLE_OUTPUT)
            {
                A = "Final A   : ";
                spaces = (int)((TimeSpan)(tpA.StartTime - left)).TotalMinutes;
                width = (int)tpA.Duration.TotalMinutes;
                for (int i = 0; i < spaces; i++)
                    A += " ";
                for (int i = 0; i < width; i++)
                    A += "A";
                for (int i = (spaces + width); i < 50; i++)
                    A += " ";
                Debug.WriteLine(A + tpA.ToString());

                B = "Final B   : ";
                spaces = (int)((TimeSpan)(tpB.StartTime - left)).TotalMinutes;
                width = (int)tpB.Duration.TotalMinutes;
                for (int i = 0; i < spaces; i++)
                    B += " ";
                for (int i = 0; i < width; i++)
                    B += "B";
                for (int i = (spaces + width); i < 50; i++)
                    B += " ";
                Debug.WriteLine(B + tpB.ToString());
            }

            foreach (TimePeriod tp in new TimePeriod[] { tpA, tpB })
            {
                foreach (IMilestone ms in new IMilestone[] { tp.StartMilestone, tp.EndMilestone })
                {
                    foreach (MilestoneRelationship mr in ms.Relationships)
                    {
                        bool b = mr.IsSatisfied();
                        Console.WriteLine(mr + (b ? " is " : " is not ") + "satisfied.");
                        //Assert.IsTrue(b, mr + " is not satisfied!");
                    }
                }
            }
        }


        private void SetUpTimePeriods(ref TimePeriod tpA, ref TimePeriod tpB, TimePeriod.Relationship relationship, TimeSpan offset)
        {
            tpB = new TimePeriod("TimePeriod B", Guid.NewGuid(), _fiveMinsAgo, _fiveMinsOn, TimeAdjustmentMode.InferDuration);
            switch (relationship)
            {
                case TimePeriod.Relationship.StartsBeforeStartOf:
                    {
                        tpA = new TimePeriod("TimePeriod A", Guid.NewGuid(), tpB.StartTime - offset, _tenMinutes, TimeAdjustmentMode.InferEndTime);
                        //tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
                        break;
                    }
                case TimePeriod.Relationship.StartsOnStartOf:
                    {
                        tpA = new TimePeriod("TimePeriod A", Guid.NewGuid(), tpB.StartTime, _tenMinutes, TimeAdjustmentMode.InferEndTime);
                        tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
                        break;
                    }
                case TimePeriod.Relationship.StartsAfterStartOf:
                    {
                        tpA = new TimePeriod("TimePeriod A", Guid.NewGuid(), tpB.StartTime + offset, _tenMinutes, TimeAdjustmentMode.InferEndTime);
                        tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
                        break;
                    }
                case TimePeriod.Relationship.StartsBeforeEndOf:
                    {
                        tpA = new TimePeriod("TimePeriod A", Guid.NewGuid(), tpB.EndTime - offset, _tenMinutes, TimeAdjustmentMode.InferEndTime);
                        tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
                        break;
                    }
                case TimePeriod.Relationship.StartsOnEndOf:
                    {
                        tpA = new TimePeriod("TimePeriod A", Guid.NewGuid(), tpB.EndTime, _tenMinutes, TimeAdjustmentMode.InferEndTime);
                        tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
                        break;
                    }
                case TimePeriod.Relationship.StartsAfterEndOf:
                    {
                        tpA = new TimePeriod("TimePeriod A", Guid.NewGuid(), tpB.EndTime + offset, _tenMinutes, TimeAdjustmentMode.InferEndTime);
                        tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
                        break;
                    }
                case TimePeriod.Relationship.EndsBeforeStartOf:
                    {
                        tpA = new TimePeriod("TimePeriod A", Guid.NewGuid(), _tenMinutes, tpB.StartTime - offset, TimeAdjustmentMode.InferStartTime);
                        tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
                        break;
                    }
                case TimePeriod.Relationship.EndsOnStartOf:
                    {
                        tpA = new TimePeriod("TimePeriod A", Guid.NewGuid(), _tenMinutes, tpB.StartTime, TimeAdjustmentMode.InferStartTime);
                        tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
                        break;
                    }
                case TimePeriod.Relationship.EndsAfterStartOf:
                    {
                        tpA = new TimePeriod("TimePeriod A", Guid.NewGuid(), _tenMinutes, tpB.StartTime + offset, TimeAdjustmentMode.InferStartTime);
                        tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
                        break;
                    }
                case TimePeriod.Relationship.EndsBeforeEndOf:
                    {
                        tpA = new TimePeriod("TimePeriod A", Guid.NewGuid(), _tenMinutes, tpB.EndTime - offset, TimeAdjustmentMode.InferStartTime);
                        tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
                        break;
                    }
                case TimePeriod.Relationship.EndsOnEndOf:
                    {
                        tpA = new TimePeriod("TimePeriod A", Guid.NewGuid(), _tenMinutes, tpB.EndTime, TimeAdjustmentMode.InferStartTime);
                        tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
                        break;
                    }
                case TimePeriod.Relationship.EndsAfterEndOf:
                    {
                        tpA = new TimePeriod("TimePeriod A", Guid.NewGuid(), _tenMinutes, tpB.EndTime + offset, TimeAdjustmentMode.InferStartTime);
                        tpA.AdjustmentMode = TimeAdjustmentMode.InferDuration;
                        break;
                    }
            }
        }
        #endregion

    }
}
