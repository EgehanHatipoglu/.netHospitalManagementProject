using System;

namespace HospitalManagementAvolonia.Models
{
    /// <summary>
    /// Appointment model with dynamic duration based on Student ID.
    /// </summary>
    public class Appointment
    {
        private const int StudentId = 230316064;
        public static readonly int AppointmentDuration = 15 + (StudentId % 5); // 19 minutes

        public int Id { get; set; }
        public Patient Patient { get; set; }
        public Doctor Doctor { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Status { get; set; }

        // Convenience properties for DataGrid bindings
        public string PatientName => $"{Patient.FirstName} {Patient.LastName}";
        public string DoctorName => $"Dr. {Doctor.FirstName} {Doctor.LastName}";
        public string DateTimeFormatted => Start.ToString("dd/MM/yyyy HH:mm");

        public Appointment(int id, Patient patient, Doctor doctor, DateTime start)
        {
            Id = id;
            Patient = patient;
            Doctor = doctor;
            Start = start;
            End = start.AddMinutes(AppointmentDuration);
            Status = "Waiting";
        }

        public override string ToString()
        {
            return $"Appointment #{Id} - {Patient.FirstName} {Patient.LastName} → Dr. {Doctor.FirstName} {Doctor.LastName} ({Start:dd/MM/yyyy HH:mm}) - {Status}";
        }
    }
}
