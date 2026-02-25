using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalManagementAvolonia.Services;

namespace HospitalManagementAvolonia.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IPatientService _patientService;
        private readonly IDoctorService _doctorService;
        private readonly IAppointmentService _appointmentService;

        [ObservableProperty] private string _activePanel = "Dashboard";
        [ObservableProperty] private string _statusMessage = "✓ Sistem hazır.";
        [ObservableProperty] private bool _isSidebarCollapsed = false;

        // Sub-ViewModels
        public DashboardViewModel Dashboard { get; }
        public PatientViewModel Patients { get; }
        public DoctorViewModel Doctors { get; }
        public AppointmentViewModel Appointments { get; }
        public EmergencyViewModel Emergency { get; }
        public DepartmentViewModel Departments { get; }
        public StatsViewModel Stats { get; }

        // Toast overlay binding
        public ToastService Toast => ToastService.Instance;
        
        public MainViewModel(
            IPatientService patientService, 
            IDoctorService doctorService, 
            IAppointmentService appointmentService)
        {
            _patientService = patientService;
            _doctorService = doctorService;
            _appointmentService = appointmentService;

            Dashboard    = new DashboardViewModel(_patientService, _doctorService, _appointmentService);
            Patients     = new PatientViewModel(_patientService);
            Doctors      = new DoctorViewModel(_doctorService);
            Appointments = new AppointmentViewModel(_appointmentService, _patientService, _doctorService);
            Emergency    = new EmergencyViewModel();
            Departments  = new DepartmentViewModel();
            Stats        = new StatsViewModel(_patientService, _doctorService, _appointmentService);
        }

        [RelayCommand]
        private void Navigate(string panelName)
        {
            if (string.IsNullOrWhiteSpace(panelName)) return;
            ActivePanel = panelName;
            
            if (panelName == "Dashboard")    Dashboard.RefreshDataCommand.Execute(null);
            if (panelName == "Patients")     Patients.RefreshDataCommand.Execute(null);
            if (panelName == "Doctors")      Doctors.RefreshDataCommand.Execute(null);
            if (panelName == "Appointments") Appointments.RefreshDataCommand.Execute(null);
            if (panelName == "Stats")        Stats.RefreshDataCommand.Execute(null);
        }

        [RelayCommand]
        private void ToggleSidebar() => IsSidebarCollapsed = !IsSidebarCollapsed;
    }
}
