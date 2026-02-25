using System;
using System.Collections.Generic;
using System.Linq;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.DataStructures
{
    /// <summary>
    /// Adjacency List based Graph implementation for the Doctor Referral Network.
    /// Supports BFS and DFS traversals.
    /// </summary>
    public class DoctorGraph
    {
        // Maps Doctor ID to their list of referred doctors (Adjacency List)
        private readonly Dictionary<int, List<Doctor>> _adjList;
        // Centralized reference of all doctors manually cached in graph for quick ID lookup
        private readonly Dictionary<int, Doctor> _allDoctors;

        public DoctorGraph()
        {
            _adjList = new Dictionary<int, List<Doctor>>();
            _allDoctors = new Dictionary<int, Doctor>();
        }

        public void AddDoctor(Doctor doctor)
        {
            if (doctor == null) return;
            if (!_adjList.ContainsKey(doctor.Id))
            {
                _adjList[doctor.Id] = new List<Doctor>();
                _allDoctors[doctor.Id] = doctor;
            }
        }

        /// <summary>
        /// Adds a directed referral connection: from -> to
        /// </summary>
        public void AddReferral(Doctor from, Doctor to)
        {
            if (from == null || to == null) return;

            AddDoctor(from);
            AddDoctor(to);

            if (!_adjList[from.Id].Contains(to))
            {
                _adjList[from.Id].Add(to);
            }
        }

        /// <summary>
        /// Removes a doctor and all connections indicating them.
        /// </summary>
        public void RemoveDoctor(int doctorId)
        {
            if (_adjList.ContainsKey(doctorId))
            {
                _adjList.Remove(doctorId);
                _allDoctors.Remove(doctorId);
            }

            foreach (var connections in _adjList.Values)
            {
                connections.RemoveAll(d => d.Id == doctorId);
            }
        }

        /// <summary>
        /// Returns all connections for a specific doctor.
        /// </summary>
        public List<Doctor> GetReferrals(int doctorId)
        {
            if (_adjList.TryGetValue(doctorId, out var list))
            {
                return list;
            }
            return new List<Doctor>();
        }

        /// <summary>
        /// Performs Breadth First Search to find shortest referral path.
        /// Returns a list of strings explaining the path.
        /// </summary>
        public List<string> BFS(int startId, int targetId)
        {
            var result = new List<string>();
            if (!_allDoctors.ContainsKey(startId) || !_allDoctors.ContainsKey(targetId))
            {
                result.Add("Geçersiz başlangıç veya hedef ID.");
                return result;
            }

            if (startId == targetId)
            {
                result.Add("Başlangıç ve hedef doktor aynı.");
                return result;
            }

            var queue = new Queue<int>();
            var visited = new HashSet<int>();
            
            // To reconstruct the path: map current node -> parent node
            var parentMap = new Dictionary<int, int>();

            queue.Enqueue(startId);
            visited.Add(startId);

            bool found = false;

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();

                if (current == targetId)
                {
                    found = true;
                    break;
                }

                foreach (var neighbor in _adjList[current])
                {
                    if (!visited.Contains(neighbor.Id))
                    {
                        visited.Add(neighbor.Id);
                        parentMap[neighbor.Id] = current;
                        queue.Enqueue(neighbor.Id);
                    }
                }
            }

            if (!found)
            {
                result.Add($"Dr. {_allDoctors[startId].FirstName} ile Dr. {_allDoctors[targetId].FirstName} arasında sevk ağı bulunamadı.");
                return result;
            }

            // Reconstruct the path backwards
            var path = new List<int>();
            int currNode = targetId;
            while (currNode != startId)
            {
                path.Add(currNode);
                currNode = parentMap[currNode];
            }
            path.Add(startId);
            path.Reverse();

            result.Add($"Sevk Ağı Bulundu (Mesafe: {path.Count - 1} adım):");
            string pathStr = string.Join(" ➔ ", path.Select(id => $"Dr. {_allDoctors[id].FirstName}"));
            result.Add(pathStr);

            return result;
        }

        /// <summary>
        /// Performs Depth First Search (DFS) completely starting from a doctor.
        /// Primarily used to find all possible downstream network from a single doctor. 
        /// Returns a list of strings of doctor names visited in sequence.
        /// </summary>
        public List<string> DFS(int startId)
        {
            var result = new List<string>();
            if (!_allDoctors.ContainsKey(startId)) return result;

            var visited = new HashSet<int>();
            DFSUtil(startId, visited, result);
            return result;
        }

        private void DFSUtil(int v, HashSet<int> visited, List<string> result)
        {
            visited.Add(v);
            result.Add($"Dr. {_allDoctors[v].FullName}");

            foreach (var neighbor in _adjList[v])
            {
                if (!visited.Contains(neighbor.Id))
                {
                    DFSUtil(neighbor.Id, visited, result);
                }
            }
        }
        
        public List<Doctor> GetAllDoctors() => _allDoctors.Values.ToList();
    }
}
