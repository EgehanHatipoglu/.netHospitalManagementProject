using System;
using System.Collections.Generic;

namespace HospitalManagementWPF.DataStructures
{
    /// <summary>
    /// Custom Generic Hash Table with Separate Chaining.
    /// Implements O(1) average-case lookup using linked-list buckets for collision resolution.
    /// </summary>
    public class HashTable<K, V> where K : notnull
    {
        private class HashNode
        {
            public K Key;
            public V Value;
            public HashNode? Next;

            public HashNode(K key, V value)
            {
                Key = key;
                Value = value;
                Next = null;
            }
        }

        private HashNode?[] _buckets;
        private int _capacity;
        private int _size;

        /// <summary>
        /// Constructor: Calculates the table capacity using the Student ID.
        /// </summary>
        public HashTable(int studentId)
        {
            _capacity = (studentId % 100) + 50;
            _buckets = new HashNode?[_capacity];
            _size = 0;
        }

        private int Hash(K key)
        {
            int hashCode = key.GetHashCode();
            int index = hashCode % _capacity;
            if (index < 0) index = -index;
            return index;
        }

        /// <summary>
        /// PUT: Inserts or updates a key-value pair.
        /// Uses chaining for collision resolution.
        /// </summary>
        public void Put(K key, V value)
        {
            int index = Hash(key);
            HashNode? current = _buckets[index];

            while (current != null)
            {
                if (current.Key.Equals(key))
                {
                    current.Value = value;
                    return;
                }
                current = current.Next;
            }

            HashNode newNode = new HashNode(key, value);
            newNode.Next = _buckets[index];
            _buckets[index] = newNode;
            _size++;
        }

        /// <summary>
        /// GET: Retrieves a value based on its key in O(1) average time.
        /// </summary>
        public V? Get(K key)
        {
            int index = Hash(key);
            HashNode? current = _buckets[index];

            while (current != null)
            {
                if (current.Key.Equals(key))
                    return current.Value;
                current = current.Next;
            }

            return default;
        }

        /// <summary>
        /// REMOVE: Deletes a key-value pair from the table.
        /// </summary>
        public V? Remove(K key)
        {
            int index = Hash(key);
            HashNode? current = _buckets[index];
            HashNode? prev = null;

            while (current != null)
            {
                if (current.Key.Equals(key))
                {
                    if (prev == null)
                        _buckets[index] = current.Next;
                    else
                        prev.Next = current.Next;

                    _size--;
                    return current.Value;
                }
                prev = current;
                current = current.Next;
            }

            return default;
        }

        public bool ContainsKey(K key) => Get(key) != null;

        public int Size => _size;

        public bool IsEmpty => _size == 0;

        public List<V> Values()
        {
            List<V> result = new List<V>();
            for (int i = 0; i < _capacity; i++)
            {
                HashNode? current = _buckets[i];
                while (current != null)
                {
                    result.Add(current.Value);
                    current = current.Next;
                }
            }
            return result;
        }

        public int Capacity => _capacity;

        public int UsedBuckets
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _capacity; i++)
                    if (_buckets[i] != null) count++;
                return count;
            }
        }
    }
}
