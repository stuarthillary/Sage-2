/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.SimCore;
using System;
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
    /// <summary>
    /// Cost Categories are, e.g. Personnel, Equipment, and Materials. The same instances can be shared among many Cost elements.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CostCategory<T> where T : IHasCost<T>, IHasName
    {
        private readonly Func<T, double> _getApportionmentFraction;
        public CostCategory(string name, bool apportionable, bool inheritable, Func<T, double> getApportionmentFraction)
        {
            Name = name;
            Apportionable = apportionable;
            Inheritable = inheritable;
            _getApportionmentFraction = getApportionmentFraction;
            Clear();
        }

        public string Name
        {
            get; set;
        }
        public bool Apportionable
        {
            get; set;
        }
        public bool Inheritable
        {
            get; set;
        }

        public double DirectCost
        {
            get; set;
        }

        public double ApportionedCost
        {
            get; internal set;
        }

        public double InheritedCost
        {
            get; internal set;
        }

        public double ApportionableCost => Apportionable ? DirectCost + ApportionedCost : 0.0;
        public double InheritableCost => Inheritable ? DirectCost + InheritedCost : 0.0;

        public double Total => DirectCost + ApportionedCost + InheritedCost;

        /// <summary>
        /// Zeros all costs.
        /// </summary>
        public void Clear()
        {
            Reset();
            ApportionedCost = 0.0;
        }

        /// <summary>
        /// Zeros all derived (i.e. non-direct) costs.
        /// </summary>
        public void Reset()
        {
            InheritedCost = 0.0;
            ApportionedCost = 0.0;
        }

        public Func<T, double> ApportionmentFraction => _getApportionmentFraction;

        public CostCategory<T> Clone()
        {
            return new CostCategory<T>(Name, Apportionable, Inheritable, _getApportionmentFraction);
        }

        public override string ToString()
        {
            return $"{Name}, {(Apportionable ? "" : "not ")}apportionable, {(Inheritable ? "" : "not ")}inheritable";
        }
    }
}
