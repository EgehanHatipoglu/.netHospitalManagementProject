using System;

namespace HospitalManagementAvolonia.DataStructures
{
    /// <summary>
    /// Segment Tree implementation to keep track of appointment counts per day.
    /// Allows O(log n) querying of total appointments in a specific date range.
    /// Focuses on dates within a defined logical interval (e.g. 365 days of the year).
    /// </summary>
    public class AppointmentSegmentTree
    {
        private readonly int[] _tree;
        private readonly int _n; // Capacity (e.g., total days mapped)
        private readonly DateTime _baseDate;

        /// <summary>
        /// Creates a Segment Tree tracking a specific number of days forward/backward from a base date.
        /// E.g. capacity=365 tracks 1 year starting from baseDate.
        /// </summary>
        public AppointmentSegmentTree(DateTime baseDate, int capacityDays = 365)
        {
            _baseDate = baseDate.Date;
            _n = capacityDays;
            
            // To be safe, tree maximum size is 4 * N for standard array-based segment trees.
            _tree = new int[4 * _n]; 
        }

        /// <summary>
        /// Resolves the 0-indexed day offset relative to the base date.
        /// </summary>
        private int ResolveIndex(DateTime date)
        {
            int diff = (int)(date.Date - _baseDate).TotalDays;
            if (diff < 0 || diff >= _n) return -1; // Out of tracked range
            return diff;
        }

        /// <summary>
        /// Whenever a new appointment is booked, increments the count for that day. O(log n)
        /// </summary>
        public void AddAppointment(DateTime date)
        {
            int idx = ResolveIndex(date);
            if (idx != -1)
            {
                UpdateRecord(0, 0, _n - 1, idx, 1);
            }
        }

        /// <summary>
        /// Whenever an appointment is cancelled/removed, decrements the count for that day. O(log n)
        /// </summary>
        public void RemoveAppointment(DateTime date)
        {
            int idx = ResolveIndex(date);
            if (idx != -1)
            {
                UpdateRecord(0, 0, _n - 1, idx, -1);
            }
        }

        private void UpdateRecord(int nodeIndex, int leftBoundary, int rightBoundary, int targetIndex, int val)
        {
            // If the target is exactly the current node range (leaf node)
            if (leftBoundary == rightBoundary)
            {
                _tree[nodeIndex] += val;
                // Avoid negative total due to bug
                if (_tree[nodeIndex] < 0) _tree[nodeIndex] = 0; 
                return;
            }

            int mid = leftBoundary + (rightBoundary - leftBoundary) / 2;
            int leftChild = 2 * nodeIndex + 1;
            int rightChild = 2 * nodeIndex + 2;

            if (targetIndex <= mid)
            {
                // Target is in left child's range
                UpdateRecord(leftChild, leftBoundary, mid, targetIndex, val);
            }
            else
            {
                // Target is in right child's range
                UpdateRecord(rightChild, mid + 1, rightBoundary, targetIndex, val);
            }

            // After children update, parent node is sum of children
            _tree[nodeIndex] = _tree[leftChild] + _tree[rightChild];
        }

        /// <summary>
        /// Returns total appointments between two dates (inclusive). O(log n)
        /// </summary>
        public int QueryRange(DateTime start, DateTime end)
        {
            int lIdx = ResolveIndex(start);
            int rIdx = ResolveIndex(end);

            if (lIdx == -1 && rIdx == -1) return 0; // Both out of bounds
            if (lIdx == -1) lIdx = 0;               // Start bounded
            if (rIdx == -1) rIdx = _n - 1;          // End bounded
            if (lIdx > rIdx) return 0;

            return RangeSum(0, 0, _n - 1, lIdx, rIdx);
        }

        private int RangeSum(int nodeIndex, int startBoundary, int endBoundary, int qStart, int qEnd)
        {
            // If query perfectly overlaps with the current node interval
            if (qStart <= startBoundary && qEnd >= endBoundary)
            {
                return _tree[nodeIndex];
            }

            // If query is completely outside interval
            if (startBoundary > qEnd || endBoundary < qStart)
            {
                return 0;
            }

            // Partial overlap: search both children
            int mid = startBoundary + (endBoundary - startBoundary) / 2;
            int leftChild = 2 * nodeIndex + 1;
            int rightChild = 2 * nodeIndex + 2;

            return RangeSum(leftChild, startBoundary, mid, qStart, qEnd) +
                   RangeSum(rightChild, mid + 1, endBoundary, qStart, qEnd);
        }
    }
}
