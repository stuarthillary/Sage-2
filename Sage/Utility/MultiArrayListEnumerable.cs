/* This source code licensed under the GNU Affero General Public License */
using System.Collections;

namespace Highpoint.Sage.Utility
{
    public class MultiArrayListEnumerable : IEnumerable {
		private readonly ArrayList[] _arraylists;

		public MultiArrayListEnumerable(ArrayList[] arrayLists){
			_arraylists = arrayLists;
		}
#region IEnumerable Members

		public IEnumerator GetEnumerator() {
			return new MultiArrayListEnumerator(_arraylists);
		}

#endregion

	}
}
