using System;

namespace HospitalManagementAvolonia.Models
{
    public enum ShiftDay
    {
        Pazartesi = 0,
        Salı = 1,
        Çarşamba = 2,
        Perşembe = 3,
        Cuma = 4,
        Cumartesi = 5,
        Pazar = 6
    }

    /// <summary>
    /// Represents a scheduled work shift for a doctor on a specific day and hour range.
    /// </summary>
    public class DoctorShift
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public ShiftDay Day { get; set; }
        public int StartHour { get; set; }
        public int EndHour { get; set; }

        public DoctorShift(int id, int doctorId, string doctorName, ShiftDay day, int startHour, int endHour)
        {
            Id = id;
            DoctorId = doctorId;
            DoctorName = doctorName;
            Day = day;
            StartHour = startHour;
            EndHour = endHour;
        }

        public bool ConflictsWith(DoctorShift other)
        {
            if (other.DoctorId != DoctorId) return false;
            if (other.Day != Day) return false;
            // Overlapping check: ranges overlap if start < other.end and end > other.start
            return StartHour < other.EndHour && EndHour > other.StartHour;
        }

        public override string ToString() =>
            $"{DoctorName} — {Day} {StartHour:00}:00-{EndHour:00}:00";
    }
}
