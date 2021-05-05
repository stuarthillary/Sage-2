/* This source code licensed under the GNU Affero General Public License */
//#define PREANNOUNCE

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Highpoint.Sage.Utility
{
    /// <summary>
    /// Summary description for TreeNodeTester.
    /// </summary>
    [TestClass]
    public class TreeNodeTester
    {
        public TreeNodeTester()
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

        /// <summary>
        /// This test manipulates a tree that holds primitive elements that have no knowledge of their participation in a tree.
        /// </summary>
        [TestMethod]
        public void TestTreeConstructionAndTraversalMethodsOverPrimitiveTree()
        {

            TreeNode<string> alice = new TreeNode<string>("Alice");
            ITreeNode<string> bob = alice.AddChild("Bob");
            ITreeNode<string> charlie = alice.AddChild("Charlie");
            ITreeNode<string> dingus = alice.AddChild("Dingus");

            ITreeNode<string> ethel = bob.AddChild("Ethel");
            ITreeNode<string> frank = bob.AddChild("Frank");
            ITreeNode<string> george = bob.AddChild("George");

            StringBuilder sb = new StringBuilder();
            foreach (ITreeNode<string> tns in alice.DescendantNodesBreadthFirst(true))
            {
                sb.AppendLine(tns.Payload);
            }
            foreach (ITreeNode<string> tns in alice.DescendantNodesDepthFirst(true))
            {
                sb.AppendLine(tns.Payload);
            }
            foreach (string s in alice.DescendantsBreadthFirst(true))
            {
                sb.AppendLine(s);
            }
            foreach (string s in alice.DescendantsDepthFirst(true))
            {
                sb.AppendLine(s);
            }

            Console.WriteLine(
                "This test manipulates a tree that holds elements that have no knowledge of their participation in a tree.");
            Console.WriteLine(sb.ToString());
            Console.WriteLine();
            Assert.IsTrue(StripCRLF(sb.ToString()).Equals(StripCRLF(REQUIRED_ITERATIONSTRING1), StringComparison.Ordinal),
                "Expected \"" + REQUIRED_ITERATIONSTRING1 + "\", but got \"" + sb.ToString() + "\" instead.");
        }

        /// <summary>
        /// This test manipulates a tree that holds primitive elements that have no knowledge of their participation in a tree, through the use of NodeWrappers.
        /// </summary>
        [TestMethod]
        public void TestTreeConstructionAndTraversalMethodsOverNodeWrapperTree()
        {

            TreeNode<StringWrapper> alice = new TreeNode<StringWrapper>(new StringWrapper("Alice"));
            ITreeNode<StringWrapper> bob = alice.AddChild(new StringWrapper("Bob"));
            ITreeNode<StringWrapper> charlie = alice.AddChild(new StringWrapper("Charlie"));
            ITreeNode<StringWrapper> dingus = alice.AddChild(new StringWrapper("Dingus"));

            ITreeNode<StringWrapper> ethel = bob.AddChild(new StringWrapper("Ethel"));
            ITreeNode<StringWrapper> frank = bob.AddChild(new StringWrapper("Frank"));
            ITreeNode<StringWrapper> george = bob.AddChild(new StringWrapper("George"));

            StringBuilder sb = new StringBuilder();
            foreach (ITreeNode<StringWrapper> tns in alice.DescendantNodesBreadthFirst(true))
            {
                sb.AppendLine(tns.Payload.ToString());
            }
            foreach (ITreeNode<StringWrapper> tns in alice.DescendantNodesDepthFirst(true))
            {
                sb.AppendLine(tns.Payload.ToString());
            }
            foreach (StringWrapper s in alice.DescendantsBreadthFirst(true))
            {
                sb.AppendLine(s.ToString());
            }
            foreach (StringWrapper s in alice.DescendantsDepthFirst(true))
            {
                sb.AppendLine(s.ToString());
            }

            Console.WriteLine(
                "This test manipulates a tree that holds elements that have no knowledge of their participation in a tree.");
            Console.WriteLine(sb.ToString());
            Console.WriteLine();
            Assert.IsTrue(StripCRLF(sb.ToString()).Equals(StripCRLF(REQUIRED_ITERATIONSTRING1), StringComparison.Ordinal),
                "Expected \"" + REQUIRED_ITERATIONSTRING1 + "\", but got \"" + sb.ToString() + "\" instead.");
        }

        /// <summary>
        /// This test manipulates a tree that holds elements that derive from TreeNode.
        /// </summary>
        [TestMethod]
        public void TestTreeConstructionAndTraversalMethodsOverTreeOfNodeDerivedObjects()
        {

            Activity alice = new Activity("Alice");
            Activity bob = new Activity("Bob");
            Activity charlie = new Activity("Charlie");
            Activity dingus = new Activity("Dingus");
            Activity ethel = new Activity("Ethel");
            Activity frank = new Activity("Frank");
            Activity george = new Activity("George");

            alice.AddChild(bob);
            alice.AddChild(charlie);
            alice.AddChild(dingus);

            bob.AddChild(ethel);
            bob.AddChild(frank);
            bob.AddChild(george);

            StringBuilder sb = new StringBuilder();
            foreach (Activity a in alice.DescendantNodesBreadthFirst(true))
            {
                sb.AppendLine(a.Name);
            }
            foreach (Activity a in alice.DescendantNodesDepthFirst(true))
            {
                sb.AppendLine(a.Name);
            }
            foreach (Activity a in alice.DescendantsBreadthFirst(true))
            {
                sb.AppendLine(a.Name);
            }
            foreach (Activity a in alice.DescendantsDepthFirst(true))
            {
                sb.AppendLine(a.Name);
            }

            Console.WriteLine("This test manipulates a tree that holds elements that derive from TreeNode.");
            Console.WriteLine(sb.ToString());
            Console.WriteLine();
            Assert.IsTrue(StripCRLF(sb.ToString()).Equals(StripCRLF(REQUIRED_ITERATIONSTRING1), StringComparison.Ordinal));

        }

        /// <summary>
        /// This test manipulates a tree that holds elements that implement ITreeNode.
        /// </summary>
        [TestMethod]
        public void TestTreeConstructionAndTraversalMethodsOverTreeOfNodeITreeNodeImplementingObjects()
        {

            Activity2 alice = new Activity2("Alice");
            Activity2 bob = new Activity2("Bob");
            Activity2 charlie = new Activity2("Charlie");
            Activity2 dingus = new Activity2("Dingus");
            Activity2 ethel = new Activity2("Ethel");
            Activity2 frank = new Activity2("Frank");
            Activity2 george = new Activity2("George");

            alice.AddChild(bob);
            alice.AddChild(charlie);
            alice.AddChild(dingus);

            bob.AddChild(ethel);
            bob.AddChild(frank);
            bob.AddChild(george);

            StringBuilder sb = new StringBuilder();

            foreach (ITreeNode<Activity2> a in alice.DescendantNodesBreadthFirst(true))
            {
                sb.AppendLine(a.Payload.Name);
            }
            foreach (ITreeNode<Activity2> a in alice.DescendantNodesDepthFirst(true))
            {
                sb.AppendLine(a.Payload.Name);
            }
            foreach (Activity2 a in alice.DescendantsBreadthFirst(true))
            {
                sb.AppendLine(a.Name);
            }
            foreach (Activity2 a in alice.DescendantsDepthFirst(true))
            {
                sb.AppendLine(a.Name);
            }

            Console.WriteLine("This test manipulates a tree that holds elements that implement ITreeNode.");
            Console.WriteLine(sb.ToString());
            Console.WriteLine();
            Assert.IsTrue(StripCRLF(sb.ToString()).Equals(StripCRLF(REQUIRED_ITERATIONSTRING1), StringComparison.Ordinal));

        }

        #region REQUIRED_ITERATIONSTRING1

        private static string REQUIRED_ITERATIONSTRING1 =
            @"Alice
Bob
Charlie
Dingus
Ethel
Frank
George
Alice
Bob
Ethel
Frank
George
Charlie
Dingus
Alice
Bob
Charlie
Dingus
Ethel
Frank
George
Alice
Bob
Ethel
Frank
George
Charlie
Dingus";

        #endregion


        [TestMethod]
        public void TestCreateCircularTree()
        {
            try
            {
                TreeNode<string> alice = new TreeNode<string>("Alice");
                ITreeNode<string> bob = alice.AddChild("Bob");
                ITreeNode<string> charlie = bob.AddChild("Charlie");
                ITreeNode<string> dingus = bob.AddChild("Dingus");
                dingus.AddChild(bob);

                Assert.IsTrue(false, "Circular tree structure was not caught.");
            }
            catch (ArgumentException)
            {

            }
        }

        [TestMethod]
        public void TestChildSorting()
        {

            Activity alice = new Activity("Alice");
            Activity bob = new Activity("Bob");
            Activity charlie = new Activity("Charlie");
            Activity dingus = new Activity("Dingus");
            Activity ethel = new Activity("Ethel");
            Activity frank = new Activity("Frank");
            Activity george = new Activity("George");

            alice.AddChild(bob);
            alice.AddChild(charlie);
            alice.AddChild(dingus);

            bob.AddChild(ethel);
            bob.AddChild(frank);
            bob.AddChild(george);

            string s = string.Empty;
            bob.ForEachChild(delegate (ITreeNode<Activity> activity)
            {
                s += activity.Payload.Name;
            });
            Assert.IsTrue(s.Equals("EthelFrankGeorge", StringComparison.Ordinal));

            s = string.Empty;
            bob.SortChildren(new Comparison<ITreeNode<Activity>>(ReverseSortTreeNodeActivities));
            bob.ForEachChild(delegate (ITreeNode<Activity> activity)
            {
                s += activity.Payload.Name;
            });
            Assert.IsTrue(s.Equals("GeorgeFrankEthel", StringComparison.Ordinal));

            Console.WriteLine(s);

        }

        private int ReverseSortTreeNodeActivities(ITreeNode<Activity> tn1, ITreeNode<Activity> tn2)
        {
            return string.Compare(tn2.Payload.Name, tn1.Payload.Name, StringComparison.Ordinal);
        }

        [TestMethod]

        public void TestNodeRemoval()
        {

            TreeNode<string> alice = new TreeNode<string>("Alice");
            ITreeNode<string> bob = alice.AddChild("Bob");
            ITreeNode<string> charlie = alice.AddChild("Charlie");
            ITreeNode<string> dingus = alice.AddChild("Dingus");

            ITreeNode<string> ethel = bob.AddChild("Ethel");
            ITreeNode<string> frank = charlie.AddChild("Frank");
            ITreeNode<string> george = dingus.AddChild("George");

            Assert.AreEqual(bob.Parent, alice, "\"bob\"'s parent should be \"alice\", but isn't.");
            Assert.IsTrue(alice.HasChild(bob), "\"alice\"'s children should include \"bob\", but doesn't.");
            Assert.IsTrue(alice.RemoveChild(bob), "\"alice\" failed to remove existing child \"bob\"");
            Assert.IsFalse(alice.HasChild(bob), "\"alice\"'s children should not include \"bob\", but still does.");
            Assert.AreEqual(bob.Parent, null, "\"bob\"'s parent should be null, but isn't.");

            Assert.IsFalse(dingus.RemoveChild(bob), "\"dingus\" claimed success in removing existing child \"bob\"");

        }




        [TestMethod]
        public void TestNodeRemoval2()
        {

            TreeNode<string> alice = new TreeNode<string>("Alice");
            ITreeNode<string> bob = alice.AddChild("Bob");
            ITreeNode<string> charlie = alice.AddChild("Charlie");
            ITreeNode<string> dingus = alice.AddChild("Dingus");

            ITreeNode<string> ethel = bob.AddChild("Ethel");
            ITreeNode<string> frank = bob.AddChild("Frank");
            ITreeNode<string> george = bob.AddChild("George");

            bob.RemoveChild(frank);

            StringBuilder sb = new StringBuilder();
            foreach (ITreeNode<string> tns in alice.DescendantNodesBreadthFirst(true))
            {
                sb.AppendLine(tns.Payload);
            }
            foreach (ITreeNode<string> tns in alice.DescendantNodesDepthFirst(true))
            {
                sb.AppendLine(tns.Payload);
            }
            foreach (string s in alice.DescendantsBreadthFirst(true))
            {
                sb.AppendLine(s);
            }
            foreach (string s in alice.DescendantsDepthFirst(true))
            {
                sb.AppendLine(s);
            }

            Console.WriteLine("This test manipulates a tree that holds elements that have no knowledge of their participation in a tree, but with tree restructuring.");
            Console.WriteLine(sb.ToString());
            Console.WriteLine();
            Assert.AreEqual(StripCRLF(sb.ToString()), StripCRLF(REQUIRED_ITERATIONSTRING2), "Expected \"" + REQUIRED_ITERATIONSTRING2 + "\", but got \"" + sb.ToString() + "\" instead.");
        }

        private string StripCRLF(string structureString) => structureString.Replace("\r", "", StringComparison.Ordinal).Replace("\n", "", StringComparison.Ordinal);

        #region REQUIRED_ITERATIONSTRING2
        private static string REQUIRED_ITERATIONSTRING2 =
@"Alice
Bob
Charlie
Dingus
Ethel
George
Alice
Bob
Ethel
George
Charlie
Dingus
Alice
Bob
Charlie
Dingus
Ethel
George
Alice
Bob
Ethel
George
Charlie
Dingus
";

        #endregion    

        class StringWrapper
        {
            private readonly string _string = null;
            public StringWrapper(string s)
            {
                _string = s;
            }
            public override string ToString()
            {
                return _string;
            }
        }

        class Activity : TreeNode<Activity>, IComparable<Activity>
        {

            private readonly string _name = null;

            public Activity(string name)
                : base(default(Activity))
            {
                _name = name;
                SetPayload(this);
            }

            public string Name
            {
                get
                {
                    return _name;
                }
            }


            #region IComparable<Activity> Members

            public int CompareTo(Activity other)
            {
                return Comparer.Default.Compare(this.Name.Length, other.Name.Length);
            }

            #endregion
        }

        class Activity2 : ITreeNode<Activity2>
        {
            private readonly TreeNode<Activity2> _treeNode;
            private readonly string _name = null;

            public Activity2(string name)
            {
                _name = name;
                _treeNode = new TreeNode<Activity2>(this);

#if PREANNOUNCE
            //_treeNode.AboutToGainChild += new TreeNodeEvent<Activity2>(treeNode_AboutToGainChild);
            //_treeNode.AboutToGainParent += new TreeNodeEvent<Activity2>(treeNode_AboutToGainParent);
            //_treeNode.AboutToLoseChild += new TreeNodeEvent<Activity2>(treeNode_AboutToLoseChild);
            //_treeNode.AboutToLoseParent += new TreeNodeEvent<Activity2>(treeNode_AboutToLoseParent);
#endif
                _treeNode.GainedChild += new TreeNodeEvent<Activity2>(treeNode_GainedChild);
                _treeNode.GainedParent += new TreeNodeEvent<Activity2>(treeNode_GainedParent);
                _treeNode.LostChild += new TreeNodeEvent<Activity2>(treeNode_LostChild);
                _treeNode.LostParent += new TreeNodeEvent<Activity2>(treeNode_LostParent);
                _treeNode.SubtreeChanged += new TreeChangeEvent<Activity2>(treeNode_SubtreeChanged);
                _treeNode.ChildrenResorted += new TreeNodeEvent<Activity2>(treeNode_ChildrenResorted);
            }

            void treeNode_ChildrenResorted(ITreeNode<Activity2> self, ITreeNode<Activity2> subject)
            {
                if (ChildrenResorted != null)
                {
                    ChildrenResorted(self.Payload, subject.Payload);
                }
            }

            void treeNode_SubtreeChanged(SubtreeChangeType changeType, ITreeNode<Activity2> where)
            {
                if (SubtreeChanged != null)
                {
                    SubtreeChanged(changeType, where.Payload);
                }
            }

            void treeNode_LostParent(ITreeNode<Activity2> self, ITreeNode<Activity2> subject)
            {
                if (LostParent != null)
                {
                    LostParent(self.Payload, subject.Payload);
                }
            }

            void treeNode_LostChild(ITreeNode<Activity2> self, ITreeNode<Activity2> subject)
            {
                if (LostChild != null)
                {
                    LostChild(self.Payload, subject.Payload);
                }
            }

            void treeNode_GainedParent(ITreeNode<Activity2> self, ITreeNode<Activity2> subject)
            {
                if (GainedParent != null)
                {
                    GainedParent(self.Payload, subject.Payload);
                }
            }

            void treeNode_GainedChild(ITreeNode<Activity2> self, ITreeNode<Activity2> subject)
            {
                if (GainedChild != null)
                {
                    GainedChild(self.Payload, subject.Payload);
                }
            }

#if PREANNOUNCE
        void treeNode_AboutToLoseParent(ITreeNode<Activity2> self, ITreeNode<Activity2> subject) {
            if (AboutToLoseParent != null) {
                AboutToLoseParent(self.Payload, subject.Payload);
            }
        }

        void treeNode_AboutToLoseChild(ITreeNode<Activity2> self, ITreeNode<Activity2> subject) {
            if (AboutToLoseChild != null) {
                AboutToLoseChild(self.Payload, subject.Payload);
            }
        }

        void treeNode_AboutToGainParent(ITreeNode<Activity2> self, ITreeNode<Activity2> subject) {
            if (AboutToGainParent != null) {
                AboutToGainParent(self.Payload, subject.Payload);
            }
        }

        void treeNode_AboutToGainChild(ITreeNode<Activity2> self, ITreeNode<Activity2> subject) {
            if (AboutToGainChild != null) {
                AboutToGainChild(self.Payload, subject.Payload);
            }
        }
#endif

            public string Name
            {
                get
                {
                    return _name;
                }
            }

            public override string ToString()
            {
                return _name;
            }

            #region ITreeNode<Activity2> Members
#if PREANNOUNCE
        public event TreeNodeEvent<Activity2> AboutToLoseParent;
        public event TreeNodeEvent<Activity2> AboutToGainParent;
        public event TreeNodeEvent<Activity2> AboutToLoseChild;
        public event TreeNodeEvent<Activity2> AboutToGainChild;
#endif

            public event TreeNodeEvent<Activity2> LostParent;

            public event TreeNodeEvent<Activity2> GainedParent;

            public event TreeNodeEvent<Activity2> LostChild;

            public event TreeNodeEvent<Activity2> GainedChild;

            public event TreeNodeEvent<Activity2> ChildrenResorted;

            public event TreeChangeEvent<Activity2> SubtreeChanged;


            public ITreeNode<Activity2> Root
            {
                get
                {
                    return _treeNode.Root;
                }
            }

            public Activity2 Payload
            {
                get
                {
                    return _treeNode.Payload;
                }
            }

            public ITreeNode<Activity2> Parent
            {
                get
                {
                    return _treeNode.Parent;
                }
                set
                {
                    _treeNode.Parent = value;
                }
            }

            public IEnumerable<ITreeNode<Activity2>> Children
            {
                get
                {
                    return _treeNode.Children;
                }
            }

            public IEnumerable<ITreeNode<Activity2>> DescendantNodesBreadthFirst(bool includeSelf)
            {
                return _treeNode.DescendantNodesBreadthFirst(includeSelf);
            }

            public IEnumerable<ITreeNode<Activity2>> DescendantNodesDepthFirst(bool includeSelf)
            {
                return _treeNode.DescendantNodesDepthFirst(includeSelf);
            }

            public IEnumerable<Activity2> DescendantsBreadthFirst(bool includeSelf)
            {
                return _treeNode.DescendantsBreadthFirst(includeSelf);
            }

            public IEnumerable<Activity2> DescendantsDepthFirst(bool includeSelf)
            {
                return _treeNode.DescendantsDepthFirst(includeSelf);
            }

            public IEnumerable<Activity2> Siblings(bool includeSelf)
            {
                return _treeNode.Siblings(includeSelf);
            }

            public bool IsChildOf(ITreeNode<Activity2> possibleParentNode)
            {
                return _treeNode.IsChildOf(possibleParentNode);
            }

            public ITreeNodeEventController<Activity2> MyEventController
            {
                get
                {
                    return _treeNode.MyEventController;
                }
            }

            public void SetParent(ITreeNode<Activity2> newParent, bool skipStructureChecking, bool childAlreadyAdded = false)
            {
                _treeNode.SetParent(newParent, skipStructureChecking, childAlreadyAdded);
            }

            public ITreeNode<Activity2> AddChild(Activity2 newChild, bool skipStructuralChecking = false)
            {
                return _treeNode.AddChild(newChild, skipStructuralChecking);
            }

            public ITreeNode<Activity2> AddChild(ITreeNode<Activity2> newChildNode, bool skipStructuralChecking = false)
            {
                return _treeNode.AddChild(newChildNode, skipStructuralChecking);
            }

            public bool RemoveChild(Activity2 existingChild)
            {
                return _treeNode.RemoveChild(existingChild);
            }

            public bool RemoveChild(ITreeNode<Activity2> existingChild)
            {
                return _treeNode.RemoveChild(existingChild);
            }

            public void SortChildren(Comparison<ITreeNode<Activity2>> comparison)
            {
                _treeNode.SortChildren(comparison);
            }

            public void SortChildren(IComparer<ITreeNode<Activity2>> comparer)
            {
                _treeNode.SortChildren(comparer);
            }

            public bool HasChild(ITreeNode<Activity2> possibleChild)
            {
                return _treeNode.HasChild(possibleChild);
            }

            public bool HasChild(Activity2 possibleChild)
            {
                return _treeNode.HasChild(possibleChild);
            }

            public void ForEachChild(Action<Activity2> action)
            {
                _treeNode.ForEachChild(action);
            }

            public void ForEachChild(Action<ITreeNode<Activity2>> action)
            {
                _treeNode.ForEachChild(action);
            }

            public IEnumerable<Activity2> ChildNodes
            {
                get
                {
                    return _treeNode.ChildNodes;
                }
            }

            #endregion
        }
    }
}