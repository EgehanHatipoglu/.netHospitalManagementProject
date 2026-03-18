using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalManagementAvolonia.Services;

namespace HospitalManagementAvolonia.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IPatientService      _patientService;
        private readonly IDoctorService       _doctorService;
        private readonly IAppointmentService  _appointmentService;
        private readonly IDepartmentService   _departmentService;
        private readonly IEmergencyService    _emergencyService;
        private readonly IUndoService         _undoService;
        private readonly INavigationService   _navigationService;   // ✅ NEW

        [ObservableProperty] private bool   _isSidebarCollapsed = false;
        [ObservableProperty] private bool   _isLightTheme       = false;

        public string ThemeButtonText => IsLightTheme ? "☀️  Açık Tema" : "🌙  Koyu Tema";
        public string ThemeButtonIcon => IsLightTheme ? "☀️" : "🌙";

        // ── Sub-ViewModels ────────────────────────────────────────────────────
        public DashboardViewModel    Dashboard     { get; }
        public PatientViewModel      Patients      { get; }
        public DoctorViewModel       Doctors       { get; }
        public AppointmentViewModel  Appointments  { get; }
        public EmergencyViewModel    Emergency     { get; }
        public DepartmentViewModel   Departments   { get; }
        public StatsViewModel        Stats         { get; }
        public UndoViewModel         Undo          { get; }
        public PrescriptionViewModel Prescriptions { get; }
        public BillingViewModel      Billing       { get; }
        public ShiftViewModel        Shifts        { get; }

        public ToastService          Toast         => ToastService.Instance;

        // ✅ Expose navigation service so MainWindow can subscribe
        public INavigationService Navigation => _navigationService;

        public MainViewModel(
            IPatientService      patientService,
            IDoctorService       doctorService,
            IAppointmentService  appointmentService,
            IDepartmentService   departmentService,
            IEmergencyService    emergencyService,
            IUndoService         undoService,
            INavigationService   navigationService,   // ✅ injected
            IBillingService      billingService,
            IPrescriptionService prescriptionService,
            IShiftService        shiftService)
        {
            _patientService     = patientService;
            _doctorService      = doctorService;
            _appointmentService = appointmentService;
            _departmentService  = departmentService;
            _emergencyService   = emergencyService;
            _undoService        = undoService;
            _navigationService  = navigationService;

            Dashboard     = new DashboardViewModel(_patientService, _doctorService, _appointmentService);
            Patients      = new PatientViewModel(_patientService);
            Doctors       = new DoctorViewModel(_doctorService);
            Appointments  = new AppointmentViewModel(_appointmentService, _patientService, _doctorService);
            Emergency     = new EmergencyViewModel(_emergencyService, _patientService);
            Departments   = new DepartmentViewModel(_departmentService);
            Stats         = new StatsViewModel(_patientService, _doctorService, _appointmentService);
            Prescriptions = new PrescriptionViewModel(prescriptionService, _patientService, _doctorService);
            Billing       = new BillingViewModel(billingService);
            Shifts        = new ShiftViewModel(shiftService, _doctorService);
            Undo          = new UndoViewModel(_undoService);

            // ✅ Register refresh callbacks — NavigationService calls these on navigate
            if (_navigationService is NavigationService ns)
            {
                ns.RegisterRefresh("Dashboard",    () => Dashboard.RefreshDataCommand.Execute(null));
                ns.RegisterRefresh("Patients",     () => Patients.RefreshDataCommand.Execute(null));
                ns.RegisterRefresh("Doctors",      () => Doctors.RefreshDataCommand.Execute(null));
                ns.RegisterRefresh("Appointments", () => Appointments.RefreshDataCommand.Execute(null));
                ns.RegisterRefresh("Stats",        () => Stats.RefreshDataCommand.Execute(null));
                ns.RegisterRefresh("Prescription", () => Prescriptions.RefreshDataCommand.Execute(null));
                ns.RegisterRefresh("Billing",      () => Billing.RefreshDataCommand.Execute(null));
                ns.RegisterRefresh("Shifts",       () => Shifts.RefreshDataCommand.Execute(null));
                ns.RegisterRefresh("Undo",         () => Undo.RefreshDataCommand.Execute(null));
            }
        }

        public async Task InitializeAllAsync()
        {
            await _departmentService.InitializeAsync();
            await Task.WhenAll(
                _patientService.InitializeAsync(),
                _doctorService.InitializeAsync());
            await _appointmentService.InitializeAsync();

            await Doctors.RefreshDataAsync();
            await Task.WhenAll(
                Prescriptions.RefreshDataAsync(),
                Billing.RefreshDataAsync(),
                Shifts.RefreshDataAsync());
            await Dashboard.RefreshDataAsync();
        }

        // ✅ FIX: NavCommand now delegates to NavigationService — no switch needed
        [RelayCommand]
        private void Navigate(string panelName) => _navigationService.NavigateTo(panelName);

        [RelayCommand]
        private void ToggleSidebar() => IsSidebarCollapsed = !IsSidebarCollapsed;

        [RelayCommand]
        private void ToggleTheme()
        {
            var app = Avalonia.Application.Current;
            if (app == null) return;
            IsLightTheme = !IsLightTheme;
            app.RequestedThemeVariant = IsLightTheme
                ? Avalonia.Styling.ThemeVariant.Light
                : Avalonia.Styling.ThemeVariant.Dark;
            OnPropertyChanged(nameof(ThemeButtonText));
            OnPropertyChanged(nameof(ThemeButtonIcon));
            ToastService.Instance.Info(IsLightTheme ? "Açık temaya geçildi." : "Karanlık temaya geçildi.");
        }
    }
}
