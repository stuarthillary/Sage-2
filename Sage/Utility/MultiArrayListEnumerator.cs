/* This source code licensed under the GNU Affero General Public License */
using System.Collections;

namespace Highpoint.Sage.Utility
{
    /// <summary>
    /// Summary description for MultiArrayListEnumerator.
    /// </summary>
    public class MultiArrayListEnumerator : IEnumerator
	{
		private readonly ArrayList[] _arrayLists;
		private int _alCursor;
		private IEnumerator _enumerator;

		public MultiArrayListEnumerator(ArrayList[] arrayLists)
		{
			_arrayLists = arrayLists;
			Reset();
		}

#region Implementation of IEnumerator

		public void Reset() {
			_alCursor = -1;
			GetNextEnumerator();
		}

		public bool MoveNext() {
			if ( _enumerator != null ) {
				if ( !_enumerator.MoveNext() ) {
					_enumerator = GetNextEnumerator();
					if ( _enumerator == null ) return false;
					return _enumerator.MoveNext();
				} else {
					return true;
				}
			}
			return false;
		}
		public object Current => _enumerator?.Current;

#endregion

		private IEnumerator GetNextEnumerator(){
			if ( _arrayLists.Length > (_alCursor+1) ) {
				_alCursor++;
				_enumerator = _arrayLists[_alCursor].GetEnumerator();
			} else {
				_enumerator = null;
				_alCursor = -1;
			}
			return _enumerator;
		}
	}
}
