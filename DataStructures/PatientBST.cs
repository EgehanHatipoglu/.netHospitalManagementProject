using System;
using System.Collections.Generic;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.DataStructures
{
    /// <summary>
    /// Standard Binary Search Tree for patient search by name.
    /// No self-balancing — demonstrates worst-case O(n) vs AVL's O(log n).
    /// </summary>
    public class PatientBST
    {
        private class BSTNode
        {
            public Patient Patient;
            public BSTNode? Left;
            public BSTNode? Right;

            public BSTNode(Patient patient)
            {
                Patient = patient;
                Left = null;
                Right = null;
            }
        }

        private BSTNode? _root;

        public PatientBST()
        {
            _root = null;
        }

        // ============================================
        // INSERT
        // ============================================
        public void Insert(Patient patient)
        {
            _root = InsertRec(_root, patient);
        }

        private BSTNode InsertRec(BSTNode? node, Patient patient)
        {
            if (node == null)
                return new BSTNode(patient);

            string newName = patient.FirstName + " " + patient.LastName;
            string nodeName = node.Patient.FirstName + " " + node.Patient.LastName;
            int cmp = string.Compare(newName, nodeName, StringComparison.OrdinalIgnoreCase);

            if (cmp < 0)
                node.Left = InsertRec(node.Left, patient);
            else if (cmp > 0)
                node.Right = InsertRec(node.Right, patient);
            // Duplicate — do not insert

            return node;
        }

        // ============================================
        // DELETE
        // ============================================
        public bool Delete(string firstName, string lastName)
        {
            int initialSize = CountNodes(_root);
            _root = DeleteRec(_root, firstName, lastName);
            return CountNodes(_root) < initialSize;
        }

        private BSTNode? DeleteRec(BSTNode? node, string firstName, string lastName)
        {
            if (node == null) return null;

            string targetName = firstName + " " + lastName;
            string nodeName = node.Patient.FirstName + " " + node.Patient.LastName;
            int cmp = string.Compare(targetName, nodeName, StringComparison.OrdinalIgnoreCase);

            if (cmp < 0)
                node.Left = DeleteRec(node.Left, firstName, lastName);
            else if (cmp > 0)
                node.Right = DeleteRec(node.Right, firstName, lastName);
            else
            {
                if (node.Left == null && node.Right == null) return null;
                if (node.Left == null) return node.Right;
                if (node.Right == null) return node.Left;

                BSTNode successor = FindMin(node.Right);
                node.Patient = successor.Patient;
                node.Right = DeleteRec(node.Right, successor.Patient.FirstName, successor.Patient.LastName);
            }

            return node;
        }

        private BSTNode FindMin(BSTNode node)
        {
            while (node.Left != null)
                node = node.Left;
            return node;
        }

        private int CountNodes(BSTNode? node)
        {
            if (node == null) return 0;
            return 1 + CountNodes(node.Left) + CountNodes(node.Right);
        }

        // ============================================
        // SEARCH
        // ============================================
        public Patient? Search(string firstName, string lastName)
        {
            return SearchRec(_root, firstName, lastName);
        }

        private Patient? SearchRec(BSTNode? node, string firstName, string lastName)
        {
            if (node == null) return null;

            string searchName = firstName + " " + lastName;
            string nodeName = node.Patient.FirstName + " " + node.Patient.LastName;

            if (string.Equals(searchName, nodeName, StringComparison.OrdinalIgnoreCase))
                return node.Patient;

            if (string.Compare(searchName, nodeName, StringComparison.OrdinalIgnoreCase) < 0)
                return SearchRec(node.Left, firstName, lastName);
            else
                return SearchRec(node.Right, firstName, lastName);
        }

        // ============================================
        // IN-ORDER TRAVERSAL
        // ============================================
        public List<Patient> GetAllInOrder()
        {
            List<Patient> result = new List<Patient>();
            InOrderRec(_root, result);
            return result;
        }

        private void InOrderRec(BSTNode? node, List<Patient> result)
        {
            if (node != null)
            {
                InOrderRec(node.Left, result);
                result.Add(node.Patient);
                InOrderRec(node.Right, result);
            }
        }

        public int TotalNodes => CountNodes(_root);
    }
}
