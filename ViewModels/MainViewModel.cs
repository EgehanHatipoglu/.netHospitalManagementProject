using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalManagementAvolonia.Data;
using HospitalManagementAvolonia.Services;

namespace HospitalManagementAvolonia.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IPatientService _patientService;
        private readonly IDoctorService _doctorService;
        private readonly IAppointmentService _appointmentService;
        private readonly IDepartmentService _departmentService;
        private readonly IEmergencyService _emergencyService;
        private readonly IUndoService _undoService;
        private readonly IDatabaseService _db;

        [ObservableProperty] private string _activePanel = "Dashboard";
        [ObservableProperty] private string _statusMessage = "✓ Sistem hazır.";
        [ObservableProperty] private bool _isSidebarCollapsed = false;

        // Undo Properties
        [ObservableProperty] private string _undoPeekMessage = "";
        [ObservableProperty] private string _undoResultMessage = "";

        // --- Sub-ViewModels ---
        public DashboardViewModel    Dashboard    { get; }
        public PatientViewModel      Patients     { get; }
        public DoctorViewModel       Doctors      { get; }
        public AppointmentViewModel  Appointments { get; }
        public EmergencyViewModel    Emergency    { get; }
        public DepartmentViewModel   Departments  { get; }
        public StatsViewModel        Stats        { get; }
        public UndoViewModel         Undo         { get; }

        public PrescriptionViewModel Prescriptions { get; }
        public BillingViewModel      Billing       { get; }
        public ShiftViewModel        Shifts        { get; }

        public ToastService Toast => ToastService.Instance;

        public MainViewModel(
            IPatientService patientService,
            IDoctorService doctorService,
            IAppointmentService appointmentService,
            IDepartmentService departmentService,
            IEmergencyService emergencyService,
            IUndoService undoService,
            IDatabaseService db)
        {
            _patientService     = patientService;
            _doctorService      = doctorService;
            _appointmentService = appointmentService;
            _departmentService  = departmentService;
            _emergencyService   = emergencyService;
            _undoService        = undoService;
            _db                 = db;

            Dashboard     = new DashboardViewModel(_patientService, _doctorService, _appointmentService);
            Patients      = new PatientViewModel(_patientService);
            Doctors       = new DoctorViewModel(_doctorService);
            Appointments  = new AppointmentViewModel(_appointmentService, _patientService, _doctorService);
            Emergency     = new EmergencyViewModel(_emergencyService, _patientService);
            Departments   = new DepartmentViewModel(_departmentService);
            Stats         = new StatsViewModel(_patientService, _doctorService, _appointmentService);
            Prescriptions = new PrescriptionViewModel(_db, _patientService, _doctorService);
            Billing       = new BillingViewModel(_db);
            Shifts        = new ShiftViewModel(_db, _doctorService);
            Undo          = new UndoViewModel(_undoService);
        }

        public async System.Threading.Tasks.Task InitializeAllAsync()
        {
            // First load departments since doctors depend on them
            await _departmentService.InitializeAsync();
            
            // Patients and Doctors can load in parallel
            await System.Threading.Tasks.Task.WhenAll(
                _patientService.InitializeAsync(),
                _doctorService.InitializeAsync()
            );
            
            // Appointments depend on both Patients and Doctors
            await _appointmentService.InitializeAsync();

            await Doctors.RefreshDataAsync();
            await Dashboard.RefreshDataAsync();
        }

        [RelayCommand]
        public void ExecuteUndo()
        {
            var op = _undoService.UndoLastOperation();
            if (op != null)
            {
                UndoResultMessage = $"Geri Alındı: {op}";
            }
            else
            {
                UndoResultMessage = "Geri alınacak işlem yok.";
            }
            UpdateUndoPeek();
        }

        public void UpdateUndoPeek()
        {
            UndoPeekMessage = _undoService.Peek();
        }

        [RelayCommand]
        private void Navigate(string panelName)
        {
            if (string.IsNullOrWhiteSpace(panelName)) return;
            ActivePanel = panelName;

            switch (panelName)
            {
                case "Dashboard":    Dashboard.RefreshDataCommand.Execute(null);     break;
                case "Patients":     Patients.RefreshDataCommand.Execute(null);      break;
                case "Doctors":      Doctors.RefreshDataCommand.Execute(null);       break;
                case "Appointments": Appointments.RefreshDataCommand.Execute(null);  break;
                case "Stats":        Stats.RefreshDataCommand.Execute(null);         break;
                case "Prescription": Prescriptions.RefreshDataCommand.Execute(null); break;
                case "Billing":      Billing.RefreshDataCommand.Execute(null);       break;
                case "Shifts":       Shifts.RefreshDataCommand.Execute(null);        break;
                case "Undo":         Undo.RefreshDataCommand.Execute(null);          break;
            }
        }

        [RelayCommand]
        private void ToggleSidebar() => IsSidebarCollapsed = !IsSidebarCollapsed;

        [RelayCommand]
        private void ToggleTheme()
        {
            var app = Avalonia.Application.Current;
            if (app != null)
            {
                if (app.RequestedThemeVariant == Avalonia.Styling.ThemeVariant.Dark)
                {
                    app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
                    ToastService.Instance.Info("Açık temaya geçildi.");
                }
                else
                {
                    app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
                    ToastService.Instance.Info("Karanlık temaya geçildi.");
                }
            }
        }
    }
}
