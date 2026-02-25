using HospitalManagementAvolonia.DataStructures;

namespace HospitalManagementAvolonia.Models
{
    /// <summary>
    /// Doctor model with a daily appointment queue.
    /// </summary>
    public class Doctor
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Department? Department { get; set; }
        public int DepartmentId { get; set; }
        public string DeptName => Department?.Name ?? "N/A";
        public string Phone { get; set; }
        public AppointmentQueue DailyQueue { get; set; }

        public Doctor(int id, string firstName, string lastName, Department? department, string phone)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Department = department;
            Phone = phone;
            DailyQueue = new AppointmentQueue();
        }

        public string FullName => $"Dr. {FirstName} {LastName}";

        public override string ToString()
        {
            return $"Dr. {FirstName} {LastName} ({Department?.Name ?? "N/A"})";
        }
    }
}
