/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using System;

namespace Highpoint.Sage.Scheduling
{
    public interface ISupportsCorrelation : IHasIdentity
    {
        Guid ParentGuid
        {
            get;
        }
        int InstanceCount
        {
            get;
        }
    }
}
