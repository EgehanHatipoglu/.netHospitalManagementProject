using System;
using System.Collections.Generic;

namespace HospitalManagementAvolonia.Models
{
    /// <summary>
    /// Represents a drug/medication with stock tracking.
    /// </summary>
    public class Drug
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }  // e.g., "tablet", "ml", "mg"
        public int Stock { get; set; }
        public int LowStockThreshold { get; set; }

        public bool IsLowStock => Stock <= LowStockThreshold;

        public Drug(int id, string name, string unit, int stock, int lowStockThreshold = 10)
        {
            Id = id;
            Name = name;
            Unit = unit;
            Stock = stock;
            LowStockThreshold = lowStockThreshold;
        }

        public override string ToString() => $"{Name} ({Stock} {Unit}){(IsLowStock ? " ⚠️ Düşük Stok!" : "")}";
    }

    /// <summary>
    /// Represents a single drug line item in a prescription.
    /// </summary>
    public class PrescriptionItem
    {
        public Drug Drug { get; set; }
        public int Quantity { get; set; }
        public string Dosage { get; set; }  // e.g., "Günde 2x1"

        public PrescriptionItem(Drug drug, int quantity, string dosage)
        {
            Drug = drug;
            Quantity = quantity;
            Dosage = dosage;
        }

        public override string ToString() => $"{Drug.Name} × {Quantity} ({Dosage})";
    }

    /// <summary>
    /// Represents a Prescription issued during a patient visit.
    /// </summary>
    public class Prescription
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public DateTime Date { get; set; }
        public List<PrescriptionItem> Items { get; set; }
        public string PatientName { get; set; }
        public string DoctorName { get; set; }

        public Prescription(int id, int patientId, string patientName, int doctorId, string doctorName, DateTime date)
        {
            Id = id;
            PatientId = patientId;
            PatientName = patientName;
            DoctorId = doctorId;
            DoctorName = doctorName;
            Date = date;
            Items = new List<PrescriptionItem>();
        }

        public override string ToString() => $"Reçete #{Id} — {PatientName} ({Date:dd/MM/yyyy})";
    }
}
