using System;

namespace HospitalManagementAvolonia.Models
{
    /// <summary>
    /// Represents a billing invoice for an appointment.
    /// </summary>
    public class Invoice
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public string PatientName { get; set; }
        public string DoctorName { get; set; }
        public DateTime Date { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal InsuranceCoveragePercent { get; set; }
        public string Status { get; set; }  // "Pending", "Paid", "Cancelled"

        public decimal InsuranceAmount => Math.Round(BaseAmount * InsuranceCoveragePercent / 100m, 2);
        public decimal PatientPayment => Math.Round(BaseAmount - InsuranceAmount, 2);

        public Invoice(int id, int appointmentId, string patientName, string doctorName, DateTime date,
                       decimal baseAmount, decimal insuranceCoveragePercent)
        {
            Id = id;
            AppointmentId = appointmentId;
            PatientName = patientName;
            DoctorName = doctorName;
            Date = date;
            BaseAmount = baseAmount;
            InsuranceCoveragePercent = insuranceCoveragePercent;
            Status = "Pending";
        }

        public override string ToString() =>
            $"Fatura #{Id} — {PatientName} | Tutar: ₺{BaseAmount:F2} | Hasta Payı: ₺{PatientPayment:F2} | {Status}";
    }
}
