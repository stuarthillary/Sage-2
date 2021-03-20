/* This source code licensed under the GNU Affero General Public License */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;


namespace Highpoint.Sage.Scheduling
{

    /// <summary>
    /// Summary description for zTestTimePeriods.
    /// </summary>
    [TestClass]
    public class MilestoneRelationshipTester
    {
        public MilestoneRelationshipTester()
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
            _twentyMinsOn = _now + _twentyMinutes;
            _twentyMinsAgo = _now - _twentyMinutes;
        }

        private readonly DateTime _now = new DateTime(2001, 05, 16, 12, 0, 0);
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
        public void TestMilestones()
        {
            Milestone ms1 = new Milestone(DateTime.Now);
            ms1.ChangeEvent += new ObservableChangeHandler(ChangeEvent);
            Debug.WriteLine(ms1.ToString());
            ms1.MoveBy(-_twentyMinutes);
            Debug.WriteLine(ms1.ToString());

            Milestone ms2 = new Milestone(DateTime.Now + _fiveMinutes);
            ms2.ChangeEvent += new ObservableChangeHandler(ChangeEvent);
            Debug.WriteLine(ms2.ToString());
            ms2.MoveBy(-_twentyMinutes);
            Debug.WriteLine(ms2.ToString());

            Debug.WriteLine("Milestone 1 is at " + ms1 + ", and Milestone 2 is at " + ms2 + ". Strutting them together.");
            MilestoneRelationship mr = new MilestoneRelationship_Strut(ms1, ms2);
            ms1.AddRelationship(mr);
            ms2.AddRelationship(mr);

            Debug.WriteLine("Moving Milestone1 by ten minutes.");
            ms1.MoveBy(_tenMinutes);
            Debug.WriteLine("Milestone 1 is at " + ms1 + ", and Milestone 2 is at " + ms2 + ".");

        }

        private void ChangeEvent(object whoChanged, object whatChanged, object howChanged)
        {
            Debug.WriteLine(((Milestone)whoChanged).ToString() + " changed by " + howChanged.ToString());
        }
    }
}
