using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls;
using HospitalManagementAvolonia.Data;

namespace HospitalManagementAvolonia;

public partial class MainWindow : Window
{
    private StackPanel[] _allPanels = null!;
    private readonly Dictionary<string, StackPanel> _panelMap = new();

    public MainWindow()
    {
        InitializeComponent();

        // Resolve the entire MVVM data context tree from DI and bind it to the view
        DataContext = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
            .GetRequiredService<ViewModels.MainViewModel>(App.Services!);

        _allPanels = new[]
        {
            PanelDashboard, PanelPatients, PanelDoctors, PanelAppointments, PanelEmergency,
            PanelBST, PanelAVL, PanelDepartments, PanelUndo, PanelStats,
            PanelPrescription, PanelShifts, PanelBilling
        };

        // Build a map from panel name to panel control
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

        // Feature: Responsive Sidebar
        this.SizeChanged += (s, e) =>
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                if (e.NewSize.Width < 900 && !vm.IsSidebarCollapsed)
                    vm.IsSidebarCollapsed = true;
                else if (e.NewSize.Width >= 900 && vm.IsSidebarCollapsed)
                    vm.IsSidebarCollapsed = false;
            }
        };

        // Initialize everything sequentially when window opens
        this.Opened += async (s, e) => await InitializeAsync();
    }

    public async System.Threading.Tasks.Task InitializeAsync()
    {
        var db = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
            .GetRequiredService<IDatabaseService>(App.Services!);
        await db.InitializeDatabaseAsync();

        if (DataContext is ViewModels.MainViewModel mvm)
        {
            await mvm.InitializeAllAsync();

            // Listen to ActivePanel changes from ViewModel so ShowPanel() is triggered automatically
            mvm.PropertyChanged += OnViewModelPropertyChanged;
        }

        // Initial Panel View
        ShowPanel(PanelDashboard);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModels.MainViewModel.ActivePanel)
            && sender is ViewModels.MainViewModel vm)
        {
            if (_panelMap.TryGetValue(vm.ActivePanel, out var panel))
                ShowPanel(panel);
        }
    }

    private void ShowPanel(StackPanel targetPanel)
    {
        foreach (var p in _allPanels)
        {
            if (p != null) p.IsVisible = (p == targetPanel);
        }
    }
}
