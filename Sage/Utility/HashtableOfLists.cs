/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Highpoint.Sage.Utility
{

    /// <summary>
    /// Manages a hashtable of lists. This is useful for maintaining collections
    /// of keyed entries where the keys are duplicated across multiple entries.
    /// </summary>
    public class HashtableOfLists : IEnumerable
    {
        private readonly Hashtable _ht;
        private static readonly ArrayList _empty_List = ArrayList.ReadOnly(new ArrayList());

        /// <summary>
        /// Creates a hashtable of lists.
        /// </summary>
        public HashtableOfLists()
        {
            _ht = new Hashtable();
        }

        /// <summary>
        /// Adds an element with the specified key and value into the Hashtable of Lists.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="item">The value of the element to add.</param>
        public void Add(object key, object item)
        {
            object obj = _ht[key];
            if (obj == null)
            {
                _ht.Add(key, item);
            }
            else if (obj is ListWrapper)
            {
                ListWrapper lw = (ListWrapper)obj;
                if (!lw.List.Contains(item))
                    lw.List.Add(item);
            }
            else
            {
                if (!obj.Equals(item))
                { // If we're re-adding the same thing, skip it.
                    _ht.Remove(key);
                    ListWrapper lw = new ListWrapper();
                    lw.List.Add(obj);
                    lw.List.Add(item);
                    _ht.Add(key, lw);
                }
            }
        }

        /// <summary>
        /// Removes the element with the specified key from the Hashtableof Lists.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <param name="item">The value of the element to remove.</param>
        public void Remove(object key, object item)
        {
            object obj = _ht[key];
            if (obj != null)
            {
                ListWrapper wrapper = obj as ListWrapper;
                if (wrapper != null)
                {
                    wrapper.List.Remove(item);
                }
                else if (obj.Equals(item))
                {
                    _ht.Remove(key);
                }
            }
        }

        /// <summary>
        /// Removes all elements from the Hashtable of Lists.
        /// </summary>
        public void Clear()
        {
            _ht.Clear();
        }

        /// <summary>
        /// Removes all elements with the specified key from the Hashtable of Lists.
        /// </summary>
        public void Remove(object key)
        {
            _ht.Remove(key);
        }

        /// <summary>
        /// Retrieves a list of items associated with the provided key.
        /// </summary>
        public IList this[object key]
        {
            get
            {
                object obj = _ht[key];
                ListWrapper wrapper = obj as ListWrapper;
                if (wrapper != null)
                    return wrapper.List;
                ArrayList al = obj != null ? new ArrayList { obj } : _empty_List;
                return al;
            }
        }

        /// <summary>
        /// Gets the keys.
        /// </summary>
        /// <value>The keys.</value>
        public ICollection Keys => _ht.Keys;

        /// <summary>
        /// Gets the values.
        /// </summary>
        /// <value>The values.</value>
        public ICollection Values
        {
            get
            {
                ArrayList retval = new ArrayList();
                foreach (object key in _ht.Keys)
                {
                    retval.AddRange(this[key]);
                }
                return retval;
            }
        }

        /// <summary>
        /// Determines whether this hashtable of lists contains the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if this hashtable of lists contains the specified key; otherwise, <c>false</c>.</returns>
        public bool ContainsKey(object key)
        {
            return _ht.ContainsKey(key);
        }

        /// <summary>
        /// Returns an Enumerator that can iterate through all of the entries in the 
        /// Hashtable of Lists. The enumerator is an enumerator of values, not
        /// DictionaryEntries. This method first prunes empty lists from the hashtable.
        /// </summary>
        /// <returns>An Enumerator that can iterate through all of the entries in the 
        /// Hashtable of Lists.</returns>
        public IEnumerator GetEnumerator()
        {
            PruneEmptyLists();
            return new HtolEnumerator(this);
        }

        /// <summary>
        /// Removes any entries in the HTOL that comprise a key and an empty list (which can
        /// result from removals of entries.)
        /// </summary>
        public void PruneEmptyLists()
        {
            ArrayList removees = new ArrayList();
            foreach (DictionaryEntry de in _ht)
            {
                ListWrapper value = de.Value as ListWrapper;
                if (value != null && value.List.Count == 0)
                    removees.Add(de.Key);
            }
            foreach (object key in removees)
                _ht.Remove(key);
        }

        /// <summary>
        /// Returns the number of entries in the Hashtable of Lists.
        /// </summary>
        public long Count
        {
            get
            {
                int count = 0;
                foreach (DictionaryEntry de in _ht)
                {
                    ListWrapper value = de.Value as ListWrapper;
                    if (value != null)
                        count += value.List.Count;
                    else
                        count++;
                }
                return count;
            }
        }

        private class ListWrapper
        {
            public ListWrapper()
            {
                List = new ArrayList();
            }
            public ArrayList List
            {
                get;
            }
        }

        private class HtolEnumerator : IEnumerator
        {
            private readonly HashtableOfLists _htol;
            private IEnumerator _htEnum, _lstEnum;
            public HtolEnumerator(HashtableOfLists htol)
            {
                _htol = htol;
                Reset();
            }
            #region Implementation of IEnumerator
            public void Reset()
            {
                _htEnum = _htol._ht.GetEnumerator();
                _lstEnum = null;
            }
            public bool MoveNext()
            {
                if (_htEnum == null)
                    return false;
                if (_lstEnum != null)
                {
                    if (_lstEnum.MoveNext())
                        return true;
                    _lstEnum = null;
                }

                while (_htEnum.MoveNext())
                {
                    object obj = ((DictionaryEntry)_htEnum.Current).Value;

                    // Find the first non-listWrapper object, or non-empty listWrapper.
                    ListWrapper wrapper = obj as ListWrapper;
                    if (wrapper == null)
                        continue;
                    if (wrapper.List.Count == 0)
                        continue;

                    // Now that we've found it,  handle it.
                    _lstEnum = wrapper.List.GetEnumerator();
                    _lstEnum.MoveNext(); // We know it's non-empty, so this must succeed.
                    return true;
                }

                _htEnum = null;
                return false;
            }
            public object Current => _lstEnum != null ? _lstEnum.Current : ((DictionaryEntry?)_htEnum?.Current)?.Value;

            #endregion
        }
    }


    /// <summary>
    /// Manages a hashtable of lists of values. The keys are of type TKey, and the lists contain elements of type TValue.
    /// This is useful for maintaining collections of keyed entries where the keys are duplicated across multiple entries.
    /// </summary>
    /// <typeparam name="TKey">The type of the t key.</typeparam>
    /// <typeparam name="TValue">The type of the t value.</typeparam>
    /// <seealso>
    ///     <cref>System.Collections.Generic.IDictionary{TKey, System.Collections.Generic.List{TValue}}</cref>
    /// </seealso>
    /// <seealso cref="System.Collections.Generic.IEnumerable{TValue}" />
    /// <seealso>
    ///   <cref>System.Collections.Generic.IDictionary{TKey, List{TValue}}</cref>
    /// </seealso>
    public class HashtableOfLists<TKey, TValue> : IEnumerable<TValue>, IDictionary<TKey, List<TValue>>
    {

        #region Private Fields
        private readonly Dictionary<TKey, List<TValue>> _dictOfLists;
        private readonly IComparer<TValue> _comparer;
        #endregion

        #region Constructors
        public HashtableOfLists()
        {
            _dictOfLists = new Dictionary<TKey, List<TValue>>();
        }

        public HashtableOfLists(IComparer<TValue> comparer)
            : this()
        {
            _comparer = comparer;
        }
        #endregion

        /// <summary>
        /// Adds an element with the specified key and value into the Hashtable of Lists.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="item">The value of the element to add.</param>
        public void Add(TKey key, TValue item)
        {
            if (!_dictOfLists.ContainsKey(key))
            {
                _dictOfLists.Add(key, new List<TValue>());
            }
            _dictOfLists[key].Add(item);
            if (_comparer != null)
            {
                _dictOfLists[key].Sort(_comparer);
            }
        }

        /// <summary>
        /// Removes all of the elements with the specified key from the Hashtableof Lists.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        public void Remove(TKey key)
        {
            _dictOfLists.Remove(key);
        }

        /// <summary>
        /// Removes all elements with the specified key from the Hashtable of Lists.
        /// </summary>
        public bool Remove(TKey key, TValue item)
        {
            return _dictOfLists[key].Remove(item);
        }

        /// <summary>
        /// Removes all elements from the Hashtable of Lists.
        /// </summary>
        public void Clear()
        {
            _dictOfLists.Clear();
        }

        /// <summary>
        /// Retrieves a list of items associated with the provided key.
        /// </summary>
        public List<TValue> this[TKey key]
        {
            get
            {
                return _dictOfLists[key];
            }
            set
            {
                _dictOfLists[key] = value;
            }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1" /> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <value>The keys.</value>
        public Dictionary<TKey, List<TValue>>.KeyCollection Keys => _dictOfLists.Keys;

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1" /> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <value>The values.</value>
        public Dictionary<TKey, List<TValue>>.ValueCollection Values => _dictOfLists.Values;

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.IDictionary`2" /> contains a list element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="T:System.Collections.Generic.IDictionary`2" />.</param>
        /// <returns>true if the <see cref="T:System.Collections.Generic.IDictionary`2" /> contains a list element with the key; otherwise, false.</returns>
        public bool ContainsKey(TKey key)
        {
            return _dictOfLists.ContainsKey(key);
        }

        /// <summary>
        /// Returns an Enumerator that can iterate through all of the entries in the 
        /// Hashtable of Lists. The enumerator is an enumerator of values, not
        /// DictionaryEntries. This method first prunes empty lists from the hashtable.
        /// </summary>
        /// <returns>An Enumerator that can iterate through all of the entries in the 
        /// Hashtable of Lists.</returns>
        public IEnumerator GetEnumerator()
        {
            PruneEmptyLists();
            return new HtolEnumerator<TKey, TValue>(this);
        }

        /// <summary>
        /// Removes any entries in the HTOL that comprise a key and an empty list (which can
        /// result from removals of entries.)
        /// </summary>
        public void PruneEmptyLists()
        {

            List<TKey> keys = new List<TKey>(_dictOfLists.Keys);

            foreach (TKey keyVal in keys)
            {
                if (_dictOfLists[keyVal].Count == 0)
                {
                    _dictOfLists.Remove(keyVal);
                }
            }
        }

        /// <summary>
        /// Returns the number of entries in the Hashtable of Lists.
        /// </summary>
        public long Count
        {
            get
            {
                return _dictOfLists.Values.Sum(valList => valList.Count);
            }
        }

        private class HtolEnumerator<TTKey, TTValue> : IEnumerator<TTValue>
        {
            private readonly HashtableOfLists<TTKey, TTValue> _htol;
            private IEnumerator<List<TTValue>> _allListEnumerator;
            private IEnumerator<TTValue> _currListEnumerator;
            public HtolEnumerator(HashtableOfLists<TTKey, TTValue> htol)
            {
                _htol = htol;
                Reset();
            }
            #region Implementation of IEnumerator
            public void Reset()
            {
                _allListEnumerator = _htol.Values.GetEnumerator();
                _currListEnumerator = null;
            }

            public bool MoveNext()
            {
                if (_currListEnumerator == null)
                {
                    if (_allListEnumerator.MoveNext())
                    {
                        _currListEnumerator = _allListEnumerator.Current.GetEnumerator();
                    }
                    else
                    {
                        return false;
                    }
                    return MoveNext();
                }
                else
                {
                    if (_currListEnumerator.MoveNext())
                    {
                        return true;
                    }
                    else
                    {
                        _currListEnumerator.Dispose();
                        _currListEnumerator = null;
                        return MoveNext();
                    }
                }
            }

            public object Current => _currListEnumerator.Current;

            #endregion

            #region IEnumerator<_TValue> Members

            TTValue IEnumerator<TTValue>.Current => _currListEnumerator.Current;

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
                if (_currListEnumerator != null)
                {
                    _currListEnumerator.Dispose();
                    _allListEnumerator.Dispose();
                }
            }

            #endregion
        }

        #region IEnumerable<TValue> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
        {
            return new HtolEnumerator<TKey, TValue>(this);
        }

        #endregion

        #region IDictionary<TKey,List<TValue>> Members

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        public void Add(TKey key, List<TValue> value)
        {
            value.ForEach(delegate (TValue tv)
            {
                Add(key, tv);
            });
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1" /> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <value>The keys.</value>
        ICollection<TKey> IDictionary<TKey, List<TValue>>.Keys => _dictOfLists.Keys;

        /// <summary>
        /// Removes the element with the specified key from the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>true if the element is successfully removed; otherwise, false.  This method also returns false if <paramref name="key" /> was not found in the original <see cref="T:System.Collections.Generic.IDictionary`2" />.</returns>
        bool IDictionary<TKey, List<TValue>>.Remove(TKey key)
        {
            return _dictOfLists.Remove(key);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value" /> parameter. This parameter is passed uninitialized.</param>
        /// <returns>true if the object that implements <see cref="T:System.Collections.Generic.IDictionary`2" /> contains an element with the specified key; otherwise, false.</returns>
        public bool TryGetValue(TKey key, out List<TValue> value)
        {
            return _dictOfLists.TryGetValue(key, out value);
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1" /> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <value>The values.</value>
        ICollection<List<TValue>> IDictionary<TKey, List<TValue>>.Values => _dictOfLists.Values;

        #endregion

        #region ICollection<KeyValuePair<TKey,List<TValue>>> Members

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        public void Add(KeyValuePair<TKey, List<TValue>> item)
        {
            item.Value.ForEach(delegate (TValue tv)
            {
                Add(item.Key, tv);
            });
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false.</returns>
        public bool Contains(KeyValuePair<TKey, List<TValue>> item)
        {
            return _dictOfLists.ContainsKey(item.Key) && _dictOfLists[item.Key].Equals(item.Value);
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1" /> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        /// <exception cref="NotImplementedException"></exception>
        public void CopyTo(KeyValuePair<TKey, List<TValue>>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <value>The count.</value>
        int ICollection<KeyValuePair<TKey, List<TValue>>>.Count => _dictOfLists.Count;

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </summary>
        /// <value><c>true</c> if this instance is read only; otherwise, <c>false</c>.</value>
        public bool IsReadOnly => false;

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>true if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool Remove(KeyValuePair<TKey, List<TValue>> item)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKey,List<TValue>>> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        IEnumerator<KeyValuePair<TKey, List<TValue>>> IEnumerable<KeyValuePair<TKey, List<TValue>>>.GetEnumerator()
        {
            return _dictOfLists.GetEnumerator();
        }

        #endregion
    }
}