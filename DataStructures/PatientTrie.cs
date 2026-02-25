using System;
using System.Collections.Generic;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.DataStructures
{
    /// <summary>
    /// Trie (Prefix Tree) implementation for fast autocomplete searches O(m)
    /// </summary>
    public class PatientTrie
    {
        private class TrieNode
        {
            public Dictionary<char, TrieNode> Children = new();
            public bool IsEndOfWord;
            public List<Patient> PatientsAtThisNode = new();
        }

        private readonly TrieNode _root;

        public PatientTrie()
        {
            _root = new TrieNode();
        }

        /// <summary>
        /// Inserts a patient's full name (case insensitive) into the Trie.
        /// </summary>
        public void Insert(Patient patient)
        {
            if (patient == null || string.IsNullOrWhiteSpace(patient.FullName)) return;

            string key = patient.FullName.ToLowerInvariant();
            TrieNode current = _root;

            foreach (char c in key)
            {
                if (!current.Children.ContainsKey(c))
                {
                    current.Children[c] = new TrieNode();
                }
                current = current.Children[c];
            }

            current.IsEndOfWord = true;
            
            // Add reference to prevent duplicates if names are perfectly matching
            if (!current.PatientsAtThisNode.Exists(p => p.Id == patient.Id))
            {
                current.PatientsAtThisNode.Add(patient);
            }
        }

        /// <summary>
        /// Fetches all patients that match a given prefix. Let k be the number of results, O(m) to find prefix + O(k) traverse.
        /// </summary>
        public List<Patient> GetSuggestions(string prefix)
        {
            var results = new List<Patient>();
            if (string.IsNullOrWhiteSpace(prefix)) return results;

            prefix = prefix.ToLowerInvariant();
            TrieNode current = _root;

            // Traverse to the end of the prefix
            foreach (char c in prefix)
            {
                if (!current.Children.ContainsKey(c))
                {
                    return results; // No matches found
                }
                current = current.Children[c];
            }

            // Once at the prefix node, collect all words branching from here
            CollectAllPatients(current, results);
            return results;
        }

        private void CollectAllPatients(TrieNode node, List<Patient> results)
        {
            if (node == null) return;

            if (node.IsEndOfWord)
            {
                results.AddRange(node.PatientsAtThisNode);
            }

            foreach (var child in node.Children.Values)
            {
                CollectAllPatients(child, results);
            }
        }

        /// <summary>
        /// Removes a patient from the trie
        /// </summary>
        public void Remove(Patient patient)
        {
            if (patient == null || string.IsNullOrWhiteSpace(patient.FullName)) return;

            string key = patient.FullName.ToLowerInvariant();
            TrieNode current = _root;

            foreach (char c in key)
            {
                if (!current.Children.ContainsKey(c)) return;
                current = current.Children[c];
            }

            if (current.IsEndOfWord)
            {
                current.PatientsAtThisNode.RemoveAll(p => p.Id == patient.Id);
                // We keep IsEndOfWord if there are still identically named patients
                if (current.PatientsAtThisNode.Count == 0)
                {
                    current.IsEndOfWord = false;
                }
            }
        }
    }
}
