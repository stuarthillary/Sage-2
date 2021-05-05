/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

namespace SageTestLib
{
    [TestClass]
    public class EventedListTester
    {

        public EventedListTester()
        {
        }

        private EventedList<string> _uut = null;
        private string _responses = null;

        #region Prep Work

        public void Init()
        {
            _responses = "";
            _uut = new EventedList<string>();
            _uut.AboutToAddItem += m_uut_AboutToAddItem;
            _uut.AboutToAddItems += m_uut_AboutToAddItems;
            _uut.AboutToRemoveItem += m_uut_AboutToRemoveItem;
            _uut.AboutToRemoveItems += m_uut_AboutToRemoveItems;
            _uut.AboutToReplaceItem += m_uut_AboutToReplaceItem;
            _uut.AddedItem += m_uut_AddedItem;
            _uut.AddedItems += m_uut_AddedItems;
            _uut.RemovedItem += m_uut_RemovedItem;
            _uut.RemovedItems += m_uut_RemovedItems;
            _uut.ReplacedItem += m_uut_ReplacedItem;
            _uut.ContentsChanged += m_uut_ContentsChanged;
        }

        void m_uut_ReplacedItem(EventedList<string> list, string oldItem, string newItem)
        {
            _responses += "m_uut_ReplacedItem" + " " + oldItem + " with " + newItem + " | ";
        }

        void m_uut_AboutToReplaceItem(EventedList<string> list, string oldItem, string newItem)
        {
            _responses += "m_uut_AboutToReplaceItem" + " " + oldItem + " with " + newItem + " | ";
        }

        void m_uut_ContentsChanged(EventedList<string> list)
        {
            _responses += "m_uut_ContentsChanged" + " | ";
        }

        void m_uut_RemovedItems(EventedList<string> list, Predicate<string> match)
        {
            _responses += "m_uut_RemovedItems" + " " + match + " | ";
        }

        void m_uut_RemovedItem(EventedList<string> list, string item)
        {
            _responses += "m_uut_RemovedItem" + " " + item + " | ";
        }

        void m_uut_AddedItems(EventedList<string> list, System.Collections.Generic.IEnumerable<string> collection)
        {
            _responses += "m_uut_AddedItems" + " " + collection + " | ";
        }

        void m_uut_AddedItem(EventedList<string> list, string item)
        {
            _responses += "m_uut_AddedItem" + " " + item + " | ";
        }

        void m_uut_AboutToRemoveItems(EventedList<string> list, Predicate<string> match)
        {
            _responses += "m_uut_AboutToRemoveItems" + " " + match + " | ";
        }

        void m_uut_AboutToRemoveItem(EventedList<string> list, string item)
        {
            _responses += "m_uut_AboutToRemoveItem" + " " + item + " | ";
        }

        void m_uut_AboutToAddItems(EventedList<string> list, System.Collections.Generic.IEnumerable<string> collection)
        {
            _responses += "m_uut_AboutToAddItems" + " " + collection + " | ";
        }

        void m_uut_AboutToAddItem(EventedList<string> list, string item)
        {
            _responses += "m_uut_AboutToAddItem" + " " + item + " | ";
        }

        [TestCleanup]
        public void destroy()
        {
            Debug.WriteLine("Done.");
        }
        #endregion

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the Add mechanism.")]
        public void TestAdd()
        {
            Init();

            string addee = "String 1";
            _uut.Add(addee);

            Assert.IsTrue(_responses.Equals("m_uut_AboutToAddItem String 1 | m_uut_AddedItem String 1 | m_uut_ContentsChanged | ", StringComparison.Ordinal));
            Console.WriteLine(_responses);
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the AddRange mechanism.")]
        public void TestAddRange()
        {
            Init();

            string[] addee = new string[] { "String 2", "String 3" };
            _uut.AddRange(addee);

            Assert.IsTrue(_responses.Equals("m_uut_AboutToAddItems System.String[] | m_uut_AddedItems System.String[] | m_uut_ContentsChanged | ", StringComparison.Ordinal));
            Assert.IsTrue(_uut[0].Equals("String 2", StringComparison.Ordinal));
            Assert.IsTrue(_uut[1].Equals("String 3", StringComparison.Ordinal));
            Console.WriteLine(_responses);
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the Remove mechanism.")]
        public void TestRemove()
        {
            Init();

            _uut.AddRange(new string[] { "Bob", "Mary", "Sue" });
            _uut.Remove("Mary");

            Assert.IsTrue(_uut[0].Equals("Bob", StringComparison.Ordinal));
            Assert.IsTrue(_uut[1].Equals("Sue", StringComparison.Ordinal));
            Assert.IsTrue(_responses.Equals("m_uut_AboutToAddItems System.String[] | m_uut_AddedItems System.String[] | m_uut_ContentsChanged | m_uut_AboutToRemoveItem Mary | m_uut_RemovedItem Mary | m_uut_ContentsChanged | ", StringComparison.Ordinal));
            Console.WriteLine(_responses);
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the RemoveAll mechanism.")]
        public void TestRemoveAll()
        {
            Init();

            _uut.AddRange(new string[] { "Bob", "Mary", "Sue" });
            _uut.RemoveAll(delegate (string s)
            {
                return s.Length.Equals(3);
            });

            Assert.IsTrue(_uut[0].Equals("Mary", StringComparison.Ordinal));
            Assert.IsTrue(_responses.Equals("m_uut_AboutToAddItems System.String[] | m_uut_AddedItems System.String[] | m_uut_ContentsChanged | m_uut_AboutToRemoveItems System.Predicate`1[System.String] | m_uut_RemovedItems System.Predicate`1[System.String] | m_uut_ContentsChanged | ", StringComparison.Ordinal));
            Console.WriteLine(_responses);
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the RemoveAt mechanism.")]
        public void TestRemoveAt()
        {
            Init();

            _uut.AddRange(new string[] { "Bob", "Mary", "Sue" });
            _uut.RemoveAt(1);

            Assert.IsTrue(_uut[0].Equals("Bob", StringComparison.Ordinal));
            Assert.IsTrue(_uut[1].Equals("Sue", StringComparison.Ordinal));
            Assert.IsTrue(_responses.Equals("m_uut_AboutToAddItems System.String[] | m_uut_AddedItems System.String[] | m_uut_ContentsChanged | m_uut_AboutToRemoveItem Mary | m_uut_RemovedItem Mary | m_uut_ContentsChanged | ", StringComparison.Ordinal));
            Console.WriteLine(_responses);
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the Clear mechanism.")]
        public void TestClear()
        {
            Init();

            _uut.AddRange(new string[] { "Bob", "Mary", "Sue" });
            _uut.Clear();

            Assert.IsTrue(_uut.Count == 0);
            Assert.IsTrue(_responses.Equals("m_uut_AboutToAddItems System.String[] | m_uut_AddedItems System.String[] | m_uut_ContentsChanged | m_uut_ContentsChanged | ", StringComparison.Ordinal));
            Console.WriteLine(_responses);
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the Indexer mechanism.")]
        public void TestIndexer()
        {
            Init();

            _uut.AddRange(new string[] { "Bob", "Mary", "Sue" });
            _uut[1] = "Steve";

            Assert.AreEqual("Bob", _uut[0]);
            Assert.AreEqual("Steve", _uut[1]);
            Assert.AreEqual("Sue", _uut[2]);
            Assert.AreEqual("m_uut_AboutToAddItems System.String[] | m_uut_AddedItems System.String[] | m_uut_ContentsChanged | m_uut_AboutToReplaceItem Mary with Steve | m_uut_ReplacedItem Mary with Steve | m_uut_ContentsChanged | ", _responses);
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the Insert mechanism.")]
        public void TestInsert()
        {
            Init();

            _uut.AddRange(new string[] { "Bob", "Mary", "Sue" });

            _uut.Insert(1, "Paul");

            Assert.AreEqual("Bob", _uut[0]);
            Assert.AreEqual("Paul", _uut[1]);
            Assert.AreEqual("Mary", _uut[2]);
            Assert.AreEqual("Sue", _uut[3]);
            Assert.AreEqual(
                "m_uut_AboutToAddItems System.String[] | m_uut_AddedItems System.String[] | m_uut_ContentsChanged | m_uut_AboutToAddItem Paul | m_uut_AddedItem Paul | m_uut_ContentsChanged | ",
                _responses
                );
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the InsertRange mechanism.")]
        public void TestInsertRange()
        {
            Init();

            _uut.AddRange(new string[] { "Bob", "Mary", "Tim" });

            _uut.InsertRange(1, new string[] { "Paul", "Randy", "Sara" });

            Assert.IsTrue(_uut[0].Equals("Bob", StringComparison.Ordinal));
            Assert.IsTrue(_uut[1].Equals("Paul", StringComparison.Ordinal));
            Assert.IsTrue(_uut[2].Equals("Randy", StringComparison.Ordinal));
            Assert.IsTrue(_uut[3].Equals("Sara", StringComparison.Ordinal));
            Assert.IsTrue(_uut[4].Equals("Mary", StringComparison.Ordinal));
            Assert.IsTrue(_uut[5].Equals("Tim", StringComparison.Ordinal));
            Assert.IsTrue(_responses.Equals("m_uut_AboutToAddItems System.String[] | m_uut_AddedItems System.String[] | m_uut_ContentsChanged | m_uut_AboutToAddItems System.String[] | m_uut_AddedItems System.String[] | m_uut_ContentsChanged | ", StringComparison.Ordinal));
            Console.WriteLine(_responses);
        }


    }
}