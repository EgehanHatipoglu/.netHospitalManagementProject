using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalManagementAvolonia.Helpers;
using HospitalManagementAvolonia.Models;
using HospitalManagementAvolonia.Services;

namespace HospitalManagementAvolonia.ViewModels
{
    public partial class PatientViewModel : ViewModelBase
    {
        private readonly IPatientService _patientService;

        public PaginatedList<Patient> Patients { get; } = new(pageSize: 15);

        [ObservableProperty] private string _newFirstName = "";
        [ObservableProperty] private string _newLastName = "";
        [ObservableProperty] private string _newNationalId = "";
        [ObservableProperty] private string _newPhone = "";
        [ObservableProperty] private DateTimeOffset? _newBirthDate = DateTimeOffset.Now;

        [ObservableProperty] private string _searchQuery = "";
        [ObservableProperty] private Patient? _selectedPatient;

        // BST panel properties
        [ObservableProperty] private string _bstSearchQuery = "";
        [ObservableProperty] private string _bstSearchResult = "";
        public ObservableCollection<Patient> BstPatients { get; } = new();

        // AVL panel properties
        [ObservableProperty] private string _avlSearchQuery = "";
        [ObservableProperty] private string _avlSearchResult = "";
        public ObservableCollection<Patient> AvlPatients { get; } = new();

        [ObservableProperty] private bool _isEditing;
        private int? _editingPatientId;

        // Profile Properties
        [ObservableProperty] private bool _isProfileVisible;
        [ObservableProperty] private string _profileName = "";
        [ObservableProperty] private string _profileDetails = "";
        public ObservableCollection<string> ProfileAppointments { get; } = new();

        public PatientViewModel(IPatientService ps)
        {
            _patientService = ps;
        }

        partial void OnSearchQueryChanged(string value)
        {
            _ = FilterPatientsAsync(value);
        }

        private async Task FilterPatientsAsync(string query)
        {
            var result = await _patientService.SearchPatientsAsync(query);
            Patients.Load(result);
        }

        [RelayCommand]
        public async Task RefreshDataAsync()
        {
            await FilterPatientsAsync(SearchQuery);
        }

        [RelayCommand]
        public async Task RegisterPatientAsync()
        {
            if (string.IsNullOrWhiteSpace(NewFirstName) || string.IsNullOrWhiteSpace(NewLastName)) return;

            var bd = NewBirthDate?.DateTime ?? DateTime.Today;

            if (IsEditing && _editingPatientId.HasValue)
            {
                var existing = await _patientService.GetPatientByIdAsync(_editingPatientId.Value);
                if (existing != null)
                {
                    existing.FirstName = NewFirstName;
                    existing.LastName = NewLastName;
                    existing.NationalId = NewNationalId;
                    existing.Phone = NewPhone;
                    existing.BirthDate = bd;
                    await _patientService.UpdatePatientAsync(existing);
                }
                IsEditing = false;
                _editingPatientId = null;
            }
            else
            {
                var p = await _patientService.AddPatientAsync(NewFirstName, NewLastName, NewNationalId, NewPhone, bd);
            }

            ResetForm();
            await RefreshDataAsync();
        }

        [RelayCommand]
        public void EditPatient()
        {
            if (SelectedPatient == null) return;
            IsEditing = true;
            _editingPatientId = SelectedPatient.Id;
            NewFirstName = SelectedPatient.FirstName;
            NewLastName = SelectedPatient.LastName;
            NewNationalId = SelectedPatient.NationalId;
            NewPhone = SelectedPatient.Phone;
            NewBirthDate = new DateTimeOffset(SelectedPatient.BirthDate);
        }

        [RelayCommand]
        public void CancelEdit()
        {
            IsEditing = false;
            _editingPatientId = null;
            ResetForm();
        }

        private void ResetForm()
        {
            NewFirstName = "";
            NewLastName = "";
            NewNationalId = "";
            NewPhone = "";
            NewBirthDate = DateTimeOffset.Now;
        }

        [RelayCommand]
        public async Task DeletePatientAsync()
        {
            if (SelectedPatient != null)
            {
                await _patientService.DeletePatientAsync(SelectedPatient.Id);
                await RefreshDataAsync();
                SelectedPatient = null;
            }
        }

        [RelayCommand]
        public void ShowProfile()
        {
            if (SelectedPatient == null) return;
            
            _patientService.RecordPatientView(SelectedPatient); // Record for LRUCache

            ProfileName = SelectedPatient.FullName;
            ProfileDetails = $"Doğum: {SelectedPatient.BirthDate:dd/MM/yyyy} ({DateTime.Today.Year - SelectedPatient.BirthDate.Year} yaş)\nTC: {SelectedPatient.NationalId} | Tel: {SelectedPatient.Phone}";
            
            ProfileAppointments.Clear();
            foreach (var entry in SelectedPatient.GetHistory())
            {
                ProfileAppointments.Add(entry);
            }
            if (ProfileAppointments.Count == 0) ProfileAppointments.Add("Geçmiş randevu bulunmuyor.");
            
            IsProfileVisible = true;
        }

        [RelayCommand]
        public void CloseProfile()
        {
            IsProfileVisible = false;
        }

        // --- Data Structure Specific Commands ---

        [RelayCommand]
        public void SearchBST()
        {
            if (string.IsNullOrWhiteSpace(BstSearchQuery)) return;
            var parts = BstSearchQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var fName = parts[0];
            var lName = parts.Length > 1 ? parts[1] : "";
            
            var p = _patientService.SearchBST(fName, lName);
            if (p != null)
            {
                BstSearchResult = "✓ BST ile bulundu: " + p.FullName;
                BstPatients.Clear();
                BstPatients.Add(p);
            }
            else
            {
                BstSearchResult = "✗ BST'de bulunamadı.";
                BstPatients.Clear();
            }
        }

        [RelayCommand]
        public void ListBST()
        {
            var list = _patientService.GetAllFromBST();
            BstPatients.Clear();
            foreach (var p in list) BstPatients.Add(p);
            BstSearchResult = $"{list.Count} kayıt listelendi.";
        }

        [RelayCommand]
        public void SearchAVL()
        {
            if (string.IsNullOrWhiteSpace(AvlSearchQuery)) return;
            var parts = AvlSearchQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var fName = parts[0];
            var lName = parts.Length > 1 ? parts[1] : "";
            
            var p = _patientService.SearchAVL(fName, lName);
            if (p != null)
            {
                AvlSearchResult = "✓ AVL ile bulundu: " + p.FullName;
                AvlPatients.Clear();
                AvlPatients.Add(p);
            }
            else
            {
                AvlSearchResult = "✗ AVL'de bulunamadı.";
                AvlPatients.Clear();
            }
        }

        [RelayCommand]
        public void ListAVL()
        {
            var list = _patientService.GetAllFromAVL();
            AvlPatients.Clear();
            foreach (var p in list) AvlPatients.Add(p);
            AvlSearchResult = $"{list.Count} kayıt listelendi.";
        }

        [RelayCommand]
        public void ShowRecentPatients()
        {
            var list = _patientService.GetRecentPatients();
            Patients.Load(list);
            ToastService.Instance.Info("LRU Cache'ten son görüntülenen hastalar.");
        }
    }
}
