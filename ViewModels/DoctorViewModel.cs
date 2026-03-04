using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalManagementAvolonia.Helpers;
using HospitalManagementAvolonia.Models;
using HospitalManagementAvolonia.Services;

namespace HospitalManagementAvolonia.ViewModels
{
    public partial class DoctorViewModel : ViewModelBase
    {
        private readonly IDoctorService _doctorService;

        // ✅ FIX: DataGrid'in ItemsSource'u PagedItems'a bağlanmalı, PaginatedList'e değil
        public PaginatedList<Doctor> Pagination { get; } = new(pageSize: 15);

        // ✅ DataGrid bu collection'a bağlanır: ItemsSource="{Binding Doctors.Pagination.PagedItems}"
        public ObservableCollection<Department> Departments { get; } = new();

        [ObservableProperty] private string _newFirstName = "";
        [ObservableProperty] private string _newLastName = "";
        [ObservableProperty] private string _newPhone = "";

        [ObservableProperty] private Department? _selectedDepartmentForNew;
        [ObservableProperty] private string _searchQuery = "";
        [ObservableProperty] private Doctor? _selectedDoctor;

        // Validation mesajı
        [ObservableProperty] private string _validationMessage = "";

        public DoctorViewModel(IDoctorService ds)
        {
            _doctorService = ds;
        }

        partial void OnSearchQueryChanged(string value)
        {
            _ = FilterDoctorsAsync(value);
        }

        private async Task FilterDoctorsAsync(string query)
        {
            var result = await _doctorService.SearchDoctorsAsync(query);
            Pagination.Load(result);
        }

        [RelayCommand]
        public async Task RefreshDataAsync()
        {
            var depts = await _doctorService.GetDepartmentsAsync();
            Departments.Clear();
            foreach (var d in depts) Departments.Add(d);

            await FilterDoctorsAsync(SearchQuery);
        }

        [RelayCommand]
        public async Task RegisterDoctorAsync()
        {
            ValidationMessage = "";

            if (string.IsNullOrWhiteSpace(NewFirstName))
            {
                ValidationMessage = "⚠ Ad boş olamaz!";
                return;
            }
            if (string.IsNullOrWhiteSpace(NewLastName))
            {
                ValidationMessage = "⚠ Soyad boş olamaz!";
                return;
            }
            if (SelectedDepartmentForNew == null)
            {
                ValidationMessage = "⚠ Bölüm seçilmedi!";
                return;
            }

            await _doctorService.AddDoctorAsync(
                NewFirstName.Trim(),
                NewLastName.Trim(),
                SelectedDepartmentForNew.Id,
                NewPhone.Trim()
            );

            NewFirstName = "";
            NewLastName = "";
            NewPhone = "";
            SelectedDepartmentForNew = null;

            await RefreshDataAsync();
            ToastService.Instance.Success("✓ Doktor kaydedildi.");
        }

        [RelayCommand]
        public async Task DeleteDoctorAsync()
        {
            if (SelectedDoctor == null)
            {
                ValidationMessage = "⚠ Silmek için doktor seçin!";
                return;
            }

            await _doctorService.DeleteDoctorAsync(SelectedDoctor.Id);
            await RefreshDataAsync();
            ValidationMessage = "";
            ToastService.Instance.Info("ℹ Doktor başarıyla silindi.");
        }

        // --- Network (Graph) Integration ---
        [ObservableProperty] private Doctor? _selectedReferralDoctor;
        public ObservableCollection<string> NetworkPath { get; } = new();

        [RelayCommand]
        public void AddReferral()
        {
            if (SelectedDoctor != null && SelectedReferralDoctor != null && SelectedDoctor.Id != SelectedReferralDoctor.Id)
            {
                _doctorService.AddReferral(SelectedDoctor, SelectedReferralDoctor);
                ToastService.Instance.Success($"{SelectedDoctor.FullName} -> {SelectedReferralDoctor.FullName} sevk ağına eklendi.");
            }
            else
            {
                ToastService.Instance.Warning("Lütfen geçerli iki farklı doktor seçin.");
            }
        }

        [RelayCommand]
        public void FindReferralPath()
        {
            if (SelectedDoctor != null && SelectedReferralDoctor != null)
            {
                var path = _doctorService.GetReferralPathBFS(SelectedDoctor.Id, SelectedReferralDoctor.Id);
                NetworkPath.Clear();
                foreach (var p in path) NetworkPath.Add(p);
            }
        }

        [RelayCommand]
        public void ShowFullNetwork()
        {
            if (SelectedDoctor != null)
            {
                var net = _doctorService.GetReferralNetworkDFS(SelectedDoctor.Id);
                NetworkPath.Clear();
                foreach (var node in net) NetworkPath.Add(node);
            }
        }
    }
}
