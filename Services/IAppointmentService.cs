using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Services
{
    public interface IAppointmentService
    {
        Task InitializeAsync();
        Task<List<Appointment>> GetAllAppointmentsAsync();
        Task<Appointment> CreateAppointmentAsync(Patient patient, Doctor doctor, DateTime dateTime);
        Task UpdateAppointmentAsync(Appointment appointment);
        Task DeleteAppointmentAsync(int id);

        int GetAppointmentsCount(DateTime start, DateTime end);
        bool HasConflict(int doctorId, DateTime dt);
        bool HasPatientConflict(int patientId, DateTime dt);
        Task<Appointment?> ExaminePatientAsync(int doctorId);
        List<Appointment> GetAppointmentsForDoctor(int doctorId, DateTime date);
    }
}
