/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.SimCore;
using System.Collections.Generic;
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
    public interface IHasCost<T> where T : IHasCost<T>, IHasName
    {
        Cost<T> Cost
        {
            get;
        }
        IHasCost<T> CostParent
        {
            get;
        }
        IEnumerable<IHasCost<T>> CostChildren
        {
            get;
        }
        IHasCost<T> CostRoot
        {
            get;
        }
    }
}
