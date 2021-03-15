/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Highpoint.Sage.Scheduling.Cost
{

    /// <summary>
    /// Summary description for zTestCost.
    /// </summary>
    [TestClass]
    public class zTestCost1
    {
        public zTestCost1()
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

        private Thing _alice, _bob, _charlie, _dave, _edna, _frank, _george, _harry;



        [TestMethod]
        public void TestCostBasics1()
        {

            SetUpTree();

            ((IHasCost<Thing>)_alice).Cost["Personnel"].DirectCost = 100.0;
            ((IHasCost<Thing>)_dave).Cost["Equipment"].DirectCost = 8.0;

            ((IHasCost<Thing>)_dave).Cost.Reconcile();

            DumpCostData(_alice);
        }

        [TestMethod]
        public void TestCostBasics2()
        {

            SetUpTree();

            ((IHasCost<Thing>)_alice).Cost["Personnel"].DirectCost = 100.0;
            ((IHasCost<Thing>)_bob).Cost["Personnel"].DirectCost = 90.0;
            ((IHasCost<Thing>)_charlie).Cost["Personnel"].DirectCost = 80.0;
            ((IHasCost<Thing>)_dave).Cost["Personnel"].DirectCost = 70.0;
            ((IHasCost<Thing>)_edna).Cost["Personnel"].DirectCost = 60.0;
            ((IHasCost<Thing>)_frank).Cost["Personnel"].DirectCost = 50.0;
            ((IHasCost<Thing>)_george).Cost["Personnel"].DirectCost = 40.0;

            ((IHasCost<Thing>)_alice).Cost["Equipment"].DirectCost = 2.0;
            ((IHasCost<Thing>)_bob).Cost["Equipment"].DirectCost = 4.0;
            ((IHasCost<Thing>)_charlie).Cost["Equipment"].DirectCost = 6.0;
            ((IHasCost<Thing>)_dave).Cost["Equipment"].DirectCost = 8.0;
            ((IHasCost<Thing>)_edna).Cost["Equipment"].DirectCost = 10.0;
            ((IHasCost<Thing>)_frank).Cost["Equipment"].DirectCost = 12.0;
            ((IHasCost<Thing>)_george).Cost["Equipment"].DirectCost = 14.0;

            ((IHasCost<Thing>)_edna).Cost["Training"].DirectCost = 77.0;

            ((IHasCost<Thing>)_dave).Cost.Reconcile();

            DumpCostData(_alice);
        }

        private void SetUpTree()
        {
            _alice = new Thing("Alice");
            _bob = new Thing("Bob");
            _charlie = new Thing("Charlie");
            _dave = new Thing("Dave");
            _edna = new Thing("Edna");
            _frank = new Thing("Frank");
            _george = new Thing("George");
            _harry = new Thing("Harry");

            _alice.AddChild(_bob);
            _alice.AddChild(_charlie);
            _alice.AddChild(_dave);
            _dave.AddChild(_edna);
            _dave.AddChild(_frank);
            _edna.AddChild(_george);
        }

        private void DumpCostData(Thing alice)
        {
            _DumpCostData(alice, 0);
        }

        private void _DumpCostData(Thing thing, int indentLevel)
        {
            Console.WriteLine("{0}{1} - total cost {2}", StringOperations.Spaces(indentLevel * 3), thing.Name, thing.Cost.Total);
            foreach (string categoryName in Thing.COST_CATEGORIES.Select(n => n.Name))
            {
                CostCategory<Thing> category = ((IHasCost<Thing>)thing).Cost[categoryName];
                Console.WriteLine("{0}{1} : {2:F2}\t{3:F2}\t{4:F2}", StringOperations.Spaces(15), category.Name, category.InheritedCost, category.DirectCost, category.ApportionedCost);
            }
            foreach (Thing child in thing.Children)
            {
                _DumpCostData(child, indentLevel + 1);
            }
        }

        class Thing : TreeNode<Thing>, IHasCost<Thing>, IHasName
        {
            public static List<CostCategory<Thing>> COST_CATEGORIES = new List<CostCategory<Thing>>()
            {   new CostCategory<Thing>("Personnel",true, true, n=>1.0/n.Children.Count()),
                new CostCategory<Thing>("Equipment",true, true, n=>1.0/n.Children.Count()),
                new CostCategory<Thing>("Training",true, true, n=>1.0/n.Children.Count()),
                new CostCategory<Thing>("Material ",true, true, n=>1.0/n.Children.Count())};

            private Cost<Thing> _cost;
            private string _name;

            public Thing(string name)
            {
                _name = name;
                _cost = new Cost<Thing>(this, COST_CATEGORIES);
                IsSelfReferential = true;
            }

            public string Name
            {
                get
                {
                    return _name;
                }
            }


            public Cost<Thing> Cost
            {
                get
                {
                    return _cost;
                }
            }
            public IHasCost<Thing> CostParent
            {
                get
                {
                    return (IHasCost<Thing>)Parent;
                }
            }
            public IEnumerable<IHasCost<Thing>> CostChildren
            {
                get
                {
                    foreach (IHasCost<Thing> thing in Children)
                        yield return thing;
                }
            }
            public IHasCost<Thing> CostRoot
            {
                get
                {
                    return Parent.Root.Payload;
                }
            }

        }

    }
}
