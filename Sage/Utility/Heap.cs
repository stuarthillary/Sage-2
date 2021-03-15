/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;

namespace Highpoint.Sage.Utility
{
    // A binary heap can be efficiently implemented as an array, where a node at index i 
    // has children at indexes 2i and 2i+1 and a parent at index i/2, with one-based indexing.
    public class Heap
    {
        public enum HEAP_RULE
        {
            MinValue = -1,
            MaxValue = 1
        }
        private static int _defaultInitialCapacity = 5;
        private static int _defaultGrowthFactor = 4;
        private int _nEntries;
        private readonly int _direction;
        private readonly int _growthFactor;
        private IComparable _parentEntry;
        private int _entryArraySize;
        private IComparable[] _entryArray;

        public Heap(HEAP_RULE direction, int initialCapacity, int growthFactor)
        {
            _direction = (int)direction;
            _nEntries = 0;
            _growthFactor = growthFactor;
            _parentEntry = null;
            _entryArraySize = initialCapacity;
            _entryArray = new IComparable[_entryArraySize + 1];
        }

        public Heap(HEAP_RULE direction, int initialCapacity) : this(direction, initialCapacity, _defaultGrowthFactor) { }

        public Heap(HEAP_RULE direction) : this(direction, _defaultInitialCapacity, _defaultGrowthFactor) { }

        public void Enqueue(IComparable newEntry)
        {
            int ndx = 1;
            if (_nEntries == _entryArraySize)
                GrowArray();
            if (_nEntries++ > 0)
            {
                ndx = _nEntries;
                int parentNdx = ndx / 2;
                _parentEntry = _entryArray[parentNdx];
                while (parentNdx > 0 && newEntry.CompareTo(_parentEntry) == _direction)
                {
                    _entryArray[ndx] = _parentEntry;
                    ndx = parentNdx;
                    parentNdx /= 2;
                    _parentEntry = _entryArray[parentNdx];
                }
            }
            _entryArray[ndx] = newEntry;
        }

        public int Count => _nEntries;

        public IComparable Peek()
        {
            return _entryArray[1];
        }

        public IComparable Dequeue()
        {
            if (_nEntries == 0)
                return null;
            IComparable leastEntry = _entryArray[1];
            IComparable relocatee = _entryArray[_nEntries];
            _nEntries--;
            int ndx = 1;
            int child = 2;
            while (child <= _nEntries)
            {
                if ((child < _nEntries) && _entryArray[child].CompareTo(_entryArray[child + 1]) == (-_direction))
                    child++;
                // m_entryArray[child] is the (e.g. in a minTree) lesser of the two children.
                // Therefore, if m_entryArray[child] is greater than relocatee, put Relocatee
                // in at ndx, and we're done. Otherwise, swap and drill down some more.
                if (_entryArray[child].CompareTo(relocatee) == (-_direction))
                    break;
                _entryArray[ndx] = _entryArray[child];
                ndx = child;
                child *= 2;
            }

            _entryArray[ndx] = relocatee;

            return leastEntry;
        }

        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 1; i <= _nEntries; i++)
            {
                string pt = (_entryArray[i] == null) ? "<null>" : _entryArray[i].ToString();
                string lc = "<empty>";
                if ((i * 2) <= _nEntries)
                    lc = _entryArray[i * 2].ToString();
                string rc = "<empty>";
                if (((i * 2) + 1) <= _nEntries)
                    rc = _entryArray[(i * 2) + 1].ToString();
                bool ok = (lc == "<empty>" || String.Compare(pt, lc, StringComparison.Ordinal) == _direction) && (rc == "<empty>" || String.Compare(pt, rc, StringComparison.Ordinal) == _direction);
                //if ( !ok ) System.Diagnostics.Debugger.Break();
                sb.Append("(" + i + ") " + pt + " : left Child is " + lc + ", right child is " + rc + "." + (ok ? "OK\r\n" : "NOT_OK\r\n"));
            }
            return sb.ToString();
        }


        private void GrowArray()
        {
            IComparable[] tmp = _entryArray;
            _entryArraySize *= _growthFactor;
            _entryArray = new IComparable[_entryArraySize + 1];
            Array.Copy(tmp, _entryArray, _nEntries + 1);
        }

        internal void Dump()
        {
            for (int i = 1; i <= _nEntries; i++)
            {
                string pt = (_entryArray[i] == null) ? "<null>" : _entryArray[i].ToString();
                string lc = "<empty>";
                if ((i * 2) <= _nEntries)
                    lc = _entryArray[i * 2].ToString();
                string rc = "<empty>";
                if (((i * 2) + 1) <= _nEntries)
                    rc = _entryArray[(i * 2) + 1].ToString();
                bool ok = (lc == "<empty>" || String.Compare(pt, lc, StringComparison.Ordinal) == _direction) && (rc == "<empty>" || String.Compare(pt, rc, StringComparison.Ordinal) == _direction);
                //if ( !ok ) System.Diagnostics.Debugger.Break();
                Console.WriteLine("(" + i + ") " + pt + " : left Child is " + lc + ", right child is " + rc + ". " + (ok ? "OK" : "NOT_OK"));
            }
        }
    }

    /// <summary>
    /// A binary heap can be efficiently implemented as an array, where a node at index i 
    /// has children at indexes 2i and 2i+1 and a parent at index i/2, with one-based indexing.
    /// </summary>
    /// <typeparam name="T">The type of things held in the heap.</typeparam>
    public class Heap<T>
    {

        /// <summary>
        /// Enum HEAP_RULE - MinValue builds a heap with the 
        /// </summary>
        public enum HEAP_RULE
        {
            MinValue = -1, 
            MaxValue = 1
        }

        #region Private variables.
        private static readonly int _defaultInitialCapacity = 5;
        private static readonly int _defaultGrowthFactor = 4;
        private readonly int _direction;
        private readonly int _growthFactor;
        private T _parentEntry;
        private int _entryArraySize;
        private T[] _entryArray;
        private readonly IComparer<T> _comparer;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Heap{T}"/> class.
        /// </summary>
        /// <param name="direction">The heap rule to be used for heap maintenance.</param>
        /// <param name="initialCapacity">The initial capacity.</param>
        /// <param name="growthFactor">The growth factor.</param>
        /// <param name="comparer">The comparer.</param>
        public Heap(HEAP_RULE direction, int initialCapacity, int growthFactor, IComparer<T> comparer)
        {
            _direction = (int)direction;
            Count = 0;
            _growthFactor = growthFactor;
            _parentEntry = default(T);
            _entryArraySize = initialCapacity;
            _entryArray = new T[_entryArraySize + 1];
            _comparer = comparer;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Heap{T}"/> class.
        /// </summary>
        /// <param name="direction">The heap rule to be used for heap maintenance.</param>
        /// <param name="initialCapacity">The initial capacity.</param>
        /// <param name="comparer">The comparer.</param>
        public Heap(HEAP_RULE direction, int initialCapacity, IComparer<T> comparer) : this(direction, initialCapacity, _defaultGrowthFactor, comparer) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Heap{T}"/> class.
        /// </summary>
        /// <param name="direction">The heap rule to be used for heap maintenance.</param>
        /// <param name="comparer">The comparer.</param>
        public Heap(HEAP_RULE direction, IComparer<T> comparer) : this(direction, _defaultInitialCapacity, _defaultGrowthFactor, comparer) { }

        /// <summary>
        /// Enqueues the specified new entry.
        /// </summary>
        /// <param name="newEntry">The new entry.</param>
        public void Enqueue(T newEntry)
        {
            int ndx = 1;
            if (Count == _entryArraySize)
                GrowArray();
            if (Count++ > 0)
            {
                ndx = Count;
                int parentNdx = ndx / 2;
                _parentEntry = _entryArray[parentNdx];
                while (parentNdx > 0 && _comparer.Compare(newEntry, _parentEntry) == _direction)
                {
                    _entryArray[ndx] = _parentEntry;
                    ndx = parentNdx;
                    parentNdx /= 2;
                    _parentEntry = _entryArray[parentNdx];
                }
            }
            _entryArray[ndx] = newEntry;
        }

        /// <summary>
        /// Gets the count of elements in the heap.
        /// </summary>
        /// <value>The count.</value>
        public int Count
        {
            get; private set;
        }

        /// <summary>
        /// Peeks at the instance at the top of the heap.
        /// </summary>
        /// <returns>T.</returns>
        public T Peek()
        {
            return _entryArray[1];
        }

        /// <summary>
        /// Dequeues the instance at the top of the heap.
        /// </summary>
        /// <returns>T.</returns>
        public T Dequeue()
        {
            if (Count == 0)
                return default(T);
            T leastEntry = _entryArray[1];
            T relocatee = _entryArray[Count];
            Count--;
            int ndx = 1;
            int child = 2;
            while (child <= Count)
            {
                if ((child < Count) && _comparer.Compare(_entryArray[child], _entryArray[child + 1]) == (-_direction))
                    child++;
                // m_entryArray[child] is the (e.g. in a minTree) lesser of the two children.
                // Therefore, if m_entryArray[child] is greater than relocatee, put Relocatee
                // in at ndx, and we're done. Otherwise, swap and drill down some more.
                if (_comparer.Compare(_entryArray[child], relocatee) == (-_direction))
                    break;
                _entryArray[ndx] = _entryArray[child];
                ndx = child;
                child *= 2;
            }

            _entryArray[ndx] = relocatee;

            return leastEntry;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 1; i <= Count; i++)
            {
                string pt = (_entryArray[i] == null) ? "<null>" : _entryArray[i].ToString();
                string lc = "<empty>";
                if ((i * 2) <= Count)
                    lc = _entryArray[i * 2].ToString();
                string rc = "<empty>";
                if (((i * 2) + 1) <= Count)
                    rc = _entryArray[(i * 2) + 1].ToString();
                bool ok = (lc == "<empty>" || string.Compare(pt, lc, StringComparison.Ordinal) == _direction) && (rc == "<empty>" || string.Compare(pt, rc, StringComparison.Ordinal) == _direction);
                //if ( !ok ) System.Diagnostics.Debugger.Break();
                sb.Append("(" + i + ") " + pt + " : left Child is " + lc + ", right child is " + rc + "." + (ok ? "OK\r\n" : "NOT_OK\r\n"));
            }
            return sb.ToString();
        }

        internal void Dump()
        {
            for (int i = 1; i <= Count; i++)
            {
                string String = (_entryArray[i] == null) ? "<null>" : _entryArray[i].ToString();
                string lc = "<empty>";
                if ((i * 2) <= Count)
                    lc = _entryArray[i * 2].ToString();
                string rc = "<empty>";
                if (((i * 2) + 1) <= Count)
                    rc = _entryArray[(i * 2) + 1].ToString();
                bool ok = (lc == "<empty>" || string.Compare(String, lc, StringComparison.Ordinal) == _direction) && (rc == "<empty>" || string.Compare(String, rc, StringComparison.Ordinal) == _direction);
                //if ( !ok ) System.Diagnostics.Debugger.Break();
                Console.WriteLine("(" + i + ") " + String + " : left Child is " + lc + ", right child is " + rc + ". " + (ok ? "OK" : "NOT_OK"));
            }
        }

        private void GrowArray()
        {
            T[] tmp = _entryArray;
            _entryArraySize *= _growthFactor;
            _entryArray = new T[_entryArraySize + 1];
            Array.Copy(tmp, _entryArray, Count + 1);
        }

    }


}
