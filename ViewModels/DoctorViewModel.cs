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

        public PaginatedList<Doctor> Doctors { get; } = new(pageSize: 15);
        public ObservableCollection<Department> Departments { get; } = new();

        [ObservableProperty] private string _newFirstName = "";
        [ObservableProperty] private string _newLastName = "";
        [ObservableProperty] private string _newPhone = "";
        
        [ObservableProperty] private Department? _selectedDepartmentForNew;

        [ObservableProperty] private string _searchQuery = "";
        [ObservableProperty] private Doctor? _selectedDoctor;

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
            Doctors.Load(result);
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
            if (string.IsNullOrWhiteSpace(NewFirstName) || string.IsNullOrWhiteSpace(NewLastName) || SelectedDepartmentForNew == null) return;

            var d = await _doctorService.AddDoctorAsync(NewFirstName, NewLastName, SelectedDepartmentForNew.Id, NewPhone);
            
            NewFirstName = "";
            NewLastName = "";
            NewPhone = "";
            SelectedDepartmentForNew = null;
            
            await RefreshDataAsync();
        }

        [RelayCommand]
        public async Task DeleteDoctorAsync()
        {
            if (SelectedDoctor != null)
            {
                await _doctorService.DeleteDoctorAsync(SelectedDoctor.Id);
                await RefreshDataAsync();
                SelectedDoctor = null;
            }
        }
    }
}
