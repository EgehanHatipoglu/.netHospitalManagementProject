using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.ViewModels
{
    public partial class DepartmentViewModel : ViewModelBase
    {
        public ObservableCollection<Department> Departments { get; } = new();

        [ObservableProperty] private string _newDepartmentName = "";
        [ObservableProperty] private int _newDepartmentCapacity = 10;

        [RelayCommand]
        public void CreateDepartment()
        {
            if (string.IsNullOrWhiteSpace(NewDepartmentName) || NewDepartmentCapacity <= 0) return;

            // In a real app we'd have IDepartmentService
            // For now, MainWindow handles DB saving for Departments natively, but we will move it.
            // ...
        }
    }
}
