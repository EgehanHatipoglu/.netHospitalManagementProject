using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HospitalManagementAvolonia.Data;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IDatabaseService _db;
        private readonly IPatientService _patientService;
        private readonly IDoctorService _doctorService;
        
        private readonly Dictionary<int, Appointment> _appointments = new();
        private int _appointmentIdCounter = 0;

        public AppointmentService(IDatabaseService db, IPatientService patientService, IDoctorService doctorService)
        {
            _db = db;
            _patientService = patientService;
            _doctorService = doctorService;
        }

        public async Task<List<Appointment>> GetAllAppointmentsAsync()
        {
            if (!_appointments.Any())
            {
                var dtos = await _db.LoadAppointmentsAsync();
                foreach (var dto in dtos)
                {
                    if (dto.id > _appointmentIdCounter) _appointmentIdCounter = dto.id;
                    
                    var p = await _patientService.GetPatientByIdAsync(dto.patientId);
                    var d = await _doctorService.GetDoctorByIdAsync(dto.doctorId);
                    
                    if (p != null && d != null && DateTime.TryParse(dto.startTime, out DateTime dt))
                    {
                        var app = new Appointment(dto.id, p, d, dt) { Status = dto.status };
                        _appointments[app.Id] = app;
                        d.DailyQueue.Enqueue(app); // Note: In a real app we might not want to re-enqueue historical appointments blindly
                    }
                }
            }
            return _appointments.Values.OrderBy(a => a.Start).ToList();
        }

        public async Task<Appointment> CreateAppointmentAsync(Patient patient, Doctor doctor, DateTime dateTime)
        {
            await GetAllAppointmentsAsync(); // ensure loaded
            _appointmentIdCounter++;
            var app = new Appointment(_appointmentIdCounter, patient, doctor, dateTime);
            _appointments[app.Id] = app;
            
            doctor.DailyQueue.Enqueue(app);
            await _db.SaveAppointmentAsync(app);
            return app;
        }

        public async Task UpdateAppointmentAsync(Appointment appointment)
        {
            _appointments[appointment.Id] = appointment;
            await _db.SaveAppointmentAsync(appointment);
        }

        public async Task DeleteAppointmentAsync(int id)
        {
            if (_appointments.Remove(id))
            {
                await _db.DeleteAppointmentAsync(id);
            }
        }
    }
}
