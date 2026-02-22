using System.Collections.Generic;

namespace HospitalManagementWPF.Models
{
    /// <summary>
    /// Department model with capacity management.
    /// </summary>
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Capacity { get; set; }
        public List<Doctor> Doctors { get; set; }

        public int DoctorCount => Doctors.Count;

        public Department(int id, string name, int capacity)
        {
            Id = id;
            Name = name;
            Capacity = capacity;
            Doctors = new List<Doctor>();
        }

        /// <summary>
        /// Adds a doctor only if capacity allows.
        /// </summary>
        public bool AddDoctor(Doctor doctor)
        {
            if (Doctors.Count >= Capacity)
                return false;
            Doctors.Add(doctor);
            return true;
        }

        public override string ToString()
        {
            return $"ID: {Id} | {Name} | Doctors: {Doctors.Count}/{Capacity}";
        }
    }
}
