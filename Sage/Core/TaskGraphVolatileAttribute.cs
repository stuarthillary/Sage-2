/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// Used to decorate the key or the value for anything that is going to be put 
    /// into the task graph that must be cleared out for each new run.
    /// </summary>
    public class TaskGraphVolatileAttribute : Attribute
    {
    }
}
