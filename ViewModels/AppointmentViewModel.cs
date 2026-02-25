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
        private readonly IPatientService _patientService;
        private readonly IDoctorService _doctorService;

        public ObservableCollection<Appointment> Appointments { get; } = new();

        [ObservableProperty] private int? _newPatientId;
        [ObservableProperty] private int? _newDoctorId;
        [ObservableProperty] private TimeSpan? _newTime;
        [ObservableProperty] private DateTimeOffset? _newDate = DateTimeOffset.Now;

        [ObservableProperty] private string _searchQuery = "";
        [ObservableProperty] private Appointment? _selectedAppointment;

        public AppointmentViewModel(IAppointmentService args, IPatientService ps, IDoctorService ds)
        {
            _appointmentService = args;
            _patientService = ps;
            _doctorService = ds;
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
            if (!NewPatientId.HasValue || !NewDoctorId.HasValue || !NewTime.HasValue) return;

            var patient = await _patientService.GetPatientByIdAsync(NewPatientId.Value);
            var doctor = await _doctorService.GetDoctorByIdAsync(NewDoctorId.Value);

            if (patient == null || doctor == null) return;

            var dt = (NewDate?.DateTime ?? DateTime.Today).Date + NewTime.Value;

            var app = await _appointmentService.CreateAppointmentAsync(patient, doctor, dt);
            Appointments.Add(app);

            NewPatientId = null;
            NewDoctorId = null;
            NewTime = null;
        }

        [RelayCommand]
        public async Task DeleteAppointmentAsync()
        {
            if (SelectedAppointment != null)
            {
                await _appointmentService.DeleteAppointmentAsync(SelectedAppointment.Id);
                Appointments.Remove(SelectedAppointment);
                SelectedAppointment = null;
            }
        }
    }
}
