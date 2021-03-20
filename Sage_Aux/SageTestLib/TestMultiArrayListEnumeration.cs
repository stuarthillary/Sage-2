/* This source code licensed under the GNU Affero General Public License */
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Diagnostics;

namespace Highpoint.Sage.Utility
{
    /// <summary>
    /// Summary description for zTestInterpolations.
    /// </summary>
    [TestClass]
    public class MultiArrayListEnumerationTester
    {
        private readonly ArrayList _al1;
        private readonly ArrayList _al2;
        private readonly ArrayList _al3;
        private readonly ArrayList _ale;
        private static readonly string _expected123 = "AlphaBravoCharleyDeltaEchoFoxtrotGolfHotelIndia";
        public MultiArrayListEnumerationTester()
        {
            Init();
            _al1 = new ArrayList();
            _al1.Add("Alpha");
            _al1.Add("Bravo");
            _al1.Add("Charley");
            _al2 = new ArrayList();
            _al2.Add("Delta");
            _al2.Add("Echo");
            _al2.Add("Foxtrot");
            _al3 = new ArrayList();
            _al3.Add("Golf");
            _al3.Add("Hotel");
            _al3.Add("India");
            _ale = new ArrayList();
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

        /// <summary>
        /// Basic test.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Simple test to aggregate three non-empty arraylists under one enumerator.")]
        public void TestBasicsOfEnumerator()
        {
            MultiArrayListEnumerable male = new MultiArrayListEnumerable(new ArrayList[] { _al1, _al2, _al3 });
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (string s in male)
                sb.Append(s);

            string result = sb.ToString();
            Console.WriteLine("Simple three list aggregation - " + result + ".");
            Assert.IsTrue(result.Equals(_expected123), "MultiArrayListEnumerable basics", "Failed test");
        }
        /// <summary>
        /// Basic test.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Simple test to aggregate three non-empty arraylists and one empty one in various locations under one enumerator.")]
        public void TestEnumeratorWithEmptyArrays()
        {
            Validate(new ArrayList[] { _ale, _al1, _al2, _al3 }, _expected123, "Leading", "Empty Arraylist at leading element of arraylists.");
            Validate(new ArrayList[] { _al1, _ale, _al2, _al3 }, _expected123, "Internal", "Empty Arraylist at internal element of arraylists.");
            Validate(new ArrayList[] { _al1, _al2, _al3, _ale }, _expected123, "Trailing", "Empty Arraylist at trailing element of arraylists.");
        }

        private void Validate(ArrayList[] arraylists, string expected, string name, string description)
        {
            MultiArrayListEnumerable male = new MultiArrayListEnumerable(new ArrayList[] { _al1, _al2, _al3 });
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (string s in male)
                sb.Append(s);
            string result = sb.ToString();
            Console.WriteLine(name + "\r\n\texpected = \"" + expected + "\",\r\n\tresult   = \"" + result + "\".\r\n\t\t" + (result.Equals(expected) ? "Passed.\r\n" : "Failed.\r\n"));
            Assert.IsTrue(result.Equals(expected), "MultiArrayListEnumerable basics", "Failed test");
        }
    }
}
