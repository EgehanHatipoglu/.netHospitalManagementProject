using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using HospitalManagementAvolonia.Data;
using HospitalManagementAvolonia.DataStructures;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia;

public partial class MainWindow : Window
{
    private StackPanel[] _allPanels = null!;

    public MainWindow()
    {
        InitializeComponent();
        
        // Resolve the entire MVVM data context tree from DI and bind it to the view
        DataContext = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ViewModels.MainViewModel>(App.Services!);

        _allPanels = new[] { PanelDashboard, PanelPatients, PanelDoctors, PanelAppointments, PanelEmergency,
                             PanelBST, PanelAVL, PanelDepartments, PanelUndo, PanelStats,
                             PanelPrescription, PanelShifts };

        // Feature 4: Responsive Sidebar
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
    }

    public async System.Threading.Tasks.Task InitializeAsync()
    {
        if (DataContext is ViewModels.MainViewModel mvm)
        {
            await mvm.InitializeAllAsync();
        }
        
        // Initial Panel View
        ShowPanel(PanelDashboard);
    }

    private void ShowPanel(StackPanel targetPanel)
    {
        foreach (var p in _allPanels)
        {
            if (p != null) p.IsVisible = (p == targetPanel);
        }
    }
}
