/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
#pragma warning disable 1587

// TODO: Code coverage to test, and then remove these disables.
// ReSharper disable SuspiciousTypeConversion.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global


/// <summary>
/// The Cost namespace contains elements that are still works-in-progress. It should be used with caution.
/// </summary>
namespace Highpoint.Sage.Scheduling.Cost
{

    public class Cost<T> : TreeNode<Cost<T>> where T : IHasCost<T>, IHasName
    {

        private static readonly bool diagnostics = false;
        private bool _initialized;
        private readonly IHasCost<T> _master;
        private readonly List<CostCategory<T>> _categories;

        public Cost(IHasCost<T> master, IEnumerable<CostCategory<T>> categories)
        {
            _master = master;
            _categories = new List<CostCategory<T>>();
            foreach (CostCategory<T> category in categories)
            {
                _categories.Add(category.Clone());
            }
        }

        CostCategory<T> GetMatchingCategory(CostCategory<T> exemplar)
        {
            return _categories.Single(n => n.Name.Equals(exemplar.Name, StringComparison.Ordinal));
        }

        public IEnumerable<CostCategory<T>> Categories => _categories;

        public CostCategory<T> this[string categoryName]
        {
            get
            {
                return _categories.Single(n => n.Name.Equals(categoryName, StringComparison.Ordinal));
            }
        }

        public double Total
        {
            get
            {
                return _categories.Sum(n => n.Total);
            }
        }

        public bool Initialized
        {
            get
            {
                return _initialized;
            }
            set
            {
                _initialized = value;
            }
        }

        public void Reset()
        {
            foreach (CostCategory<T> category in Categories)
            {
                category.Reset();
                _initialized = false;
            }
        }

        public void Subsume()
        {
            foreach (CostCategory<T> category in Categories)
            {
                if (category.Inheritable)
                {
                    // Pull inheritable costs up.
                    foreach (Cost<T> child in _master.CostChildren.Select(n => n.Cost))
                    {
                        CostCategory<T> childsCategory = child.GetMatchingCategory(category);
                        if (diagnostics)
                        {
                            Console.WriteLine("{0} is inheriting {1} cost {2} from {3}.",
                                ((IHasName)_master).Name,
                                category.Name,
                                childsCategory.InheritableCost,
                                ((IHasName)child._master).Name);
                        }
                        category.InheritedCost += (childsCategory.InheritableCost);
                    }
                }
            }
        }
        public void Apportion()
        {
            foreach (CostCategory<T> category in _categories)
            {
                if (category.Apportionable)
                {
                    // Push my apportionable costs down
                    foreach (Cost<T> child in _master.CostChildren.Select(n => n.Cost))
                    {
                        CostCategory<T> childsCategory = child.GetMatchingCategory(category);
                        double portion = childsCategory.ApportionmentFraction((T)child._master);
                        if (diagnostics)
                        {
                            Console.WriteLine("{0} is assigning {1:0%} of its {2} apportionable cost {3} to {4}.",
                                ((IHasName)_master).Name,
                                portion,
                                category.Name,
                                category.ApportionableCost,
                                ((IHasName)child._master).Name);
                        }
                        childsCategory.ApportionedCost = portion * category.ApportionableCost;
                    }
                }
            }
        }


        public void Reconcile(bool startAtRoot = true)
        {
            if (startAtRoot && _master.CostParent != null)
            {
                _master.CostRoot.Cost.Reconcile();
            }
            else
            {
                // I'm the root. Reconcile all below me.
                _Reconcile();
            }
        }
        private void _Reconcile()
        {

            foreach (Cost<T> child in _master.CostChildren.Select(n => n.Cost))
            {
                foreach (CostCategory<T> childsCategory in child._categories)
                {
                    childsCategory.Clear();
                }
            }
            #region Apportion each category downward
            Apportion();
            #endregion

            #region Reconcile Children
            foreach (Cost<T> child in _master.CostChildren.Select(n => n.Cost))
            {
                child._Reconcile();
            }
            #endregion

            #region Inherit each category upward
            Subsume();
            #endregion

            //m_valid = true;
        }
    }
}
