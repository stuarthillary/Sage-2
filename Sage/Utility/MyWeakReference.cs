/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.Utility
{
    internal class MyWeakReference : WeakReference
    {
        public MyWeakReference(object obj) : base(obj) { }

        public override int GetHashCode()
        {
            return Target.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return Target.Equals(obj);
        }
    }
}