using System;
using System.Collections.Generic;
using HospitalManagementWPF.Models;

namespace HospitalManagementWPF.DataStructures
{
    /// <summary>
    /// AVL Tree (Self-Balancing BST) for patient search by name.
    /// Guarantees O(log n) search, insert, and delete via automatic rotations.
    /// </summary>
    public class PatientAVL
    {
        private class AVLNode
        {
            public Patient Patient;
            public AVLNode? Left;
            public AVLNode? Right;
            public int Height;

            public AVLNode(Patient patient)
            {
                Patient = patient;
                Left = null;
                Right = null;
                Height = 1;
            }
        }

        private AVLNode? _root;

        public PatientAVL()
        {
            _root = null;
        }

        // ============================================
        // HEIGHT & BALANCE
        // ============================================
        private int Height(AVLNode? node) => node == null ? 0 : node.Height;

        private int GetBalance(AVLNode? node) =>
            node == null ? 0 : Height(node.Left) - Height(node.Right);

        // ============================================
        // ROTATIONS
        // ============================================
        private AVLNode RightRotate(AVLNode node)
        {
            AVLNode leftChild = node.Left!;
            AVLNode? temp = leftChild.Right;

            leftChild.Right = node;
            node.Left = temp;

            node.Height = Math.Max(Height(node.Left), Height(node.Right)) + 1;
            leftChild.Height = Math.Max(Height(leftChild.Left), Height(leftChild.Right)) + 1;

            return leftChild;
        }

        private AVLNode LeftRotate(AVLNode node)
        {
            AVLNode rightChild = node.Right!;
            AVLNode? temp = rightChild.Left;

            rightChild.Left = node;
            node.Right = temp;

            node.Height = Math.Max(Height(node.Left), Height(node.Right)) + 1;
            rightChild.Height = Math.Max(Height(rightChild.Left), Height(rightChild.Right)) + 1;

            return rightChild;
        }

        // ============================================
        // INSERT
        // ============================================
        public void Insert(Patient patient)
        {
            _root = InsertRec(_root, patient);
        }

        private AVLNode InsertRec(AVLNode? node, Patient patient)
        {
            if (node == null)
                return new AVLNode(patient);

            string newName = patient.FirstName + " " + patient.LastName;
            string nodeName = node.Patient.FirstName + " " + node.Patient.LastName;

            int cmp = string.Compare(newName, nodeName, StringComparison.OrdinalIgnoreCase);

            if (cmp < 0)
                node.Left = InsertRec(node.Left, patient);
            else if (cmp > 0)
                node.Right = InsertRec(node.Right, patient);
            else
                return node; // Duplicate

            node.Height = 1 + Math.Max(Height(node.Left), Height(node.Right));
            int balance = GetBalance(node);

            // Left-Left
            if (balance > 1)
            {
                string leftName = node.Left!.Patient.FirstName + " " + node.Left.Patient.LastName;
                if (string.Compare(newName, leftName, StringComparison.OrdinalIgnoreCase) < 0)
                    return RightRotate(node);
                // Left-Right
                if (string.Compare(newName, leftName, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    node.Left = LeftRotate(node.Left);
                    return RightRotate(node);
                }
            }

            // Right-Right
            if (balance < -1)
            {
                string rightName = node.Right!.Patient.FirstName + " " + node.Right.Patient.LastName;
                if (string.Compare(newName, rightName, StringComparison.OrdinalIgnoreCase) > 0)
                    return LeftRotate(node);
                // Right-Left
                if (string.Compare(newName, rightName, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    node.Right = RightRotate(node.Right);
                    return LeftRotate(node);
                }
            }

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

        private AVLNode? DeleteRec(AVLNode? node, string firstName, string lastName)
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
                if (node.Left == null) return node.Right;
                if (node.Right == null) return node.Left;

                AVLNode successor = FindMin(node.Right);
                node.Patient = successor.Patient;
                node.Right = DeleteRec(node.Right, successor.Patient.FirstName, successor.Patient.LastName);
            }

            node.Height = 1 + Math.Max(Height(node.Left), Height(node.Right));
            int balance = GetBalance(node);

            if (balance > 1 && GetBalance(node.Left) >= 0)
                return RightRotate(node);
            if (balance > 1 && GetBalance(node.Left) < 0)
            {
                node.Left = LeftRotate(node.Left!);
                return RightRotate(node);
            }
            if (balance < -1 && GetBalance(node.Right) <= 0)
                return LeftRotate(node);
            if (balance < -1 && GetBalance(node.Right) > 0)
            {
                node.Right = RightRotate(node.Right!);
                return LeftRotate(node);
            }

            return node;
        }

        private AVLNode FindMin(AVLNode node)
        {
            while (node.Left != null)
                node = node.Left;
            return node;
        }

        // ============================================
        // SEARCH
        // ============================================
        public Patient? Search(string firstName, string lastName)
        {
            return SearchRec(_root, firstName, lastName);
        }

        private Patient? SearchRec(AVLNode? node, string firstName, string lastName)
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

        private void InOrderRec(AVLNode? node, List<Patient> result)
        {
            if (node != null)
            {
                InOrderRec(node.Left, result);
                result.Add(node.Patient);
                InOrderRec(node.Right, result);
            }
        }

        // ============================================
        // STATISTICS
        // ============================================
        public bool IsBalanced() => IsBalancedRec(_root);

        private bool IsBalancedRec(AVLNode? node)
        {
            if (node == null) return true;
            if (Math.Abs(GetBalance(node)) > 1) return false;
            return IsBalancedRec(node.Left) && IsBalancedRec(node.Right);
        }

        private int CountNodes(AVLNode? node)
        {
            if (node == null) return 0;
            return 1 + CountNodes(node.Left) + CountNodes(node.Right);
        }

        public int TotalNodes => CountNodes(_root);
        public int TreeHeight => Height(_root);

        public (int totalNodes, int treeHeight, bool isBalanced) GetStats()
        {
            return (TotalNodes, TreeHeight, IsBalanced());
        }
    }
}
