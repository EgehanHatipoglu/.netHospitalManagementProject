using System;
using System.Collections.Generic;

namespace HospitalManagementWPF.Models
{
    /// <summary>
    /// Patient model with a Singly Linked List for medical history.
    /// Each Visit node stores past examination data.
    /// </summary>
    public class Patient
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NationalId { get; set; }
        public string Phone { get; set; }
        public DateTime BirthDate { get; set; }

        // Linked List for medical history
        private Visit? _head;

        private class Visit
        {
            public DateTime Date;
            public Doctor Doctor;
            public string Notes;
            public Visit? Next;

            public Visit(DateTime date, Doctor doctor, string notes)
            {
                Date = date;
                Doctor = doctor;
                Notes = notes;
                Next = null;
            }

            public override string ToString()
            {
                return $"Date: {Date:dd/MM/yyyy HH:mm}, Doctor: Dr. {Doctor.FirstName} {Doctor.LastName}, Notes: {Notes}";
            }
        }

        public Patient(int id, string firstName, string lastName, string nationalId, string phone, DateTime birthDate)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            NationalId = nationalId;
            Phone = phone;
            BirthDate = birthDate;
            _head = null;
        }

        /// <summary>
        /// Adds a new medical record to the end of the linked list. O(n)
        /// </summary>
        public void AddVisit(DateTime date, Doctor doctor, string notes)
        {
            if (doctor == null) return;

            Visit newVisit = new Visit(date, doctor, notes);

            if (_head == null)
            {
                _head = newVisit;
            }
            else
            {
                Visit current = _head;
                while (current.Next != null)
                    current = current.Next;
                current.Next = newVisit;
            }
        }

        /// <summary>
        /// Returns all visit records as a list of strings for display.
        /// </summary>
        public List<string> GetHistory()
        {
            List<string> history = new List<string>();
            if (_head == null)
            {
                history.Add("No medical history available.");
                return history;
            }

            Visit? current = _head;
            int count = 1;
            while (current != null)
            {
                history.Add($"{count}. {current}");
                current = current.Next;
                count++;
            }
            return history;
        }

        public string FullName => $"{FirstName} {LastName}";

        public override string ToString()
        {
            return $"ID: {Id} | {FirstName} {LastName} | TC: {NationalId}";
        }
    }
}
