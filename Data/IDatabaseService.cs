using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Data
{
    public interface IDatabaseService
    {
        Task InitializeDatabaseAsync();
        
        Task SaveDepartmentAsync(Department dept);
        Task<List<(int id, string name, int capacity)>> LoadDepartmentsAsync();
        
        Task SavePatientAsync(Patient p);
        Task DeletePatientAsync(int patientId);
        Task<List<(int id, string firstName, string lastName, string nationalId, string phone, string birthDate)>> LoadPatientsAsync();
        
        Task SaveDoctorAsync(Doctor d);
        Task DeleteDoctorAsync(int doctorId);
        Task<List<(int id, string firstName, string lastName, int departmentId, string phone)>> LoadDoctorsAsync();
        
        Task SaveAppointmentAsync(Appointment a);
        Task DeleteAppointmentAsync(int appointmentId);
        Task<List<(int id, int patientId, int doctorId, string startTime, string endTime, string status)>> LoadAppointmentsAsync();
        
        Task SaveVisitAsync(int patientId, int doctorId, DateTime visitDate, string notes);
        Task<List<(int patientId, int doctorId, string visitDate, string notes)>> LoadVisitsAsync();

        // Phase 3: Drug CRUD
        Task SaveDrugAsync(Drug drug);
        Task<List<(int id, string name, string unit, int stock, int threshold)>> LoadDrugsAsync();

        // Phase 3: Prescription CRUD
        Task SavePrescriptionAsync(Prescription rx);
        Task<List<(int id, int patientId, string patientName, int doctorId, string doctorName, string date)>> LoadPrescriptionsAsync();

        // Phase 3: Invoice CRUD
        Task SaveInvoiceAsync(Invoice inv);
        Task<List<(int id, int appId, string patientName, string doctorName, string date, double baseAmount, double insurancePct, string status)>> LoadInvoicesAsync();

        // Phase 3: Shift CRUD
        Task SaveShiftAsync(DoctorShift shift);
        Task DeleteShiftAsync(int shiftId);
        Task<List<(int id, int doctorId, string doctorName, int day, int startHour, int endHour)>> LoadShiftsAsync();
    }
}
