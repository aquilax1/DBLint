using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DBLint.Model
{

    public static class DictionaryFactory
    {
        public static DatabaseDictionary<SchemaID, TVal> CreateSchemaID<TVal>()
        {
            return new DatabaseDictionary<SchemaID, TVal>(SchemaID.GetSchemaHashCode, SchemaID.SchemaEquals);
        }
        public static DatabaseDictionary<TableID, TVal> CreateTableID<TVal>()
        {
            return new DatabaseDictionary<TableID, TVal>(TableID.GetTableHashCode, TableID.TableEquals);
        }

        public static DatabaseDictionary<ColumnID, TVal> CreateColumnID<TVal>()
        {
            return new DatabaseDictionary<ColumnID, TVal>(ColumnID.GetColumnHashCode, ColumnID.ColumnEquals);
        }
    }

    public class DatabaseDictionary<A, B> : IDictionary<A, B> where A : DatabaseID
    {
        private int count = 0;

        public DatabaseDictionary(Func<A, int> hashFunc, Func<A, A, bool> equalsFunc)
        {
            if (hashFunc == null)
                throw new ArgumentNullException("hashFunc");
            if (equalsFunc == null)
                throw new ArgumentNullException("equalsFunc");
            this._hashFunc = hashFunc;
            this._equalsFunc = equalsFunc;
        }

        private Dictionary<int, List<KeyValuePair<A, B>>> _mapper = new Dictionary<int, List<KeyValuePair<A, B>>>();
        private readonly Func<A, int> _hashFunc = null;
        private Func<A, A, bool> _equalsFunc;

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<KeyValuePair<A, B>> GetEnumerator()
        {
            foreach (var val in _mapper.Values)
            {
                for (int index = 0; index < val.Count; index++)
                {
                    var b = val[index];
                    yield return b;
                }
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
        public void Add(KeyValuePair<A, B> item)
        {
            var hashcode = _hashFunc(item.Key);
            if (this._mapper.ContainsKey(hashcode))
            {
                var list = _mapper[hashcode];
                if (list.Contains(item))
                    throw new InvalidDataException("Dictionary already contains key");
                else list.Add(item);
            }
            else
            {
                _mapper[hashcode] = new List<KeyValuePair<A, B>> { item };
            }
            count++;
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only. </exception>
        public void Clear()
        {
            _mapper.Clear();
            count = 0;
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
        /// </summary>
        /// <returns>
        /// true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.
        /// </returns>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
        public bool Contains(KeyValuePair<A, B> item)
        {
            var hashcode = _hashFunc(item.Key);
            if (!_mapper.ContainsKey(hashcode))
                return false;
            var list = _mapper[hashcode];
            return list.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param><param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param><exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception><exception cref="T:System.ArgumentException"><paramref name="array"/> is multidimensional.-or-The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.-or-Type <paramref name="T"/> cannot be cast automatically to the type of the destination <paramref name="array"/>.</exception>
        public void CopyTo(KeyValuePair<A, B>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <returns>
        /// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </returns>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
        public bool Remove(KeyValuePair<A, B> item)
        {
            var hashcode = _hashFunc(item.Key);
            if (!_mapper.ContainsKey(hashcode))
                return false;
            var list = _mapper[hashcode];
            return list.Remove(item);
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <returns>
        /// The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </returns>
        public int Count
        {
            get
            {
                return count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
        /// </summary>
        /// <returns>
        /// true if the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only; otherwise, false.
        /// </returns>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified key.
        /// </summary>
        /// <returns>
        /// true if the <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the key; otherwise, false.
        /// </returns>
        /// <param name="key">The key to locate in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.</param><exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        public bool ContainsKey(A key)
        {
            var hashcode = _hashFunc(key);
            if (!_mapper.ContainsKey(hashcode))
                return false;
            var list = _mapper[hashcode];
            return list.Any(p => _equalsFunc(p.Key, key));
        }

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param><param name="value">The object to use as the value of the element to add.</param><exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception><exception cref="T:System.ArgumentException">An element with the same key already exists in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.</exception><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IDictionary`2"/> is read-only.</exception>
        public void Add(A key, B value)
        {
            var kvp = new KeyValuePair<A, B>(key, value);
            this.Add(kvp);
        }

        /// <summary>
        /// Removes the element with the specified key from the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// true if the element is successfully removed; otherwise, false.  This method also returns false if <paramref name="key"/> was not found in the original <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        /// <param name="key">The key of the element to remove.</param><exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IDictionary`2"/> is read-only.</exception>
        public bool Remove(A key)
        {
            var hashcode = _hashFunc(key);
            if (!_mapper.ContainsKey(hashcode))
                return false;
            var list = _mapper[hashcode];
            var removeCount = list.RemoveAll(p => _equalsFunc(p.Key, key));
            count -= removeCount;
            return removeCount > 0;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <returns>
        /// true if the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified key; otherwise, false.
        /// </returns>
        /// <param name="key">The key whose value to get.</param><param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param><exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        public bool TryGetValue(A key, out B value)
        {
            var hashcode = _hashFunc(key);
            if (_mapper.ContainsKey(hashcode))
            {
                var list = _mapper[hashcode];
                if (list.Any(p => _equalsFunc(p.Key, key)))
                {
                    var item = list.First(p => _equalsFunc(p.Key, key));
                    value = item.Value;
                    return true;
                }
            }
            value = default(B);
            return false;
        }

        /// <summary>
        /// Gets or sets the element with the specified key.
        /// </summary>
        /// <returns>
        /// The element with the specified key.
        /// </returns>
        /// <param name="key">The key of the element to get or set.</param><exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception><exception cref="T:System.Collections.Generic.KeyNotFoundException">The property is retrieved and <paramref name="key"/> is not found.</exception><exception cref="T:System.NotSupportedException">The property is set and the <see cref="T:System.Collections.Generic.IDictionary`2"/> is read-only.</exception>
        public B this[A key]
        {
            get
            {
                var hashcode = _hashFunc(key);
                var list = _mapper[hashcode];
                var item = list.First(p => _equalsFunc(p.Key, key));
                return item.Value;
            }
            set
            {
                var newval = new KeyValuePair<A, B>(key, value);
                var hashcode = _hashFunc(key);

                List<KeyValuePair<A, B>> list;
                if (this._mapper.TryGetValue(hashcode, out list))
                {
                    var removedKeys = list.RemoveAll(k => _equalsFunc(k.Key, key));
                    list.Add(newval);
                    this.count = count - removedKeys + 1;
                }
                else
                {
                    this._mapper[hashcode] = new List<KeyValuePair<A, B>> { newval };
                }
            }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        public ICollection<A> Keys
        {
            get
            {
                List<A> list = new List<A>();
                foreach (var val in _mapper.Values)
                    foreach (var pair in val)
                        list.Add(pair.Key);
                return list;
            }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        public ICollection<B> Values
        {
            get
            {
                List<B> list = new List<B>();
                foreach (var val in _mapper.Values)
                    foreach (var pair in val)
                        list.Add(pair.Value);
                return list;
            }
        }
    }
}
