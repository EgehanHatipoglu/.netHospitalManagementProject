using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalManagementAvolonia.Data;
using HospitalManagementAvolonia.Models;
using HospitalManagementAvolonia.Services;

namespace HospitalManagementAvolonia.ViewModels
{
    public partial class DepartmentViewModel : ViewModelBase
    {
        private readonly IDepartmentService _departmentService;

        public ObservableCollection<Department> Departments { get; } = new();

        [ObservableProperty] private string _newDepartmentName = "";
        [ObservableProperty] private int _newDepartmentCapacity = 10;
        [ObservableProperty] private Department? _selectedDepartment;
        [ObservableProperty] private string _validationMessage = "";
        [ObservableProperty] private string _treeOutput = "";

        public DepartmentViewModel(IDepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        [RelayCommand]
        public async Task RefreshDataAsync()
        {
            var depts = await _departmentService.GetAllDepartmentsAsync();
            Departments.Clear();
            foreach (var d in depts)
            {
                Departments.Add(d);
            }
        }

        [RelayCommand]
        public async Task CreateDepartmentAsync()
        {
            ValidationMessage = "";

            if (string.IsNullOrWhiteSpace(NewDepartmentName))
            {
                ValidationMessage = "⚠ Bölüm adı boş olamaz!";
                return;
            }
            if (NewDepartmentCapacity <= 0)
            {
                ValidationMessage = "⚠ Kapasite 0'dan büyük olmalı!";
                return;
            }

            var dept = await _departmentService.AddDepartmentAsync(NewDepartmentName.Trim(), NewDepartmentCapacity);
            Departments.Add(dept);

            NewDepartmentName = "";
            NewDepartmentCapacity = 10;

            ToastService.Instance.Success($"✓ '{dept.Name}' bölümü oluşturuldu.");
        }

        [RelayCommand]
        public void ShowHierarchy()
        {
            var hierarchy = _departmentService.GetHierarchy();
            if (hierarchy.Count == 0)
            {
                TreeOutput = "Henüz bölüm bulunmuyor.";
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("🏥 Hastane Organizasyonu");
            foreach (var (name, level, doctorCount) in hierarchy)
            {
                var indent = new string(' ', (level + 1) * 2);
                sb.AppendLine($"{indent}└── {name} ({doctorCount} Doktor)");
            }
            TreeOutput = sb.ToString().TrimEnd();
        }
    }
}

