using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalManagementAvolonia.Models;
using HospitalManagementAvolonia.Services;

namespace HospitalManagementAvolonia.ViewModels
{
    public partial class AppointmentViewModel : ViewModelBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly IPatientService     _patientService;
        private readonly IDoctorService      _doctorService;

        public ObservableCollection<Appointment> Appointments { get; } = new();

        [ObservableProperty] private int?            _newPatientId;
        [ObservableProperty] private int?            _newDoctorId;
        [ObservableProperty] private DateTimeOffset? _newDate = DateTimeOffset.Now;

        // ✅ FIX: Was string — now proper TimeSpan for TimePicker binding
        [ObservableProperty] private TimeSpan? _newTime = new TimeSpan(9, 0, 0);

        [ObservableProperty] private Appointment? _selectedAppointment;

        public AppointmentViewModel(IAppointmentService args,
            IPatientService ps, IDoctorService ds)
        {
            _appointmentService = args;
            _patientService     = ps;
            _doctorService      = ds;
        }

        [RelayCommand]
        public async Task RefreshDataAsync()
        {
            var result = await _appointmentService.GetAllAppointmentsAsync();
            Appointments.Clear();
            foreach (var r in result) Appointments.Add(r);
        }

        [RelayCommand]
        public async Task CreateAppointmentAsync()
        {
            if (!NewPatientId.HasValue || !NewDoctorId.HasValue || !NewTime.HasValue)
            {
                ToastService.Instance.Warning("Hasta, doktor ve saat alanları zorunludur.");
                return;
            }

            var patient = await _patientService.GetPatientByIdAsync(NewPatientId.Value);
            var doctor  = await _doctorService.GetDoctorByIdAsync(NewDoctorId.Value);

            if (patient == null)
            { ToastService.Instance.Error($"Hasta ID {NewPatientId} bulunamadı."); return; }
            if (doctor == null)
            { ToastService.Instance.Error($"Doktor ID {NewDoctorId} bulunamadı."); return; }

            // ✅ FIX: Clean DateTime assembly — no string parsing
            var date = (NewDate?.DateTime ?? DateTime.Today).Date;
            var dt   = date + NewTime.Value;

            if (_appointmentService.HasConflict(doctor.Id, dt))
            { ToastService.Instance.Warning($"Dr. {doctor.FullName} bu saatte dolu!"); return; }

            if (_appointmentService.HasPatientConflict(patient.Id, dt))
            { ToastService.Instance.Warning($"{patient.FullName} bu saatte başka randevusu var!"); return; }

            var app = await _appointmentService.CreateAppointmentAsync(patient, doctor, dt);
            Appointments.Insert(0, app);

            NewPatientId = null;
            NewDoctorId  = null;
            NewTime      = new TimeSpan(9, 0, 0);

            ToastService.Instance.Success($"✓ Randevu oluşturuldu: {app.Patient.FullName} → Dr. {app.Doctor.FullName}");
        }

        [RelayCommand]
        public async Task DeleteAppointmentAsync()
        {
            if (SelectedAppointment == null) return;
            await _appointmentService.DeleteAppointmentAsync(SelectedAppointment.Id);
            Appointments.Remove(SelectedAppointment);
            SelectedAppointment = null;
            ToastService.Instance.Info("Randevu silindi.");
        }

        [RelayCommand]
        public void CalculateDensity()
        {
            var start = (NewDate?.DateTime ?? DateTime.Today).Date;
            var end   = start.AddDays(7);
            int count = _appointmentService.GetAppointmentsCount(start, end);
            ToastService.Instance.Info(
                $"{start:dd/MM} - {end:dd/MM} arasında {count} randevu (SegmentTree).");
        }

        [RelayCommand]
        public async Task ShowDoctorQueueAsync()
        {
            if (!NewDoctorId.HasValue) return;
            var apps = _appointmentService.GetAppointmentsForDoctor(
                NewDoctorId.Value, DateTime.Today);
            Appointments.Clear();
            foreach (var a in apps) Appointments.Add(a);
        }

        [RelayCommand]
        public async Task ExaminePatientAsync()
        {
            if (!NewDoctorId.HasValue) return;
            var app = await _appointmentService.ExaminePatientAsync(NewDoctorId.Value);
            if (app != null)
            {
                await RefreshDataAsync();
                ToastService.Instance.Success($"✓ {app.Patient.FullName} muayenesi tamamlandı.");
            }
            else
            {
                ToastService.Instance.Warning("Kuyrukta bekleyen hasta yok.");
            }
        }
    }
}
