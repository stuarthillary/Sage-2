/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.ItemBased
{
    public interface IPeriodicity
    {
        TimeSpan GetNext();
    }
}