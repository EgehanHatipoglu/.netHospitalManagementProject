using System.Collections.Generic;
using Avalonia.Controls;
using HospitalManagementAvolonia.Data;
using HospitalManagementAvolonia.Services;
using HospitalManagementAvolonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagementAvolonia;

public partial class MainWindow : Window
{
    // ✅ FIX: Only UI mapping lives here — zero business logic
    private readonly Dictionary<string, StackPanel> _panelMap = new();
    private StackPanel[] _allPanels = null!;

    public MainWindow()
    {
        InitializeComponent();

        var vm = App.Services!.GetRequiredService<MainViewModel>();
        DataContext = vm;

        _allPanels = new[]
        {
            PanelDashboard, PanelPatients, PanelDoctors, PanelAppointments, PanelEmergency,
            PanelBST, PanelAVL, PanelDepartments, PanelUndo, PanelStats,
            PanelPrescription, PanelShifts, PanelBilling
        };

        _panelMap["Dashboard"]    = PanelDashboard;
        _panelMap["Patients"]     = PanelPatients;
        _panelMap["Doctors"]      = PanelDoctors;
        _panelMap["Appointments"] = PanelAppointments;
        _panelMap["Emergency"]    = PanelEmergency;
        _panelMap["BST"]          = PanelBST;
        _panelMap["AVL"]          = PanelAVL;
        _panelMap["Departments"]  = PanelDepartments;
        _panelMap["Undo"]         = PanelUndo;
        _panelMap["Stats"]        = PanelStats;
        _panelMap["Prescription"] = PanelPrescription;
        _panelMap["Shifts"]       = PanelShifts;
        _panelMap["Billing"]      = PanelBilling;

        // ✅ FIX: Subscribe to NavigationService — no PropertyChanged hacks
        vm.Navigation.Navigated += OnNavigated;

        // Responsive sidebar
        SizeChanged += (_, e) =>
        {
            if (vm.IsSidebarCollapsed != (e.NewSize.Width < 900))
                vm.IsSidebarCollapsed = e.NewSize.Width < 900;
        };

        Opened += async (_, _) => await InitializeAsync();
    }

    // ✅ FIX: Only responsible for swapping panels — called by NavigationService event
    private void OnNavigated(string panelName)
    {
        if (_panelMap.TryGetValue(panelName, out var panel))
            ShowPanel(panel);
    }

    private void ShowPanel(StackPanel target)
    {
        foreach (var p in _allPanels)
            if (p != null) p.IsVisible = p == target;
    }

    private async System.Threading.Tasks.Task InitializeAsync()
    {
        var db = App.Services!.GetRequiredService<IDatabaseService>();
        await db.InitializeDatabaseAsync();

        if (DataContext is MainViewModel vm)
        {
            await vm.InitializeAllAsync();
            // Navigate to default panel after init
            vm.Navigation.NavigateTo("Dashboard");
        }
    }
}
