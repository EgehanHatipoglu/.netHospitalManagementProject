using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalManagementAvolonia.Models;
using HospitalManagementAvolonia.Services;

namespace HospitalManagementAvolonia.ViewModels
{
    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly IPatientService _patientService;
        private readonly IDoctorService _doctorService;
        private readonly IAppointmentService _appointmentService;

        [ObservableProperty] private int _totalPatients;
        [ObservableProperty] private int _totalDoctors;
        [ObservableProperty] private int _todayAppointments;
        [ObservableProperty] private int _emergencyPatients;

        public ObservableCollection<Appointment> TodayAppointmentList { get; } = new();

        public DashboardViewModel(IPatientService ps, IDoctorService ds, IAppointmentService args)
        {
            _patientService = ps;
            _doctorService = ds;
            _appointmentService = args;
        }

        [RelayCommand]
        public async Task RefreshDataAsync()
        {
            var patients = await _patientService.GetAllPatientsAsync();
            TotalPatients = patients.Count;

            var doctors = await _doctorService.GetAllDoctorsAsync();
            TotalDoctors = doctors.Count;

            var apps = await _appointmentService.GetAllAppointmentsAsync();
            var todayApps = apps.Where(x => x.Start.Date == DateTime.Today).ToList();
            TodayAppointments = todayApps.Count;

            TodayAppointmentList.Clear();
            foreach (var a in todayApps) TodayAppointmentList.Add(a);
        }
    }
}
