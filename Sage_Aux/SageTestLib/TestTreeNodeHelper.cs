/* This source code licensed under the GNU Affero General Public License */
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Diagnostics;

namespace Highpoint.Sage.Utility
{
    /// <summary>
	/// Summary description for zTestTemperatureController.
	/// </summary>
	[TestClass]
    public class TreeNodeHelperTester
    {
        public TreeNodeHelperTester()
        {
            Init();
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

        private readonly string _adamsResult = "Joseph Adams 1654-1736\r\n\tJohn Adams Sr, 1690-1761\r\n\t\tJohn Adams, Jr 1735-1826\r\n\t\t\tAbigail Adams 1765-1813\r\n\t\t\tSusanna Adams 1768-1770\r\n\t\t\tCharles Adams b. 1770\r\n\t\t\tThomas Boylston Adams b. 1772\r\n\t\t\tJohn Quincy Adams 1767-1848\r\n\t\t\t\tGeorge Washington Adams b. 1801\r\n\t\t\t\tJohn Adams, III b. 1803\r\n\t\t\t\tCharles Francis Adams b. 1807\r\n\t\t\t\tLouisa Catherine Adams b. 1811\r\n";
        private readonly string _adamsResultJQAChildrenSequenced = "Joseph Adams 1654-1736\r\n\tJohn Adams Sr, 1690-1761\r\n\t\tJohn Adams, Jr 1735-1826\r\n\t\t\tAbigail Adams 1765-1813\r\n\t\t\tSusanna Adams 1768-1770\r\n\t\t\tCharles Adams b. 1770\r\n\t\t\tThomas Boylston Adams b. 1772\r\n\t\t\tJohn Quincy Adams 1767-1848\r\n\t\t\t\tCharles Francis Adams b. 1807\r\n\t\t\t\tGeorge Washington Adams b. 1801\r\n\t\t\t\tJohn Adams, III b. 1803\r\n\t\t\t\tLouisa Catherine Adams b. 1811\r\n";

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test creates and navigates a tree built of the TreeNodeHelper proxy.")]
        public void TestTreeNodeHelperBasics()
        {


            string ja0 = "Joseph Adams 1654-1736";
            string ja1 = "John Adams Sr, 1690-1761";
            string ja2 = "John Adams, Jr 1735-1826";
            string aa1 = "Abigail Adams 1765-1813";
            string jqa = "John Quincy Adams 1767-1848";
            string gwa = "George Washington Adams b. 1801";
            string ja3 = "John Adams, III b. 1803";
            string cfa = "Charles Francis Adams b. 1807";
            string lca = "Louisa Catherine Adams b. 1811";
            string sa = "Susanna Adams 1768-1770";
            string ca = "Charles Adams b. 1770";
            string tba = "Thomas Boylston Adams b. 1772";

            TreeNodeHelper root = new TreeNodeHelper(ja0, false, false);
            TreeNodeHelper child = (TreeNodeHelper)root.AddChild(ja1);
            TreeNodeHelper gchild = (TreeNodeHelper)child.AddChild(ja2);
            gchild.AddChild(aa1);
            gchild.AddChild(sa);
            gchild.AddChild(ca);
            gchild.AddChild(tba);
            TreeNodeHelper ggchild = (TreeNodeHelper)gchild.AddChild(jqa);
            ggchild.AddChild(gwa);
            ggchild.AddChild(ja3);
            ggchild.AddChild(cfa);
            ggchild.AddChild(lca);


            string result = root.ToStringDeep();
            Assert.IsTrue(_adamsResult.Equals(result, StringComparison.Ordinal), "TestTreeNodeHelperBasics", StringComparison.Ordinal);
        }

        [TestMethod]
        public void TestReadOnlyTreeNodeHelperBasics()
        {
            string ja0 = "Joseph Adams 1654-1736";
            string ja1 = "John Adams Sr, 1690-1761";
            string ja2 = "John Adams, Jr 1735-1826";
            string aa1 = "Abigail Adams 1765-1813";
            string jqa = "John Quincy Adams 1767-1848";
            string gwa = "George Washington Adams b. 1801";
            string ja3 = "John Adams, III b. 1803";
            string cfa = "Charles Francis Adams b. 1807";
            string lca = "Louisa Catherine Adams b. 1811";
            string sa = "Susanna Adams 1768-1770";
            string ca = "Charles Adams b. 1770";
            string tba = "Thomas Boylston Adams b. 1772";

            TreeNodeHelper root = new TreeNodeHelper(ja0, false, false);
            TreeNodeHelper child = (TreeNodeHelper)root.AddChild(ja1);
            child = (TreeNodeHelper)child.AddChild(ja2);
            child.AddChild(aa1);
            child.AddChild(sa);
            child.AddChild(ca);
            child.AddChild(tba);
            TreeNodeHelper jqaNode = (TreeNodeHelper)child.AddChild(jqa);

            jqaNode.AddChild(gwa);
            jqaNode.AddChild(ja3);
            jqaNode.AddChild(cfa);
            jqaNode.AddChild(lca);

            jqaNode.SetReadOnly(true);

            bool blewUp = false;
            try
            {
                jqaNode.AddChild("Alfred E. Neuman 1971-1977");
            }
            catch (ArgumentException)
            {
                blewUp = true;
            }
            Assert.IsTrue(blewUp, "TestReadOnlyTreeNodeHelperBasics");


            string result = jqaNode.GetRoot().ToStringDeep();

            Console.WriteLine(result);
            Assert.IsTrue(_adamsResult.Equals(result, StringComparison.Ordinal), "TestReadOnlyTreeNodeHelperBasics", StringComparison.Ordinal);
        }

        [TestMethod]
        public void TestTreeNodeHelperChildSequencing()
        {


            string ja0 = "Joseph Adams 1654-1736";
            string ja1 = "John Adams Sr, 1690-1761";
            string ja2 = "John Adams, Jr 1735-1826";
            string aa1 = "Abigail Adams 1765-1813";
            string jqa = "John Quincy Adams 1767-1848";
            string gwa = "George Washington Adams b. 1801";
            string ja3 = "John Adams, III b. 1803";
            string cfa = "Charles Francis Adams b. 1807";
            string lca = "Louisa Catherine Adams b. 1811";
            string sa = "Susanna Adams 1768-1770";
            string ca = "Charles Adams b. 1770";
            string tba = "Thomas Boylston Adams b. 1772";

            TreeNodeHelper root = new TreeNodeHelper(ja0, false, false);
            TreeNodeHelper child = (TreeNodeHelper)root.AddChild(ja1);
            child = (TreeNodeHelper)child.AddChild(ja2);
            child.AddChild(aa1);
            child.AddChild(sa);
            child.AddChild(ca);
            child.AddChild(tba);
            TreeNodeHelper jqaNode = (TreeNodeHelper)child.AddChild(jqa);

            jqaNode.AddChild(gwa);
            jqaNode.AddChild(ja3);
            jqaNode.AddChild(cfa);
            jqaNode.AddChild(lca);

            jqaNode.ResequenceChildren(Comparer.Default);
            string result = jqaNode.GetRoot().ToStringDeep();

            Console.WriteLine(result);
            Assert.IsTrue(_adamsResultJQAChildrenSequenced.Equals(result, StringComparison.Ordinal), "TestTreeNodeHelperChildSequencing");
        }
    }
}