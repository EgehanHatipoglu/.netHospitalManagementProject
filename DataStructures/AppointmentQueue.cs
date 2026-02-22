using System.Collections.Generic;
using HospitalManagementWPF.Models;

namespace HospitalManagementWPF.DataStructures
{
    /// <summary>
    /// FIFO Linked-List Queue for managing a doctor's daily appointments.
    /// Ensures fair, First-Come-First-Served order.
    /// </summary>
    public class AppointmentQueue
    {
        private class QueueNode
        {
            public Appointment Data;
            public QueueNode? Next;

            public QueueNode(Appointment data)
            {
                Data = data;
                Next = null;
            }
        }

        private QueueNode? _front;
        private QueueNode? _rear;

        public AppointmentQueue()
        {
            _front = null;
            _rear = null;
        }

        public bool IsEmpty => _front == null;

        /// <summary>
        /// Enqueue: Adds to the end of the queue. O(1)
        /// </summary>
        public void Enqueue(Appointment appointment)
        {
            QueueNode newNode = new QueueNode(appointment);
            if (_rear != null)
                _rear.Next = newNode;
            _rear = newNode;
            if (_front == null)
                _front = newNode;
        }

        /// <summary>
        /// Dequeue: Removes and returns the front appointment. O(1)
        /// </summary>
        public Appointment? Dequeue()
        {
            if (IsEmpty) return null;

            Appointment data = _front!.Data;
            _front = _front.Next;
            if (_front == null)
                _rear = null;
            return data;
        }

        public int Size
        {
            get
            {
                if (IsEmpty) return 0;
                int count = 0;
                QueueNode? current = _front;
                while (current != null)
                {
                    count++;
                    current = current.Next;
                }
                return count;
            }
        }

        /// <summary>
        /// Returns all appointments in queue order for display.
        /// </summary>
        public List<Appointment> GetAll()
        {
            List<Appointment> result = new List<Appointment>();
            QueueNode? current = _front;
            while (current != null)
            {
                result.Add(current.Data);
                current = current.Next;
            }
            return result;
        }
    }
}
