using System;
using System.Collections.Generic;
using HospitalManagementWPF.Models;

namespace HospitalManagementWPF.DataStructures
{
    /// <summary>
    /// Max-Heap based Priority Queue for Emergency Room Triage.
    /// The patient with the highest severity (1-10) is always at the top.
    /// </summary>
    public class ERPriorityQueue
    {
        /// <summary>
        /// Represents a patient in the Emergency Room with triage data.
        /// </summary>
        public class ERPatient
        {
            public Patient Patient { get; }
            public int Severity { get; }
            public DateTime ArrivalTime { get; }
            public string Complaint { get; }

            public ERPatient(Patient patient, int severity, string complaint)
            {
                if (patient == null)
                    throw new ArgumentException("Patient cannot be null!");
                if (severity < 1 || severity > 10)
                    throw new ArgumentException($"Severity must be 1-10! Given: {severity}");
                if (string.IsNullOrWhiteSpace(complaint))
                    throw new ArgumentException("Complaint cannot be empty!");

                Patient = patient;
                Severity = severity;
                Complaint = complaint.Trim();
                ArrivalTime = DateTime.Now;
            }

            public override string ToString()
            {
                return $"{Patient.FirstName} {Patient.LastName} (Severity: {Severity}, Complaint: {Complaint}, Arrival: {ArrivalTime:HH:mm})";
            }
        }

        private List<ERPatient> _heap;

        public ERPriorityQueue()
        {
            _heap = new List<ERPatient>();
        }

        public bool IsEmpty => _heap.Count == 0;
        public int Size => _heap.Count;

        /// <summary>
        /// Adds a patient and bubbles up to maintain max-heap property. O(log n)
        /// </summary>
        public void AddPatient(ERPatient patient)
        {
            _heap.Add(patient);
            HeapifyUp(_heap.Count - 1);
        }

        /// <summary>
        /// Extracts the root (most critical patient). O(log n)
        /// </summary>
        public ERPatient? RemoveHighestPriority()
        {
            if (IsEmpty) return null;

            ERPatient highest = _heap[0];
            ERPatient last = _heap[_heap.Count - 1];
            _heap.RemoveAt(_heap.Count - 1);

            if (!IsEmpty)
            {
                _heap[0] = last;
                HeapifyDown(0);
            }

            return highest;
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (_heap[index].Severity > _heap[parentIndex].Severity)
                {
                    Swap(index, parentIndex);
                    index = parentIndex;
                }
                else break;
            }
        }

        private void HeapifyDown(int index)
        {
            int size = _heap.Count;
            while (true)
            {
                int largest = index;
                int left = 2 * index + 1;
                int right = 2 * index + 2;

                if (left < size && _heap[left].Severity > _heap[largest].Severity)
                    largest = left;
                if (right < size && _heap[right].Severity > _heap[largest].Severity)
                    largest = right;

                if (largest != index)
                {
                    Swap(index, largest);
                    index = largest;
                }
                else break;
            }
        }

        private void Swap(int i, int j)
        {
            ERPatient temp = _heap[i];
            _heap[i] = _heap[j];
            _heap[j] = temp;
        }

        /// <summary>
        /// Returns a sorted copy (by severity descending) for display.
        /// </summary>
        public List<ERPatient> GetAllSorted()
        {
            List<ERPatient> copy = new List<ERPatient>(_heap);
            copy.Sort((a, b) => b.Severity.CompareTo(a.Severity));
            return copy;
        }

        /// <summary>
        /// Removes a specific patient by ID. Used for Undo.
        /// </summary>
        public bool RemovePatientById(int patientId)
        {
            for (int i = 0; i < _heap.Count; i++)
            {
                if (_heap[i].Patient.Id == patientId)
                {
                    RemoveAtIndex(i);
                    return true;
                }
            }
            return false;
        }

        private void RemoveAtIndex(int index)
        {
            if (index >= _heap.Count) return;

            ERPatient last = _heap[_heap.Count - 1];
            _heap.RemoveAt(_heap.Count - 1);

            if (index < _heap.Count)
            {
                _heap[index] = last;
                int parent = (index - 1) / 2;
                if (index > 0 && _heap[index].Severity > _heap[parent].Severity)
                    HeapifyUp(index);
                else
                    HeapifyDown(index);
            }
        }
    }
}
