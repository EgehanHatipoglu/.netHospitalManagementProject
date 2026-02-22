using System.Collections.Generic;
using HospitalManagementWPF.Models;

namespace HospitalManagementWPF.DataStructures
{
    /// <summary>
    /// N-ary (General) Tree representing the hospital organizational hierarchy.
    /// Each node can have an unlimited number of children (sub-departments).
    /// </summary>
    public class HospitalTree
    {
        private class DepartmentTreeNode
        {
            public Department Department;
            public List<DepartmentTreeNode> Children;

            public DepartmentTreeNode(Department department)
            {
                Department = department;
                Children = new List<DepartmentTreeNode>();
            }

            public void AddChild(DepartmentTreeNode child)
            {
                Children.Add(child);
            }
        }

        private DepartmentTreeNode _root;

        public HospitalTree(string hospitalName)
        {
            Department hospital = new Department(0, hospitalName, 999);
            _root = new DepartmentTreeNode(hospital);
        }

        public void AddDepartmentToRoot(Department department)
        {
            if (department != null)
            {
                DepartmentTreeNode newNode = new DepartmentTreeNode(department);
                _root.AddChild(newNode);
            }
        }

        /// <summary>
        /// Returns the hierarchy as a list of (name, level, doctorCount) tuples for UI display.
        /// </summary>
        public List<(string name, int level, int doctorCount)> GetHierarchy()
        {
            var result = new List<(string name, int level, int doctorCount)>();
            GetHierarchyRec(_root, 0, result);
            return result;
        }

        private void GetHierarchyRec(DepartmentTreeNode node, int level,
            List<(string name, int level, int doctorCount)> result)
        {
            result.Add((node.Department.Name, level, node.Department.DoctorCount));

            foreach (var child in node.Children)
            {
                GetHierarchyRec(child, level + 1, result);
            }
        }

        public int GetDepartmentCount()
        {
            return CountDepartments(_root) - 1; // Exclude root (hospital)
        }

        private int CountDepartments(DepartmentTreeNode node)
        {
            int count = 1;
            foreach (var child in node.Children)
                count += CountDepartments(child);
            return count;
        }

        public int GetTotalDoctorCount()
        {
            return CountDoctors(_root);
        }

        private int CountDoctors(DepartmentTreeNode node)
        {
            int count = node.Department.DoctorCount;
            foreach (var child in node.Children)
                count += CountDoctors(child);
            return count;
        }
    }
}
