using System;
using System.Collections.Generic;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.DataStructures
{
    /// <summary>
    /// LRU (Least Recently Used) Cache implementation to keep track of the last N accessed patients.
    /// Operates in O(1) time for Get and Put operations.
    /// Combines a Doubly Linked List with a Dictionary mapping.
    /// </summary>
    public class PatientLRUCache
    {
        private class DListNode
        {
            public int Key;
            public Patient Value;
            public DListNode? Prev;
            public DListNode? Next;

            public DListNode(int key, Patient value)
            {
                Key = key;
                Value = value;
            }
        }

        private readonly int _capacity;
        private readonly Dictionary<int, DListNode> _cache;
        
        // Dummy head and tail to prevent null checks during doubly link ops
        private readonly DListNode _head;
        private readonly DListNode _tail;

        public int Count => _cache.Count;

        public PatientLRUCache(int capacity = 10)
        {
            _capacity = capacity;
            _cache = new Dictionary<int, DListNode>();
            
            // Initialize dummy edges
            _head = new DListNode(0, null!);
            _tail = new DListNode(0, null!);
            _head.Next = _tail;
            _tail.Prev = _head;
        }

        /// <summary>
        /// Adds or pulls the target patient to the front (Most Recently Used position)
        /// </summary>
        public void AccessPatient(Patient patient)
        {
            if (patient == null) return;

            if (_cache.ContainsKey(patient.Id))
            {
                // Patient exists, pull to front
                DListNode node = _cache[patient.Id];
                // Update object reference just in case properties changed
                node.Value = patient; 
                RemoveNode(node);
                AddNodeToHead(node);
            }
            else
            {
                // New patient
                DListNode newNode = new DListNode(patient.Id, patient);
                _cache[patient.Id] = newNode;
                AddNodeToHead(newNode);

                if (_cache.Count > _capacity)
                {
                    // Evict Least Recently Used (node before tail)
                    DListNode lru = _tail.Prev;
                    if (lru != null && lru != _head)
                    {
                        RemoveNode(lru);
                        _cache.Remove(lru.Key);
                    }
                }
            }
        }

        /// <summary>
        /// Optional: If a patient is deleted globally, remove them from cache
        /// </summary>
        public void RemovePatient(int patientId)
        {
            if (_cache.ContainsKey(patientId))
            {
                DListNode node = _cache[patientId];
                RemoveNode(node);
                _cache.Remove(patientId);
            }
        }

        /// <summary>
        /// Fetches the cache elements in Most Recently Used -> Least Recently Used order
        /// </summary>
        public List<Patient> GetRecentPatients()
        {
            var list = new List<Patient>();
            DListNode current = _head.Next;
            while (current != _tail && current != null)
            {
                list.Add(current.Value);
                current = current.Next;
            }
            return list;
        }

        // ============ Doubly Linked List Helpers ============

        private void RemoveNode(DListNode node)
        {
            DListNode p = node.Prev;
            DListNode n = node.Next;

            if (p != null) p.Next = n;
            if (n != null) n.Prev = p;
        }

        private void AddNodeToHead(DListNode node)
        {
            // Add right after dummy head
            DListNode next = _head.Next;

            _head.Next = node;
            node.Prev = _head;

            node.Next = next;
            if (next != null) next.Prev = node;
        }
    }
}
