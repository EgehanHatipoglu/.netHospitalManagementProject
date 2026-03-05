using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HospitalManagementAvolonia.Data;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Services
{
    public class AppointmentService : ServiceBase, IAppointmentService
    {
        private readonly IDatabaseService _db;
        private readonly IPatientService _patientService;
        private readonly IDoctorService _doctorService;
        private readonly Dictionary<int, Appointment> _appointments = new();
        private int _appointmentIdCounter = 0;

        private readonly HospitalManagementAvolonia.DataStructures.AppointmentSegmentTree _segmentTree = new(DateTime.Today.AddDays(-30), 100);

        public AppointmentService(IDatabaseService db, IPatientService patientService, IDoctorService doctorService)
        {
            _db = db;
            _patientService = patientService;
            _doctorService = doctorService;
        }

        public async Task InitializeAsync()
        {
            await EnsureInitializedAsync(async () =>
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
                        
                        _segmentTree.AddAppointment(app.Start);
                    }
                }
            });
        }

        public async Task<List<Appointment>> GetAllAppointmentsAsync()
        {
            if (!IsInitialized) await InitializeAsync();
            return _appointments.Values.OrderBy(a => a.Start).ToList();
        }

        public async Task<Appointment> CreateAppointmentAsync(Patient patient, Doctor doctor, DateTime dateTime)
        {
            if (!IsInitialized) await InitializeAsync();

            _appointmentIdCounter++;
            var app = new Appointment(_appointmentIdCounter, patient, doctor, dateTime);
            _appointments[app.Id] = app;
            
            doctor.DailyQueue.Enqueue(app);
            await _db.SaveAppointmentAsync(app);
            
            _segmentTree.AddAppointment(app.Start);

            return app;
        }

        public async Task UpdateAppointmentAsync(Appointment appointment)
        {
            if (!IsInitialized) await InitializeAsync();

            if (_appointments.TryGetValue(appointment.Id, out var oldApp))
            {
                if (oldApp.Start.Date != appointment.Start.Date)
                {
                    _segmentTree.RemoveAppointment(oldApp.Start);
                    _segmentTree.AddAppointment(appointment.Start);
                }
            }

            _appointments[appointment.Id] = appointment;
            await _db.SaveAppointmentAsync(appointment);
        }

        public async Task DeleteAppointmentAsync(int id)
        {
            if (!IsInitialized) await InitializeAsync();

            if (_appointments.TryGetValue(id, out var app))
            {
                _appointments.Remove(id);
                _segmentTree.RemoveAppointment(app.Start);
                await _db.DeleteAppointmentAsync(id);
            }
        }

        public int GetAppointmentsCount(DateTime start, DateTime end)
        {
            return _segmentTree.QueryRange(start, end);
        }

        public bool HasConflict(int doctorId, DateTime dt)
        {
            var end = dt.AddMinutes(Appointment.AppointmentDuration);
            return _appointments.Values.Any(a => a.Doctor.Id == doctorId && dt < a.End && a.Start < end);
        }

        public bool HasPatientConflict(int patientId, DateTime dt)
        {
            var end = dt.AddMinutes(Appointment.AppointmentDuration);
            return _appointments.Values.Any(a => a.Patient.Id == patientId && dt < a.End && a.Start < end);
        }

        public async Task<Appointment?> ExaminePatientAsync(int doctorId)
        {
            if (!IsInitialized) await InitializeAsync();
            var d = await _doctorService.GetDoctorByIdAsync(doctorId);
            if (d == null) return null;

            var app = d.DailyQueue.Dequeue();
            if (app == null) return null;

            app.Patient.AddVisit(DateTime.Now, d, "Muayene tamamlandı");
            app.Status = "Completed";

            await _db.SaveVisitAsync(app.Patient.Id, d.Id, DateTime.Now, "Muayene tamamlandı");
            await _db.SaveAppointmentAsync(app);
            
            return app;
        }

        public List<Appointment> GetAppointmentsForDoctor(int doctorId, DateTime date)
        {
            return _appointments.Values
                .Where(a => a.Doctor.Id == doctorId && a.Start.Date == date.Date)
                .OrderBy(a => a.Start)
                .ToList();
        }
    }
}
